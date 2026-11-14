using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Controllers;
public abstract class PersonnelControllerBase(PersonnelQueryService query,ILogger logger) : Controller
{
    protected abstract string Kind { get; }
    [HttpGet] public async Task<IActionResult> Index([FromQuery] PersonnelListVm vm)
    {
        (vm.Title,vm.DateLabel)=Kind switch {
            "Contracts"=>("Hợp đồng lao động","Ngày bắt đầu hợp đồng"),
            "Transfers"=>("Điều chuyển nhân viên","Ngày hiệu lực"),
            _=>("Nhân viên thôi việc","Ngày nghỉ")
        };
        try { if(ModelState.IsValid) await query.List(Kind,vm); else await query.Options(vm); }
        catch(BusinessException ex) { Error(ex); }
        ViewData["Kind"]=Kind;
        return View("~/Views/Personnel/Index.cshtml",vm);
    }
    [HttpGet] public async Task<IActionResult> Periods(int employee) => Json(
        Kind is "Transfers" or "Terminations"
            ? await query.DecisionPeriodOptions(employee)
            : await query.PeriodOptions(employee));
    // GET and invalid POST use the same lists. Never append submitted IDs.
    // Razor prioritizes ModelState, so clear rejected attempted values as well.
    protected async Task CurrentDecisionOptions(PersonnelOptions vm, int? employee)
    {
        await query.DecisionOptions(vm, employee, null);
        bool invalidEmployee = employee != null && !vm.Employees.Any(x => x.Value == employee.ToString());
        if (invalidEmployee || (employee == null && ModelState.ContainsKey("MANV")))
        {
            ClearAttempt("MANV"); employee = null;
            vm.Periods.Clear(); vm.CompanyName = null; vm.EligibilityWarning = null;
        }
        ModelState.Remove("IDDOT");
        if (invalidEmployee) ModelState.AddModelError("", DecisionEligibility.Unavailable);
        if (vm is TransferForm transfer) { transfer.MANV=employee; transfer.IDDOT=null; }
        if (vm is TerminationForm termination) { termination.MANV=employee; termination.IDDOT=null; }
    }
    [HttpGet] public async Task<IActionResult> CurrentEmployment(int employee,DateOnly? date)
    {
        if(!ModelState.IsValid) return BadRequest(new {message="Nhân viên hoặc ngày hiệu lực không hợp lệ."});
        return Json(await query.CurrentDecisionInfo(employee,date));
    }
    private void ClearAttempt(string field)
    {
        var errors = ModelState.TryGetValue(field, out var entry)
            ? entry.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Giá trị nhập không hợp lệ." : x.ErrorMessage).ToList()
            : new List<string>();
        ModelState.Remove(field);
        foreach (var error in errors) ModelState.AddModelError(field, error);
    }
    [HttpGet] public async Task<IActionResult> OldDepartment(int employee,int period,DateOnly date) => Json(new {text=await query.OldDepartment(employee,period,date)});
    protected void Error(Exception ex)
    {
        if(ex is BusinessException b) {ModelState.AddModelError(b.Field,b.Message);return;}
        logger.LogError(ex,"Không thể ghi nghiệp vụ nhân sự.");
        var sql=ex as SqlException ?? ex.InnerException as SqlException;
        ModelState.AddModelError("",sql?.Number switch {
            2601 or 2627=>"Số văn bản hoặc dữ liệu đã tồn tại. Kiểm tra danh sách trước khi gửi lại.",
            51001 or 51002 or 51003 or 51004=>"SQL Server phát hiện đợt/lịch sử chồng lấn hoặc nằm ngoài đợt. Toàn bộ giao dịch đã hủy; kiểm tra lại dữ liệu.",
            547=>"Liên kết hoặc khoảng ngày vi phạm ràng buộc SQL Server. Kiểm tra đợt và lịch sử.",
            1205 or 51020 or -2=>"Có thao tác đồng thời hoặc hết thời gian chờ. Không lưu dở giao dịch; hãy tải lại danh sách để kiểm tra trước khi thử lại.",
            _=>"Không thể xác nhận lưu. Hãy kiểm tra danh sách trước khi thử lại và xem nhật ký ứng dụng; không có thao tác lưu một phần trong transaction."
        });
    }
    protected IActionResult DetailView(PersonnelDetailsVm? vm)
    {
        ViewData["Kind"]=Kind;
        return vm==null?NotFound():View("~/Views/Personnel/Details.cshtml",vm);
    }
    protected IActionResult Saved(string number)
    {
        TempData["Success"]="Đã lưu dữ liệu thành công.";
        return RedirectToAction("Details",new {number});
    }
}
