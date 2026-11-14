using System.ComponentModel.DataAnnotations;
using System.Data;
using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Services;

public class PersonnelService(HrmsContext db)
{
    public static void Validate(object form)
    {
        var errors=new List<ValidationResult>();
        if(!Validator.TryValidateObject(form,new ValidationContext(form),errors,true))
            throw new BusinessException(errors[0].ErrorMessage ?? "Dữ liệu không hợp lệ.",errors[0].MemberNames.FirstOrDefault() ?? "");
    }
    // Every new write uses the same transaction-owned application lock, then SQL serializable
    // range/row locks. No schema change, no fake user IDs, no in-memory-only duplicate guard.
    private Task Atomic(Func<Task> action) => PersonnelWriteTransaction.Execute(db,action);
    private async Task<WorkPeriod> Period(int? employee,int? period)
    {
        if(employee is null || period is null) throw new BusinessException("Chọn nhân viên và đợt làm việc.");
        var p=await db.WorkPeriods.FromSqlInterpolated($"SELECT * FROM dbo.tb_DOTLAMVIEC WITH (UPDLOCK,HOLDLOCK) WHERE IDDOT={period}").SingleOrDefaultAsync();
        if(p==null || p.MANV!=employee) throw new BusinessException("Đợt làm việc không thuộc nhân viên đã chọn.","IDDOT");
        return p;
    }
    public async Task<int> NextContractSigning(int employeeId)
    {
        // Existing signing numbers remain reserved, including soft-deleted contracts.
        var history = await db.Contracts.AsNoTracking().Where(x => x.MANV == employeeId)
            .GroupBy(x => x.MANV)
            .Select(group => new { Count = group.Count(), Highest = group.Max(x => x.LANKY) ?? 0 })
            .SingleOrDefaultAsync();
        int used = history == null ? 0 : Math.Max(history.Count, history.Highest);
        if (used == int.MaxValue) throw new BusinessException("Lần ký đã vượt giới hạn.", nameof(ContractForm.LANKY));
        return used + 1;
    }

    public async Task<string> SaveContract(string? original,ContractForm f)
    {
        string savedNumber="";
        var submittedNumber=f.SOHD;
        try
        {
        await Atomic(async () =>
        {
        if (original == null)
        {
            f.SOHD = await new DocumentNumberService(db).NextContract(f.NGAYKY);
            f.NGAYKETTHUC = null;
            f.THOIHAN = null;
            f.LANKY = 1;
        }
        Validate(f);
        if (original == null)
        {
            var start = f.NGAYBATDAU!.Value.Date;
            if (f.DurationMonths is not int months || months < 1)
                throw new BusinessException("Nhập thời hạn là số tháng nguyên lớn hơn 0.", nameof(f.DurationMonths));
            if (months > (9999 - start.Year) * 12 + 12 - start.Month)
                throw new BusinessException("Thời hạn vượt quá phạm vi ngày hỗ trợ.", nameof(f.DurationMonths));
            f.NGAYKETTHUC = start.AddMonths(months);
            f.THOIHAN = $"{months} tháng";
        }
        var number=f.SOHD.Trim();
        if(original!=null && !String.Equals(original,f.SOHD,StringComparison.Ordinal)) throw new BusinessException("Không được đổi số hợp đồng sau khi tạo.");
        LaborContract e;
        WorkPeriod p;
        if(original==null)
        {
            if(await db.Contracts.AnyAsync(x=>x.SOHD==number)) throw new BusinessException("Số hợp đồng đã tồn tại, kể cả hợp đồng đã xóa mềm.","SOHD");
            p=await new ContractEmploymentService(db).ResolveForCreate(f);
            // Reject identical contract data if the resolved period already has it.
            var signed=f.NGAYKY!.Value.Date;var start=f.NGAYBATDAU!.Value.Date;var end=f.NGAYKETTHUC?.Date;
            var content=ContractDocumentTemplate.ForStorage(f.NOIDUNG);
            if(await db.Contracts.AnyAsync(x=>x.MANV==p.MANV && x.IDDOT==p.IDDOT &&
                x.DELETED_DATE==null && x.DELETED_BY==null && x.NGAYKY==signed && x.NGAYBATDAU==start &&
                x.NGAYKETTHUC==end &&
                x.HESOLUONG==f.HESOLUONG && x.LUONGCOBAN==f.LUONGCOBAN && x.NOIDUNG==content))
                throw new BusinessException("Đã có hợp đồng có toàn bộ thông tin trùng khớp trong lần làm việc này. Hãy kiểm tra hợp đồng đã lưu trước khi lập mới.");
            f.LANKY=await NextContractSigning(p.MANV);
            e=new(){SOHD=number,CREATED_DATE=DateTime.Now}; db.Contracts.Add(e);
        }
        else
        {
            e=await db.Contracts.SingleOrDefaultAsync(x=>x.SOHD==original) ?? throw new BusinessException("Không tìm thấy hợp đồng.");
            if(e.DELETED_DATE!=null || e.DELETED_BY!=null) throw new BusinessException("Hợp đồng đã xóa mềm, không thể sửa.");
            p=await Period(f.MANV,f.IDDOT);
            e.UPDATED_DATE=DateTime.Now;
        }
        e.MANV=p.MANV; e.IDDOT=p.IDDOT; e.MACTY=p.MACTY;
        e.NGAYKY=f.NGAYKY?.Date; e.NGAYBATDAU=f.NGAYBATDAU?.Date; e.NGAYKETTHUC=f.NGAYKETTHUC?.Date;
        e.THOIHAN=f.THOIHAN; e.LANKY=f.LANKY; e.NOIDUNG=ContractDocumentTemplate.ForStorage(f.NOIDUNG); e.HESOLUONG=f.HESOLUONG; e.LUONGCOBAN=f.LUONGCOBAN;
        savedNumber=number;
        });
        }
        catch {f.SOHD=submittedNumber;throw;}
        return savedNumber;
    }
    public Task DeleteContract(string number) => Atomic(async () =>
    {
        var e=await db.Contracts.SingleOrDefaultAsync(x=>x.SOHD==number) ?? throw new BusinessException("Không tìm thấy hợp đồng.");
        if(e.DELETED_DATE!=null || e.DELETED_BY!=null) throw new BusinessException("Hợp đồng đã được xóa mềm trước đó.");
        e.DELETED_DATE=DateTime.Now;
        // No login: DELETED_BY remains NULL. Both date and legacy BY are checked on reads.
    });
    private async Task<(Employee Employee,WorkPeriod Period,List<WorkHistory> History)> ActiveTimeline(int? employee)
    {
        if(employee==null) throw new BusinessException("Chọn nhân viên đang làm việc.","MANV");
        var current=await DecisionEligibility.LoadAsync(db,employee);
        if(current.Count!=1) throw new BusinessException("Nhân viên không còn một đợt làm việc hiện tại hợp lệ. Vui lòng chọn lại hoặc kiểm tra hồ sơ.","MANV");
        var p=await Period(employee,current[0].PeriodId);
        var e=await db.Employees.SingleAsync(x=>x.MANV==p.MANV);
        var periods=await db.WorkPeriods.Where(x=>x.MANV==p.MANV).OrderBy(x=>x.NGAYBATDAU).ToListAsync();
        var eligible = await DecisionEligibility.LoadAsync(db, e.MANV);
        if (!eligible.Any(x => x.PeriodId == p.IDDOT && x.CompanyId == p.MACTY))
        {
            var terms = await db.Terminations.AsNoTracking().Where(x => x.MANV == e.MANV || x.IDDOT == p.IDDOT).ToListAsync();
            bool unlinked = await db.Transfers.AnyAsync(x => x.MANV == e.MANV && x.IDDOT == null && x.DELETED_DATE == null && x.DELETED_BY == null)
                || terms.Any(x => x.IDDOT == null && x.DELETED_DATE == null && x.DELETED_BY == null);
            var result = DecisionEligibility.Evaluate(e, p, periods, terms, unlinked, DateOnly.FromDateTime(DateTime.Today));
            throw new BusinessException(result.Reason ?? DecisionEligibility.Unavailable, "IDDOT");
        }
        var h=await db.WorkHistorys.Where(x=>x.IDDOT==p.IDDOT).OrderBy(x=>x.TUNGAY).ThenBy(x=>x.IDLS).ToListAsync();
        ValidateTimeline(p,h);
        return(e,p,h);
    }
    public static void ValidateTimeline(WorkPeriod p,IReadOnlyList<WorkHistory> h)
    {
        if(h.Count==0) throw new BusinessException("Đợt chưa có lịch sử công tác. Không tự dựng giai đoạn cũ.");
        if(h[0].TUNGAY!=p.NGAYBATDAU) throw new BusinessException("Lịch sử không bắt đầu cùng đợt; cần kiểm tra dữ liệu gốc.");
        for(int i=0;i<h.Count;i++)
        {
            if(h[i].DENNGAY<h[i].TUNGAY || h[i].TUNGAY<p.NGAYBATDAU)
                throw new BusinessException("Lịch sử có khoảng ngày không hợp lệ.");
            if(i>0 && (h[i-1].DENNGAY is not {} end || end==DateOnly.MaxValue || end.AddDays(1)!=h[i].TUNGAY))
                throw new BusinessException("Lịch sử có khoảng trống hoặc chồng lấn; cần xử lý trước khi lập quyết định.");
        }
        if(h[^1].DENNGAY!=null) throw new BusinessException("Giai đoạn cuối đã đóng nhưng đợt vẫn mở; cần kiểm tra dữ liệu.");
    }
    public async Task<string> CreateTransfer(TransferForm f)
    {
        string savedNumber="";
        var submittedNumber=f.SOQD;
        try
        {
        await Atomic(async () =>
        {
        f.SOQD=await new DocumentNumberService(db).NextTransfer(f.NGAY);
        Validate(f); var number=f.SOQD.Trim();
        if(await db.Transfers.AnyAsync(x=>x.SOQD==number)) throw new BusinessException("Số quyết định đã tồn tại. Không gửi lại form đã lưu.","SOQD");
        var (e,p,h)=await ActiveTimeline(f.MANV);
        var effective=f.NGAYHIEULUC!.Value;
        var last=h[^1];
        if(effective<=last.TUNGAY) throw new BusinessException("Ngày hiệu lực phải sau ngày bắt đầu giai đoạn cuối. Không hỗ trợ nhập lùi qua nghiệp vụ đã phát sinh hoặc tạo giai đoạn âm.","NGAYHIEULUC");
        if(await db.Transfers.AnyAsync(x=>x.IDDOT==p.IDDOT && (x.NGAYHIEULUC==null || x.NGAYHIEULUC>=effective)))
            throw new BusinessException("Đợt đã có điều chuyển cùng/sau ngày này hoặc quyết định thiếu ngày hiệu lực. Chưa hỗ trợ tái tính lịch sử.");
        if(!await db.Departments.AnyAsync(x=>x.IDPB==f.MAPB2) || !await db.Positions.AnyAsync(x=>x.IDCV==f.IDCV) ||
            (f.IDBP!=null && !await db.Sections.AnyAsync(x=>x.IDBP==f.IDBP))) throw new BusinessException("Phòng ban, bộ phận hoặc chức vụ không tồn tại.");
        if(last.IDPB==f.MAPB2 && last.IDBP==f.IDBP && last.IDCV==f.IDCV) throw new BusinessException("Thông tin sau điều chuyển không thay đổi so với giai đoạn hiện tại.");
        db.Transfers.Add(new(){SOQD=number,MANV=p.MANV,IDDOT=p.IDDOT,NGAY=f.NGAY?.Date,NGAYHIEULUC=effective,
            MAPB=last.IDPB,MAPB2=f.MAPB2,LYDO=f.LYDO,GHICHU=f.GHICHU,CREATED_DATE=DateTime.Now});
        await db.SaveChangesAsync(); // SQL composite FK requires decision before history; still atomic.
        last.DENNGAY=effective.AddDays(-1);
        await db.SaveChangesAsync(); // Close first: tr_LS_KiemTraThoiGian rejects overlapping INSERTs.
        db.WorkHistorys.Add(new(){IDDOT=p.IDDOT,IDPB=f.MAPB2!.Value,IDBP=f.IDBP,IDCV=f.IDCV!.Value,TUNGAY=effective,SOQD_DIEUCHUYEN=number,GHICHU=f.GHICHU});
        e.MACTY=p.MACTY; e.IDPB=f.MAPB2; e.IDBP=f.IDBP; e.IDCV=f.IDCV;
        savedNumber=number;
        });
        }
        catch {f.SOQD=submittedNumber;throw;}
        return savedNumber;
    }
    public async Task<string> CreateTermination(TerminationForm f)
    {
        string savedNumber="";var submittedNumber=f.SOQD;
        try {
        await Atomic(async () =>
        {
        f.SOQD=await new DocumentNumberService(db).NextTermination(f.NGAYNGHI);
        Validate(f); var number=f.SOQD.Trim();
        if(await db.Terminations.AnyAsync(x=>x.SOQD==number)) throw new BusinessException("Số quyết định đã tồn tại. Không gửi lại form đã lưu.","SOQD");
        var (e,p,h)=await ActiveTimeline(f.MANV);
        var leave=DateOnly.FromDateTime(f.NGAYNGHI!.Value);
        if(leave<=p.NGAYBATDAU || leave<=h[^1].TUNGAY) throw new BusinessException("Ngày nghỉ phải sau ngày bắt đầu đợt và giai đoạn cuối; không được để lịch sử phát sinh từ ngày nghỉ trở đi.","NGAYNGHI");
        if(await db.Transfers.AnyAsync(x=>x.IDDOT==p.IDDOT && (x.NGAYHIEULUC==null || x.NGAYHIEULUC>=leave)))
            throw new BusinessException("Có quyết định điều chuyển sau thời điểm đóng đợt hoặc thiếu ngày hiệu lực. Cần xử lý dữ liệu trước.");
        db.Terminations.Add(new(){SOQD=number,MANV=p.MANV,IDDOT=p.IDDOT,NGAYNOPDON=f.NGAYNOPDON?.Date,NGAYNGHI=f.NGAYNGHI?.Date,
            LYDO=f.LYDO,GHICHU=f.GHICHU,CREATED_DATE=DateTime.Now});
        await db.SaveChangesAsync();
        h[^1].DENNGAY=leave.AddDays(-1);
        await db.SaveChangesAsync(); // tr_DOT requires all histories closed BEFORE closing the period.
        p.NGAYKETTHUC=leave.AddDays(-1);
        var today=DateOnly.FromDateTime(DateTime.Today);
        e.DATHOIVIEC=!await db.WorkPeriods.AnyAsync(x=>x.MANV==p.MANV && x.IDDOT!=p.IDDOT && x.NGAYBATDAU<=today && (x.NGAYKETTHUC==null || x.NGAYKETTHUC>=today));
        // Contract expiry is intentionally untouched.
        savedNumber=number;
        });
        }
        catch {f.SOQD=submittedNumber;throw;}
        return savedNumber;
    }
    public Task EditTransferNotes(string number,DecisionNotesForm f) => Atomic(async () =>
    {
        Validate(f);
        var e=await db.Transfers.SingleOrDefaultAsync(x=>x.SOQD==number) ?? throw new BusinessException("Không tìm thấy quyết định.");
        if(e.DELETED_DATE!=null || e.DELETED_BY!=null) throw new BusinessException("Quyết định có cờ xóa cũ, chỉ được xem.");
        e.LYDO=f.LYDO; e.GHICHU=f.GHICHU; e.UPDATED_DATE=DateTime.Now;
    });
    public Task EditTerminationNotes(string number,DecisionNotesForm f) => Atomic(async () =>
    {
        Validate(f);
        if(f.LYDO?.Length>200) throw new BusinessException("Lý do thôi việc tối đa 200 ký tự.","LYDO");
        var e=await db.Terminations.SingleOrDefaultAsync(x=>x.SOQD==number) ?? throw new BusinessException("Không tìm thấy quyết định.");
        if(e.DELETED_DATE!=null || e.DELETED_BY!=null) throw new BusinessException("Quyết định có cờ xóa cũ, chỉ được xem.");
        e.LYDO=f.LYDO; e.GHICHU=f.GHICHU; e.UPDATED_DATE=DateTime.Now;
    });
}
