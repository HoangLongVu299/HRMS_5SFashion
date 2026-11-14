using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
namespace HRMS.Web.Controllers;
public class HistoryController(HistoryQueryService service,PersonnelQueryService options) : Controller
{
    [HttpGet] public async Task<IActionResult> Index([FromQuery] HistoryVm vm)
    {
        if(ModelState.IsValid) try {await service.Load(vm);}catch(BusinessException ex){ModelState.AddModelError(ex.Field,ex.Message);}
        else await options.Options(vm);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] HistoryVm vm)
    {
        if (!ModelState.IsValid) return BadRequest("Bộ lọc tra cứu lịch sử công tác không hợp lệ.");

        List<HistoryRow> results;
        try
        {
            results = await service.LoadPrintRows(vm);
        }
        catch (BusinessException ex)
        {
            return BadRequest(ex.Message);
        }

        var rows = results.Select(row => new string?[]
        {
            row.EmployeeId.ToString(), row.Name, $"#{row.PeriodId} · {DisplayText.Value(row.Company)}\n{row.PeriodRange}",
            row.Department, row.Section, row.Position,
            row.HistoryRange is { } range && row.HistoryWarning is { } warning
                ? $"{range}\n{warning}" : row.HistoryRange ?? row.HistoryWarning
        }).ToList();

        var from = DisplayText.Date(vm.From);
        var to = vm.To ?? vm.From;
        var dateFilter = vm.From == to ? $"Ngày {from}" : $"Từ {from} đến {DisplayText.Date(to)}";
        return View("PrintList", new PrintListVm("Kết quả tra cứu lịch sử công tác",
            [new("Mã NV", SingleLine: true), new("Họ và tên"), new("Đợt / Công ty"), new("Phòng ban"),
             new("Bộ phận"), new("Chức vụ"), new("Khoảng giai đoạn gốc")],
            rows, ShowRowNumbers: false)
        {
            Subtitle = dateFilter
        });
    }
}
