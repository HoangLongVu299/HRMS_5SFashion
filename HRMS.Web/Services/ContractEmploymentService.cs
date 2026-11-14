using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;

// Creation policy only. Editing an existing contract keeps the explicit historical link.
public static class ContractEmploymentPolicy
{
    public static IQueryable<int?> UnlinkedDecisionEmployees(HrmsContext db) =>
        db.Terminations.Where(x => x.IDDOT == null && x.DELETED_DATE == null && x.DELETED_BY == null)
            .Select(x => x.MANV)
            .Union(db.Transfers.Where(x => x.IDDOT == null && x.DELETED_DATE == null && x.DELETED_BY == null)
                .Select(x => x.MANV));

    public static void ValidateRehire(IReadOnlyList<WorkPeriod> periods, bool unlinkedDecision,
        DateOnly start, DateOnly today)
    {
        if (periods.Any(x => x.NGAYKETTHUC < x.NGAYBATDAU))
            throw new BusinessException("Dữ liệu đợt làm việc không hợp lệ; cần xác minh hồ sơ.", "MANV");
        var ordered = periods.OrderBy(x => x.NGAYBATDAU).ToList();
        for (int i = 1; i < ordered.Count; i++)
            if (ordered[i - 1].NGAYKETTHUC == null || ordered[i - 1].NGAYKETTHUC >= ordered[i].NGAYBATDAU)
                throw new BusinessException("Có đợt làm việc chồng lấn hoặc nhiều đợt mở; cần kiểm tra dữ liệu.", "MANV");
        if (unlinkedDecision)
            throw new BusinessException("Hồ sơ còn quyết định chưa liên kết đợt; cần xác minh trước khi lập hợp đồng mới.", "MANV");
        if (periods.Count == 0)
            throw new BusinessException("Chưa có đợt làm việc cũ để tiếp nhận lại. Cần xác minh hồ sơ.", "MANV");
        if (periods.Any(x => x.NGAYKETTHUC == null))
            throw new BusinessException("Hồ sơ đã thôi việc nhưng vẫn còn đợt làm việc mở. Cần xác minh hồ sơ.", "MANV");
        if (periods.Any(x => x.NGAYKETTHUC >= start))
            throw new BusinessException("Ngày quay lại phải sau ngày làm việc cuối cùng của tất cả đợt cũ.", "NGAYBATDAU");
        if (start > today)
            throw new BusinessException("Chỉ tiếp nhận lại từ ngày thực tế vào làm.", "NGAYBATDAU");
    }

    public static void ValidateClosedHistories(IReadOnlyList<WorkPeriod> periods, IReadOnlyList<WorkHistory> histories)
    {
        foreach(var period in periods)
        {
            var timeline=histories.Where(x=>x.IDDOT==period.IDDOT).OrderBy(x=>x.TUNGAY).ThenBy(x=>x.IDLS).ToList();
            if(period.NGAYKETTHUC==null || timeline.Count==0 || timeline[0].TUNGAY!=period.NGAYBATDAU || timeline[^1].DENNGAY!=period.NGAYKETTHUC)
                throw new BusinessException("Lịch sử đợt cũ chưa đóng đầy đủ; cần kiểm tra trước khi tiếp nhận lại.");
            for(int i=0;i<timeline.Count;i++)
                if(timeline[i].DENNGAY==null || timeline[i].DENNGAY<timeline[i].TUNGAY ||
                    timeline[i].TUNGAY<period.NGAYBATDAU || timeline[i].DENNGAY>period.NGAYKETTHUC ||
                    (i>0 && (timeline[i-1].DENNGAY==DateOnly.MaxValue || timeline[i-1].DENNGAY!.Value.AddDays(1)!=timeline[i].TUNGAY)))
                    throw new BusinessException("Lịch sử đợt cũ có khoảng trống hoặc chồng lấn; cần kiểm tra dữ liệu.");
        }
    }
}

// Called INSIDE PersonnelService.Atomic: the period, first history, profile and contract
// either commit together or roll back together. IDDOT from the browser is never used.
public class ContractEmploymentService(HrmsContext db)
{
    public async Task<WorkPeriod> ResolveForCreate(ContractForm form)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        // Recheck the profile status inside the write transaction against the selected mode.
        var candidate = await ContractEmployeeOptions.Eligible(db, db.Employees.Where(x => x.MANV == form.MANV), form.Rehire, today)
            .SingleOrDefaultAsync()
            ?? throw new BusinessException("Nhân viên không còn thuộc nhóm đang chọn. Vui lòng chọn lại nhân viên.", "MANV");
        var employee = candidate.Employee;
        var rehire = candidate.Status == "left";
        var start = DateOnly.FromDateTime(form.NGAYBATDAU!.Value);
        if (employee.NGAYSINH is { } birth && start < DateOnly.FromDateTime(birth))
            throw new BusinessException("Ngày bắt đầu không được trước ngày sinh.", "NGAYBATDAU");

        if (rehire)
        {
            var periods = await db.WorkPeriods.Where(x => x.MANV == employee.MANV).ToListAsync();
            var unlinked = await ContractEmploymentPolicy.UnlinkedDecisionEmployees(db).ContainsAsync(employee.MANV);
            ContractEmploymentPolicy.ValidateRehire(periods, unlinked, start, today);
            var periodIds = periods.Select(x => x.IDDOT).ToList();
            var histories = await db.WorkHistorys.Where(x => periodIds.Contains(x.IDDOT)).ToListAsync();
            ContractEmploymentPolicy.ValidateClosedHistories(periods, histories);
        }

        var company = form.RehireCompanyId;
        var department = form.RehireDepartmentId;
        var position = form.RehirePositionId;
        var section = form.RehireSectionId;
        if (company == null || !await db.Companys.AnyAsync(x => x.MACTY == company))
            throw new BusinessException("Bổ sung công ty hợp lệ cho lần tiếp nhận.", "RehireCompanyId");
        if (department == null || !await db.Departments.AnyAsync(x => x.IDPB == department))
            throw new BusinessException("Bổ sung phòng ban hợp lệ cho lần tiếp nhận.", "RehireDepartmentId");
        if (position == null || !await db.Positions.AnyAsync(x => x.IDCV == position))
            throw new BusinessException("Bổ sung chức vụ hợp lệ cho lần tiếp nhận.", "RehirePositionId");
        if (section != null && !await db.Sections.AnyAsync(x => x.IDBP == section))
            throw new BusinessException("Bộ phận không tồn tại.", "RehireSectionId");
        var created = new WorkPeriod { MANV=employee.MANV, MACTY=company.Value, NGAYBATDAU=start,
            CREATED_DATE=DateTime.Now, GHICHU=rehire ? "Tiếp nhận lại khi lập hợp đồng mới." : "Tiếp nhận lần đầu khi lập hợp đồng đầu tiên." };
        db.WorkPeriods.Add(created);
        await db.SaveChangesAsync(); // Obtain SQL IDENTITY before inserting the history and contract.
        db.WorkHistorys.Add(new WorkHistory { IDDOT=created.IDDOT, IDPB=department.Value,
            IDBP=section, IDCV=position.Value, TUNGAY=start });
        employee.MACTY=company; employee.IDPB=department; employee.IDBP=section; employee.IDCV=position;
        employee.DATHOIVIEC=false;
        return created;
    }
}
