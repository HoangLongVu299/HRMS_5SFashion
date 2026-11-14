using System.Data;
using System.ComponentModel.DataAnnotations;
using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;
public class EmployeeService(HrmsContext db)
{
    public async Task ValidateAsync(PersonalForm form)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(form, new ValidationContext(form), errors, true))
            throw new BusinessException(errors[0].ErrorMessage!, errors[0].MemberNames.FirstOrDefault() ?? "");
        if (form.IDTD is int td && !await db.Educations.AnyAsync(x=>x.IDTD==td)) throw new BusinessException("Trình độ không tồn tại.", "IDTD");
        if (form.IDDT is int dt && !await db.Ethnicitys.AnyAsync(x=>x.ID==dt)) throw new BusinessException("Dân tộc không tồn tại.", "IDDT");
        if (form.IDTG is int tg && !await db.Religions.AnyAsync(x=>x.ID==tg)) throw new BusinessException("Tôn giáo không tồn tại.", "IDTG");
    }
    private static void ApplyPersonal(Employee entity, PersonalForm form, byte[]? photo)
    {
        entity.HOTEN = form.HOTEN?.Trim();
        entity.NGAYSINH = form.NGAYSINH?.Date;
        entity.GIOITINH = form.GIOITINH;
        entity.DIENTHOAI = form.DIENTHOAI?.Trim();
        entity.CCCD = form.CCCD?.Trim();
        entity.DIACHI = form.DIACHI?.Trim();
        entity.IDTD = form.IDTD; entity.IDDT = form.IDDT; entity.IDTG = form.IDTG;
        if (form.RemovePhoto) entity.HINHANH = null;
        else if (photo != null) entity.HINHANH = photo;
    }
    private async Task EnsureUniqueIdentityAsync(string? identity, int? employeeId = null)
    {
        var number = identity?.Trim();
        if (string.IsNullOrEmpty(number)) return;

        if (await db.Employees.AnyAsync(x =>
            (employeeId == null || x.MANV != employeeId) && x.CCCD != null && x.CCCD.Trim() == number))
            throw new BusinessException("CCCD này đã được sử dụng cho hồ sơ nhân viên khác.", nameof(PersonalForm.CCCD));
    }

    public async Task<int> CreateAsync(EmployeeCreateForm form)
    {
        await ValidateAsync(form);
        if (!OperatingSystem.IsWindows() && form.Photo != null) throw new BusinessException("Xử lý ảnh yêu cầu Windows.", "Photo");
        var photo = OperatingSystem.IsWindows() ? await PhotoService.ReadAsync(form.Photo) : null;
        var employee = new Employee();
        await PersonnelWriteTransaction.Execute(db, async () =>
        {
            await EnsureUniqueIdentityAsync(form.CCCD);
            ApplyPersonal(employee, form, photo);
            db.Employees.Add(employee);
        });
        return employee.MANV;
    }

    public async Task UpdateAsync(int id, PersonalForm form)
    {
        await ValidateAsync(form);
        if (!OperatingSystem.IsWindows() && form.Photo != null) throw new BusinessException("Xử lý ảnh yêu cầu Windows.", "Photo");
        var photo = OperatingSystem.IsWindows() ? await PhotoService.ReadAsync(form.Photo) : null;
        await PersonnelWriteTransaction.Execute(db, async () =>
        {
            var employee = await db.Employees.FindAsync(id) ?? throw new BusinessException("Hồ sơ không còn tồn tại.");
            await EnsureUniqueIdentityAsync(form.CCCD, id);
            ApplyPersonal(employee, form, photo);
            // Không gán MACTY, IDPB, IDBP, IDCV, DATHOIVIEC. POST giả mạo không thể sửa chúng.
        });
    }
    public async Task DeleteAsync(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var employee = await db.Employees.FindAsync(id) ?? throw new BusinessException("Hồ sơ không còn tồn tại.");
            var related = await db.Database.SqlQuery<int>($"""
                SELECT CAST(CASE WHEN
                EXISTS(SELECT 1 FROM dbo.tb_DOTLAMVIEC WHERE MANV={id}) OR
                EXISTS(SELECT 1 FROM dbo.tb_HOPDONG WHERE MANV={id}) OR
                EXISTS(SELECT 1 FROM dbo.tb_BAOHIEM WHERE MANV={id}) OR
                EXISTS(SELECT 1 FROM dbo.tb_KHENTHUONG_KYLUAT WHERE MANV={id}) OR
                EXISTS(SELECT 1 FROM dbo.tb_NHANVIEN_DIEUCHUYEN WHERE MANV={id}) OR
                EXISTS(SELECT 1 FROM dbo.tb_NHANVIEN_THOIVIEC WHERE MANV={id})
                THEN 1 ELSE 0 END AS int) AS [Value]
                """).SingleAsync();
            if (related == 1) throw new BusinessException("Không thể xóa: hồ sơ có đợt làm việc, hợp đồng, bảo hiểm, quyết định hoặc dữ liệu liên quan. Kể cả dữ liệu có cờ đã xóa vẫn phải được bảo toàn.");
            db.Employees.Remove(employee);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            throw;
        }
    }
    public async Task<EmployeeDetailsVm?> DetailsAsync(int id)
    {
        var employee = await db.Employees.AsNoTracking()
            .Include(x=>x.Department).Include(x=>x.Section).Include(x=>x.Position).Include(x=>x.Company)
            .Include(x=>x.Education).Include(x=>x.Ethnicity).Include(x=>x.Religion)
            .SingleOrDefaultAsync(x=>x.MANV==id);
        if (employee == null) return null;
        var periods = await db.WorkPeriods.AsNoTracking().Where(x=>x.MANV==id)
            .Include(x=>x.Company).Include(x=>x.Histories).ThenInclude(x=>x.Department)
            .Include(x=>x.Histories).ThenInclude(x=>x.Section)
            .Include(x=>x.Histories).ThenInclude(x=>x.Position)
            .OrderBy(x=>x.NGAYBATDAU).ThenBy(x=>x.IDDOT).AsSplitQuery().ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var status = await EmployeeEmployment.WithStatus(db, db.Employees.AsNoTracking().Where(x => x.MANV == id), today)
            .Select(row => row.Status).SingleOrDefaultAsync() ?? "unknown";
        var result = new EmployeeDetailsVm { Employee=employee, Periods=periods, Status=status };
        if (periods.Count == 0 && status != "pending") result.Warnings.Add("Hồ sơ chưa có đợt làm việc. Không đủ dữ liệu để xác định ngày bắt đầu hoặc phục dựng quá trình công tác.");
        foreach (var period in periods)
        {
            if (period.Histories.Count == 0) result.Warnings.Add($"Đợt #{period.IDDOT} chưa có lịch sử công tác.");
            var histories = period.Histories.OrderBy(x=>x.TUNGAY).ThenBy(x=>x.IDLS).ToList();
            foreach (var history in histories)
            {
                if (history.TUNGAY < period.NGAYBATDAU || (period.NGAYKETTHUC is { } end && (history.DENNGAY == null || history.DENNGAY > end || history.TUNGAY > end)))
                    result.Warnings.Add($"Lịch sử #{history.IDLS} nằm ngoài khoảng đợt #{period.IDDOT}; cần kiểm tra dữ liệu gốc.");
            }
            for (int i=1; i<histories.Count; i++)
                if (histories.Take(i).Any(x=>x.DENNGAY == null || x.DENNGAY >= histories[i].TUNGAY))
                    result.Warnings.Add($"Đợt #{period.IDDOT} có các giai đoạn chồng lấn (tính cả ngày kết thúc).");
        }
        var active = periods.Where(x=>x.NGAYBATDAU<=today && (x.NGAYKETTHUC == null || x.NGAYKETTHUC>=today)).ToList();
        if (active.Count>1) result.Warnings.Add("Có nhiều đợt làm việc đồng thời; ứng dụng giữ nguyên dữ liệu gốc.");
        if (periods.Count>0 && ((employee.DATHOIVIEC==false && active.Count==0 && status!="scheduled") || (employee.DATHOIVIEC==true && active.Count>0)))
            result.Warnings.Add("Trạng thái trên hồ sơ không khớp các đợt làm việc tại hôm nay.");
        if (active.Count==1)
        {
            var now = active[0].Histories.Where(x=>x.TUNGAY<=today && (x.DENNGAY == null || x.DENNGAY>=today)).ToList();
            if (employee.MACTY != active[0].MACTY || (now.Count==1 && (employee.IDPB!=now[0].IDPB || employee.IDBP!=now[0].IDBP || employee.IDCV!=now[0].IDCV)))
                result.Warnings.Add("Thông tin tổ chức trên hồ sơ khác lịch sử hiện tại. Không tự ghi đè hoặc đồng bộ.");
        }
        return result;
    }
}
