using HRMS.Web.Data;
using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Controllers;
public class TerminationsController(HrmsContext db,PersonnelService service,PersonnelQueryService query,ILogger<TerminationsController> logger)
    : PersonnelControllerBase(query,logger)
{
    protected override string Kind=>"Terminations";
    [HttpGet]
    public async Task<IActionResult> PrintList([FromQuery] PersonnelListVm vm)
    {
        if (!ModelState.IsValid || vm.From > vm.To)
            return BadRequest("Bộ lọc nhân viên thôi việc không hợp lệ.");

        vm.IncludeDeleted = false;
        await query.List(Kind, vm, allRows: true);
        return View("PrintList", new PrintListVm("Danh sách nhân viên thôi việc",
            [new("Số quyết định", SingleLine: true), new("Mã NV", SingleLine: true), new("Họ và tên"),
             new("Phòng ban trước khi nghỉ"), new("Chức vụ trước khi nghỉ"), new("Ngày nghỉ", SingleLine: true),
             new("Lý do")],
            vm.Rows.Select<PersonnelRow, string?[]>(row =>
                [row.Number, row.EmployeeId?.ToString(), row.EmployeeName, row.Department,
                 row.Position, row.Date?.ToString("dd/MM/yyyy"), row.Reason]).ToList()));
    }

    [HttpGet] public async Task<IActionResult> Details(string number)=>DetailView(await query.TerminationDetails(number));
    [HttpGet] public async Task<IActionResult> Create(int? employeeId)
    {
        var f=new TerminationForm {MANV=employeeId};await PrepareNumber(f);await CurrentDecisionOptions(f,f.MANV);return View(f);
    }
    [HttpPost] public async Task<IActionResult> Create(TerminationForm f)
    {
        await PrepareNumber(f);await CurrentDecisionOptions(f,f.MANV);
        if(ModelState.IsValid) try {return Saved(await service.CreateTermination(f));}
            catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);}
        await CurrentDecisionOptions(f,f.MANV);return View(f);
    }
    private async Task PrepareNumber(TerminationForm f)
    {
        ModelState.Remove(nameof(f.SOQD));
        try {f.SOQD=await new DocumentNumberService(db).NextTermination(f.NGAYNGHI is {} date && date.Year>=1753 ? date : null);}
        catch(BusinessException ex) {f.SOQD="";ModelState.AddModelError("",ex.Message);}
    }
    [HttpGet] public async Task<IActionResult> NextNumber(DateTime? date)
    {
        if(!ModelState.IsValid) return BadRequest(new {message="Ngày nghỉ không hợp lệ."});
        try {return Json(new {number=await new DocumentNumberService(db).NextTermination(date)});}
        catch(BusinessException ex) {return BadRequest(new {message=ex.Message});}
    }
    [HttpGet] public async Task<IActionResult> Edit(string number)
    {
        var e=await db.Terminations.AsNoTracking().SingleOrDefaultAsync(x=>x.SOQD==number);if(e==null)return NotFound();
        if(e.DELETED_DATE!=null || e.DELETED_BY!=null)return DetailView(await query.TerminationDetails(number));
        ViewData["Number"]=number;return View("~/Views/Personnel/EditNotes.cshtml",new DecisionNotesForm {LYDO=e.LYDO,GHICHU=e.GHICHU});
    }
    [HttpPost] public async Task<IActionResult> Edit(string number,DecisionNotesForm f)
    {
        if(ModelState.IsValid) try {await service.EditTerminationNotes(number,f);return Saved(number);}
            catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);}
        ViewData["Number"]=number;return View("~/Views/Personnel/EditNotes.cshtml",f);
    }
    // No Delete action: deleting only a decision would break the applied history.
}
