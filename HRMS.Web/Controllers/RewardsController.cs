using HRMS.Web.Models;
using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Controllers;
public class RewardsController(RewardDecisionService service,ILogger<RewardsController> logger) : Controller
{
    [HttpGet] public async Task<IActionResult> Index([FromQuery] RewardListVm vm)
    {
        ValidateFilters(vm);
        try { vm.Employees=await service.Employees(); if(ModelState.IsValid) await service.List(vm); }
        catch(Exception ex) { Error(ex); }
        return View(vm);
    }
    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] RewardListVm vm)
    {
        ValidateFilters(vm);
        if (!ModelState.IsValid) return BadRequest("Bộ lọc khen thưởng – kỷ luật không hợp lệ.");

        vm.IncludeDeleted = false;
        var decisions = await service.Ordered(vm).ToListAsync();
        int.TryParse(vm.Type, out var decisionType);
        var title = decisionType switch
        {
            RewardTypes.Reward => "Danh sách quyết định khen thưởng",
            RewardTypes.Discipline => "Danh sách quyết định kỷ luật",
            _ => "Danh sách quyết định khen thưởng – kỷ luật"
        };
        return View("PrintList", new PrintListVm(title,
            [new("Số quyết định", SingleLine: true), new("Mã NV", SingleLine: true), new("Họ và tên"),
             new("Loại quyết định"), new("Ngày quyết định", SingleLine: true), new("Lý do"), new("Nội dung")],
            decisions.Select<RewardDecision, string?[]>(row =>
                [row.SOQUYETDINH, row.MANV?.ToString(), row.Employee?.HOTEN, RewardTypes.Label(row.LOAI),
                 row.NGAY?.ToString("dd/MM/yyyy"), row.LYDO, row.NOIDUNG]).ToList()));
    }

    private void ValidateFilters(RewardListVm vm)
    {
        if(vm.Type!="unknown" && !string.IsNullOrEmpty(vm.Type) && !RewardTypes.Options().Any(x=>x.Value==vm.Type))
            ModelState.AddModelError(nameof(vm.Type),"Bộ lọc loại không hợp lệ.");
        if(vm.From>vm.To) ModelState.AddModelError(nameof(vm.To),"Ngày kết thúc lọc không được trước ngày bắt đầu.");
    }

    [HttpGet] public async Task<IActionResult> Details(string number)
    {
        try { var d=await service.Get(number); return d==null?NotFound():View(d); }
        catch(Exception ex) { Error(ex); return View("~/Views/Shared/CatalogRewardError.cshtml"); }
    }
    [HttpGet] public async Task<IActionResult> Create(int? employeeId)
    {
        var vm=new RewardForm { EmployeeId=employeeId };
        await Options(vm); return View("Form",vm);
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(RewardForm vm)
    {
        vm.IsEdit=false;
        await Options(vm);
        try {
            foreach(var e in await service.Validate(vm,true)) ModelState.AddModelError(e.Field,e.Message);
            if(ModelState.IsValid) return Saved(await service.Save(vm,null));
        } catch(Exception ex) { Error(ex); }
        await Options(vm); return View("Form",vm);
    }
    [HttpGet] public async Task<IActionResult> NextNumber(int? type,DateTime? date)
    {
        if(!ModelState.IsValid) return BadRequest(new {message="Loại hoặc ngày quyết định không hợp lệ."});
        try {return Json(new {number=await service.NextNumber(type,date)});}
        catch(BusinessException ex) {return BadRequest(new {message=ex.Message});}
    }
    [HttpGet] public async Task<IActionResult> Edit(string number)
    {
        try {
            var d=await service.Get(number); if(d==null) return NotFound();
            if(RewardDecisionService.Deleted(d)) { TempData["Notice"]="Quyết định đã xóa mềm, chỉ được xem.";return RedirectToAction(nameof(Details),new{number}); }
            var vm=RewardDecisionService.Form(d);await Options(vm);return View("Form",vm);
        } catch(Exception ex) { Error(ex);return View("~/Views/Shared/CatalogRewardError.cshtml"); }
    }
    [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit([FromQuery(Name="key")] string number,RewardForm vm)
    {
        vm.IsEdit=true;
        // Query-string key is immutable and checked again against the loaded entity in the service.
        if(number!=vm.Number) ModelState.AddModelError(nameof(vm.Number),"Không được thay đổi số quyết định.");
        try {
            foreach(var e in await service.Validate(vm,false)) ModelState.AddModelError(e.Field,e.Message);
            if(ModelState.IsValid) return Saved(await service.Save(vm,number));
        } catch(Exception ex) { Error(ex); }
        ViewData["OriginalNumber"]=number;
        await Options(vm);return View("Form",vm);
    }
    [HttpGet] public async Task<IActionResult> Delete(string number)
    {
        try {
            var d=await service.Get(number);if(d==null) return NotFound();
            if(RewardDecisionService.Deleted(d)) return RedirectToAction(nameof(Details),new{number});
            return View(new RewardDeleteVm { Number=d.SOQUYETDINH,Version=RewardDecisionService.Version(d),Decision=d });
        } catch(Exception ex) { Error(ex);return View("~/Views/Shared/CatalogRewardError.cshtml"); }
    }
    [HttpPost,ValidateAntiForgeryToken,ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed(RewardDeleteVm vm)
    {
        try {
            if(ModelState.IsValid) { await service.Delete(vm.Number,vm.Version);TempData["Success"]="Đã xóa mềm quyết định. Có thể xem lại bằng bộ lọc bản ghi đã xóa.";return RedirectToAction(nameof(Index)); }
        } catch(Exception ex) { Error(ex); }
        try { var d=await service.Get(vm.Number); if(d==null) return NotFound();vm.Decision=d;return View("Delete",vm); }
        catch(Exception ex) { Error(ex);return View("~/Views/Shared/CatalogRewardError.cshtml"); }
    }
    private async Task Options(RewardForm vm)
    {
        try {
            vm.Employees=await service.Employees(!vm.IsEdit);
            if(!vm.IsEdit)
            {
                if(vm.EmployeeId!=null && !vm.Employees.Any(x=>x.Value==vm.EmployeeId.ToString()))
                {
                    vm.EmployeeId=null;ModelState.Remove(nameof(vm.EmployeeId));
                    ModelState.AddModelError(nameof(vm.EmployeeId),"Chọn nhân viên đang làm việc trong danh sách hiện tại.");
                }
                ModelState.Remove(nameof(vm.Number));
                vm.Number=RewardTypes.IsKnown(vm.Type)
                    ? await service.NextNumber(vm.Type,vm.DecisionDate is {} date && date.Year>=1753 ? date : null) : "";
            }
        }
        catch(Exception ex) { Error(ex); }
    }
    private IActionResult Saved(string number) {TempData["Success"]="Đã lưu quyết định.";return RedirectToAction(nameof(Details),new{number});}
    private void Error(Exception ex)
    {
        if(ex is BusinessException b) {ModelState.AddModelError(b.Field,b.Message);return;}
        logger.LogError(ex,"Không thể xử lý quyết định khen thưởng / kỷ luật.");
        var sql=ex as SqlException ?? ex.InnerException as SqlException;
        ModelState.AddModelError("",ex is DbUpdateConcurrencyException ? "Dữ liệu vừa được người khác sửa hoặc xóa. Mở lại trang để kiểm tra trước khi lưu." : sql?.Number switch {
            2601 or 2627=>"Số quyết định đã tồn tại, kể cả quyết định đã xóa mềm. Kiểm tra danh sách trước khi gửi lại.",
            547=>"Nhân viên hoặc dữ liệu liên kết không còn hợp lệ. Tải lại danh sách và kiểm tra.",
            _=>"Không thể tải hoặc xác nhận lưu dữ liệu. Kiểm tra kết nối SQL Server và danh sách quyết định trước khi thử lại."
        });
    }
}
