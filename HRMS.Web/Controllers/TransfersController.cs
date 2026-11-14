using HRMS.Web.Data;
using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Controllers;
public class TransfersController(HrmsContext db,PersonnelService service,PersonnelQueryService query,ILogger<TransfersController> logger)
    : PersonnelControllerBase(query,logger)
{
    protected override string Kind=>"Transfers";
    [HttpGet] public async Task<IActionResult> Details(string number)=>DetailView(await query.TransferDetails(number));
    [HttpGet] public async Task<IActionResult> Create(int? employeeId)
    {
        var f=new TransferForm {MANV=employeeId,SOQD=await new DocumentNumberService(db).NextTransfer()};await CurrentDecisionOptions(f,f.MANV);return View(f);
    }
    [HttpPost] public async Task<IActionResult> Create(TransferForm f)
    {
        await PrepareNumber(f);
        await CurrentDecisionOptions(f,f.MANV);
        if(ModelState.IsValid) try {return Saved(await service.CreateTransfer(f));}
            catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);}
        await CurrentDecisionOptions(f,f.MANV);return View(f);
    }
    private async Task PrepareNumber(TransferForm f)
    {
        ModelState.Remove(nameof(f.SOQD));
        try {f.SOQD=await new DocumentNumberService(db).NextTransfer(f.NGAY is {} date && date.Year>=1753 ? date : null);}
        catch(BusinessException ex) {f.SOQD="";ModelState.AddModelError("",ex.Message);}
    }
    [HttpGet] public async Task<IActionResult> NextNumber(DateTime? date)
    {
        if(!ModelState.IsValid) return BadRequest(new {message="Ngày quyết định không hợp lệ."});
        try {return Json(new {number=await new DocumentNumberService(db).NextTransfer(date)});}
        catch(BusinessException ex) {return BadRequest(new {message=ex.Message});}
    }
    [HttpGet] public async Task<IActionResult> Edit(string number)
    {
        var e=await db.Transfers.AsNoTracking().SingleOrDefaultAsync(x=>x.SOQD==number);if(e==null)return NotFound();
        if(e.DELETED_DATE!=null || e.DELETED_BY!=null)return DetailView(await query.TransferDetails(number));
        ViewData["Number"]=number;return View("~/Views/Personnel/EditNotes.cshtml",new DecisionNotesForm {LYDO=e.LYDO,GHICHU=e.GHICHU});
    }
    [HttpPost] public async Task<IActionResult> Edit(string number,DecisionNotesForm f)
    {
        if(ModelState.IsValid) try {await service.EditTransferNotes(number,f);return Saved(number);}
            catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);}
        ViewData["Number"]=number;return View("~/Views/Personnel/EditNotes.cshtml",f);
    }
    // No Delete action: deleting only a decision would break the applied history.
}
