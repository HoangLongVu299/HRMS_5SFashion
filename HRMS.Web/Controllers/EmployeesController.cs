using HRMS.Web.Data;
using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Controllers;
public class EmployeesController(HrmsContext db, EmployeeService service, LookupService lookups, ILogger<EmployeesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] EmployeeListVm vm)
    {
        vm.Lookups = await lookups.LoadAsync();
        if (!ModelState.IsValid) return View(vm);
        var query = Filtered(vm);
        vm.Total=await query.CountAsync(); vm.Page=Math.Clamp(vm.Page, 1, vm.Pages);
        vm.Rows=await Rows(query.OrderBy(x=>x.Employee.MANV).Skip((vm.Page-1)*vm.PageSize).Take(vm.PageSize)).ToListAsync();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] EmployeeListVm vm)
    {
        if (!ModelState.IsValid) return BadRequest("Bộ lọc hồ sơ nhân viên không hợp lệ.");

        var employees = await Filtered(vm).OrderBy(row => row.Employee.MANV)
            .Select(row => new
            {
                row.Employee.MANV,
                row.Employee.HOTEN,
                row.Employee.DIENTHOAI,
                Department = row.Employee.Department == null ? null : row.Employee.Department.TENPB,
                Section = row.Employee.Section == null ? null : row.Employee.Section.TENBP,
                Position = row.Employee.Position == null ? null : row.Employee.Position.TENCV,
                Education = row.Employee.Education == null ? null : row.Employee.Education.TENTD,
                Ethnicity = row.Employee.Ethnicity == null ? null : row.Employee.Ethnicity.TENDT,
                Religion = row.Employee.Religion == null ? null : row.Employee.Religion.TENTG,
                row.Employee.DIACHI,
                row.Status
            })
            .ToListAsync();
        var rows = employees.Select(employee => new string?[]
        {
            employee.MANV.ToString(), employee.HOTEN, employee.DIENTHOAI, employee.Department,
            employee.Section, employee.Position, employee.Education, employee.Ethnicity,
            employee.Religion, employee.DIACHI, DisplayText.EmploymentStatus(employee.Status)
        }).ToList();

        return View("PrintList", new PrintListVm("Danh sách nhân viên",
            [new("Mã NV", SingleLine: true), new("Họ và tên"), new("Điện thoại", SingleLine: true),
             new("Phòng ban"), new("Bộ phận"), new("Chức vụ"), new("Trình độ"), new("Dân tộc"),
             new("Tôn giáo", SingleLine: true), new("Địa chỉ"), new("Trạng thái")],
            rows, ShowRowNumbers: false)
        {
            Subtitle = string.IsNullOrEmpty(vm.Status) ? "Tất cả" : DisplayText.EmploymentStatus(vm.Status)
        });
    }

    private IQueryable<EmployeeEmployment.Row> Filtered(EmployeeListVm vm)
    {
        var query = db.Employees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(vm.Search))
        {
            var word = vm.Search.Trim();
            int.TryParse(word, out int code);
            query = query.Where(x=>x.MANV==code || (x.HOTEN!=null && x.HOTEN.Contains(word)) || (x.CCCD!=null && x.CCCD.Contains(word)) || (x.DIENTHOAI!=null && x.DIENTHOAI.Contains(word)));
        }
        if (vm.CompanyId is int company) query=query.Where(x=>x.MACTY==company);
        if (vm.DepartmentId is int department) query=query.Where(x=>x.IDPB==department);
        if (vm.PositionId is int position) query=query.Where(x=>x.IDCV==position);
        var rows = EmployeeEmployment.WithStatus(db, query, DateOnly.FromDateTime(DateTime.Today));
        return string.IsNullOrEmpty(vm.Status) ? rows : rows.Where(row => row.Status == vm.Status);
    }

    private static IQueryable<EmployeeRow> Rows(IQueryable<EmployeeEmployment.Row> query) =>
        query.Select(row => new EmployeeRow {
            Id=row.Employee.MANV, Name=row.Employee.HOTEN, Phone=row.Employee.DIENTHOAI,
            Identity=row.Employee.CCCD, Status=row.Status,
            Company=row.Employee.Company==null?null:row.Employee.Company.TENCTY,
            Department=row.Employee.Department==null?null:row.Employee.Department.TENPB,
            Section=row.Employee.Section==null?null:row.Employee.Section.TENBP,
            Position=row.Employee.Position==null?null:row.Employee.Position.TENCV,
            MissingHistory=row.Status!="pending" && (!row.Employee.WorkPeriods.Any() || row.Employee.WorkPeriods.Any(d=>!d.Histories.Any()))
        });

    [HttpGet] public async Task<IActionResult> Details(int id)
    {
        var vm=await service.DetailsAsync(id); return vm==null?NotFound():View(vm);
    }
    [HttpGet] public async Task<IActionResult> Create() => View(new EmployeeCreateForm { Lookups=await lookups.LoadAsync() });
    [HttpPost, RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> Create(EmployeeCreateForm form)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id=await service.CreateAsync(form);
                TempData["Success"]="Đã lưu hồ sơ. Chọn Lập hợp đồng đầu tiên để ghi nhận thông tin tiếp nhận.";
                return RedirectToAction(nameof(Details),new { id });
            }
            catch (Exception ex) when (ex is BusinessException or DbUpdateException or SqlException) { AddError(ex); }
        }
        form.Lookups=await lookups.LoadAsync();
        if (form.Photo!=null) ModelState.AddModelError("Photo","Nếu cần tải ảnh, vui lòng chọn lại tệp trước khi lưu.");
        return View(form);
    }
    [HttpGet] public async Task<IActionResult> Edit(int id)
    {
        var details=await service.DetailsAsync(id); if (details==null) return NotFound();
        var e=details.Employee;
        var form=new PersonalForm { EmployeeId=id, HOTEN=e.HOTEN, NGAYSINH=e.NGAYSINH, GIOITINH=e.GIOITINH, DIENTHOAI=e.DIENTHOAI, CCCD=e.CCCD, DIACHI=e.DIACHI, IDTD=e.IDTD, IDDT=e.IDDT, IDTG=e.IDTG };
        await PrepareEdit(form, id); return View(form);
    }
    [HttpPost, RequestSizeLimit(6*1024*1024)]
    public async Task<IActionResult> Edit(int id, PersonalForm form)
    {
        if (!await db.Employees.AsNoTracking().AnyAsync(x=>x.MANV==id)) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                await service.UpdateAsync(id,form); TempData["Success"]="Đã cập nhật thông tin cá nhân. Quá trình công tác được giữ nguyên.";
                return RedirectToAction(nameof(Details),new { id });
            }
            catch (Exception ex) when (ex is BusinessException or DbUpdateException or SqlException) { AddError(ex); }
        }
        await PrepareEdit(form,id);
        if (form.Photo!=null) ModelState.AddModelError("Photo","Nếu cần tải ảnh, vui lòng chọn lại tệp trước khi lưu.");
        return View(form);
    }
    private async Task PrepareEdit(PersonalForm form,int id)
    {
        form.EmployeeId=id; form.Lookups=await lookups.LoadAsync();
        var details=await service.DetailsAsync(id);
        if (details==null) return;
        var e=details.Employee;
        form.OrganizationSummary=$"Công ty: {DisplayText.Value(e.Company?.TENCTY)} | Phòng ban: {DisplayText.Value(e.Department?.TENPB)} | Bộ phận: {DisplayText.Value(e.Section?.TENBP)} | Chức vụ: {DisplayText.Value(e.Position?.TENCV)} | Trạng thái: {DisplayText.EmploymentStatus(details.Status)}";
        ViewData["Warnings"]=details.Warnings;
    }
    [HttpGet] public async Task<IActionResult> Delete(int id)
    {
        var details=await service.DetailsAsync(id); return details==null?NotFound():View(details);
    }
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try { await service.DeleteAsync(id); TempData["Success"]="Đã xóa hồ sơ nhập nhầm không có dữ liệu liên quan."; return RedirectToAction(nameof(Index)); }
        catch (Exception ex) when (ex is BusinessException or DbUpdateException or SqlException) { AddError(ex); }
        var details=await service.DetailsAsync(id); return details==null?NotFound():View("Delete",details);
    }
    [HttpGet] public async Task<IActionResult> Photo(int id)
    {
        var bytes=await db.Employees.AsNoTracking().Where(x=>x.MANV==id).Select(x=>x.HINHANH).SingleOrDefaultAsync();
        Response.Headers["Cache-Control"]="no-store";
        Response.Headers["X-Content-Type-Options"]="nosniff";
        if (bytes==null || bytes.Length==0 || PhotoService.Mime(bytes)==null)
            return File(System.Text.Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' width='160' height='180'><rect width='100%' height='100%' fill='#e9eef5'/><circle cx='80' cy='58' r='27' fill='#8798b0'/><path d='M27 150a53 53 0 0 1 106 0' fill='#8798b0'/><text x='80' y='172' text-anchor='middle' font-size='12'>Chưa có ảnh hợp lệ</text></svg>"),"image/svg+xml");
        return File(bytes,PhotoService.Mime(bytes)!);
    }
    private void AddError(Exception ex)
    {
        if (ex is BusinessException business) { ModelState.AddModelError(business.Field,business.Message); return; }
        logger.LogError(ex,"Lỗi ghi dữ liệu hồ sơ nhân viên");
        var sql=ex as SqlException ?? ex.InnerException as SqlException;
        string message=sql?.Number switch {
            547=>"Thao tác vi phạm ràng buộc dữ liệu: danh mục đã thay đổi hoặc hồ sơ đang có dữ liệu liên quan. Vui lòng kiểm tra lại.",
            2601 or 2627=>"Dữ liệu trùng khóa hoặc ràng buộc duy nhất trong SQL Server.",
            1205=>"Có thao tác đồng thời. Giao dịch đã hủy; vui lòng thử lại.",
            _=>"Không thể lưu thay đổi. Vui lòng kiểm tra kết nối SQL Server và nhật ký ứng dụng. Nếu đang thêm hồ sơ, toàn bộ giao dịch đã được hủy."
        };
        ModelState.AddModelError("",message);
    }
}
