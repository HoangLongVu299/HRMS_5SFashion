using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Controllers;
public class CatalogsController(CatalogService service, ILogger<CatalogsController> logger) : Controller
{
    [HttpGet] public async Task<IActionResult> Index()
    {
        try { return View(await service.Overview()); }
        catch (Exception ex) { logger.LogError(ex,"Đọc danh mục thất bại"); ModelState.AddModelError("","Không thể tải danh mục. Kiểm tra kết nối SQL Server rồi thử lại."); return View(new List<CatalogCard>()); }
    }
    [HttpGet] public async Task<IActionResult> List([FromQuery] CatalogListVm vm)
    {
        if (CatalogDefinitions.Find(vm.Kind)==null) return NotFound();
        try { if(ModelState.IsValid) await service.List(vm); }
        catch(Exception ex) { logger.LogError(ex,"Đọc danh mục thất bại"); ModelState.AddModelError("","Không thể tải dữ liệu. Kiểm tra kết nối SQL Server rồi thử lại."); }
        return View(vm);
    }
    [HttpGet] public async Task<IActionResult> Details(string kind, int? id, [FromQuery] CatalogEmployeeFilter filter)
    {
        var definition = CatalogDefinitions.Find(kind);
        if (definition == null || id == null || (ModelState["id"]?.Errors.Count ?? 0) > 0) return NotFound();
        try {
            var row = await service.Query(kind).SingleOrDefaultAsync(x => x.Id == id.Value);
            if (row == null) return NotFound();
            if (!ModelState.IsValid) {
                Response.StatusCode = 400;
                ViewData["Message"] = "Bộ lọc không hợp lệ. Từ khóa tối đa 100 ký tự; chọn trạng thái hợp lệ và số trang nguyên.";
                return View("~/Views/Shared/Message.cshtml");
            }
            var vm = new CatalogDetailVm(definition, row) { Filter = filter };
            await service.LoadEmployees(vm);
            return View(vm);
        } catch (Exception ex) {
            logger.LogError(ex, "Đọc chi tiết danh mục thất bại");
            Response.StatusCode = 500;
            return View("~/Views/Shared/CatalogRewardError.cshtml");
        }
    }
}
