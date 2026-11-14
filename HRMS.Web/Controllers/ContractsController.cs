using HRMS.Web.Data;
using HRMS.Web.Services;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Controllers;
public class ContractsController(HrmsContext db,PersonnelService service,PersonnelQueryService query,ILogger<ContractsController> logger)
    : PersonnelControllerBase(query,logger)
{
    protected override string Kind=>"Contracts";
    [HttpGet] public async Task<IActionResult> Print(string number)
    {
        if(string.IsNullOrWhiteSpace(number)) return BadRequest();
        var document=await new ContractDocumentService(db).Build(number);
        return document==null?NotFound():View(document);
    }
    [HttpGet] public async Task<IActionResult> PrintForEmployee(int employeeId)
    {
        var list=await new ContractDocumentService(db).ForEmployee(employeeId);
        if(list==null) return NotFound();
        if(list.Contracts.Count==1) return RedirectToAction(nameof(Print),new {number=list.Contracts[0].Number});
        return View("PrintSelection",list);
    }
    [HttpGet] public async Task<IActionResult> Details(string number)=>DetailView(await query.ContractDetails(number));
    [HttpGet] public async Task<IActionResult> Create(int? employeeId,bool rehire=false)
    {
        var f=new ContractForm {MANV=employeeId,Rehire=rehire}; await CreateOptions(f); return View("Form",f);
    }
    [HttpPost] public async Task<IActionResult> Create(ContractForm f)
    {
        // A stale/forged browser period must never choose which employment receives a new contract.
        f.IDDOT=null; ModelState.Remove(nameof(f.IDDOT));
        // These fields are calculated on save, regardless of values sent by the client.
        foreach (var field in new[] { nameof(f.NGAYKETTHUC), nameof(f.THOIHAN), nameof(f.LANKY) })
            ModelState.Remove(field);
        await CreateOptions(f);
        if(ModelState.IsValid) try {return Saved(await service.SaveContract(null,f));}
            catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);await CreateOptions(f);}
        return View("Form",f);
    }
    private async Task CreateOptions(ContractForm f)
    {
        f.Lookups = await new LookupService(db).LoadAsync();
        if(string.IsNullOrWhiteSpace(f.NOIDUNG)) f.NOIDUNG=ContractDocumentTemplate.DefaultBody;
        f.DecisionIssues=[];
        var choices=await ContractEmployeeOptions.LoadForCreate(db, f.Rehire, f.DecisionIssues);
        f.FirstEmploymentIds=choices.Where(x=>x.FirstEmployment).Select(x=>x.Id).ToList();
        f.Employees=choices.Select(x=>new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.Text,x.Id.ToString())).ToList();
        if(f.MANV!=null && !choices.Any(x=>x.Id==f.MANV))
        {
            f.MANV=null;ModelState.Remove(nameof(f.MANV));
            ModelState.AddModelError(nameof(f.MANV),"Nhân viên không thuộc nhóm đang chọn. Vui lòng chọn lại nhân viên.");
        }
        ModelState.Remove(nameof(f.SOHD));
        try {f.SOHD=await new DocumentNumberService(db).NextContract(f.NGAYKY is {} date && date.Year>=1753 ? date : null);}
        catch(BusinessException ex) {f.SOHD="";ModelState.AddModelError("",ex.Message);}
        if(f.RehireCompanyId==null && f.Lookups.Companies.Count==1)
            f.RehireCompanyId=int.Parse(f.Lookups.Companies[0].Value);
    }
    [HttpGet] public async Task<IActionResult> EmployeeChoices(bool rehire=false)
    {
        var issues=new List<string>();var choices=await ContractEmployeeOptions.LoadForCreate(db, rehire, issues);
        return Json(new {choices,issues});
    }
    [HttpGet] public async Task<IActionResult> NextSigning(int employeeId)
    {
        if (!ModelState.IsValid || employeeId <= 0) return BadRequest();
        if (!await db.Employees.AnyAsync(x => x.MANV == employeeId)) return NotFound();
        try { return Json(new { signing = await service.NextContractSigning(employeeId) }); }
        catch (BusinessException) { return BadRequest(); }
    }
    [HttpGet] public async Task<IActionResult> NextNumber(DateTime? date)
    {
        if(!ModelState.IsValid) return BadRequest(new {message="Ngày ký không hợp lệ."});
        try {return Json(new {number=await new DocumentNumberService(db).NextContract(date)});}
        catch(BusinessException ex) {return BadRequest(new {message=ex.Message});}
    }
    [HttpGet] public async Task<IActionResult> Edit(string number)
    {
        var e=await db.Contracts.AsNoTracking().SingleOrDefaultAsync(x=>x.SOHD==number);if(e==null)return NotFound();
        if(e.DELETED_DATE!=null || e.DELETED_BY!=null)return DetailView(await query.ContractDetails(number));
        if(!ContractContentText.TryRead(e.NOIDUNG,out var content)) return DetailView(await query.ContractDetails(number));
        var f=new ContractForm {SOHD=e.SOHD,MANV=e.MANV,IDDOT=e.IDDOT,NGAYKY=e.NGAYKY,NGAYBATDAU=e.NGAYBATDAU,NGAYKETTHUC=e.NGAYKETTHUC,
            LANKY=e.LANKY,THOIHAN=e.THOIHAN,NOIDUNG=ContractDocumentTemplate.Editable(e.NOIDUNG),HESOLUONG=e.HESOLUONG,LUONGCOBAN=e.LUONGCOBAN};
        if(!ContractDocumentTemplate.IsTemplate(e.NOIDUNG)) ViewData["PreviousContractContent"]=content;
        ViewData["Original"]=number; await query.Options(f,f.MANV,f.IDDOT);return View("Form",f);
    }
    [HttpPost] public async Task<IActionResult> Edit(string number,ContractForm f)
    {
        if(ModelState.IsValid) try {await service.SaveContract(number,f);return Saved(number);}
            catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);}
        ViewData["Original"]=number;await query.Options(f,f.MANV,f.IDDOT);return View("Form",f);
    }
    [HttpGet] public async Task<IActionResult> Delete(string number)
    {
        var vm=await query.ContractDetails(number);return vm==null?NotFound():View(vm);
    }
    [HttpPost,ActionName("Delete")] public async Task<IActionResult> DeleteConfirmed(string number)
    {
        try {await service.DeleteContract(number);TempData["Success"]="Đã xóa mềm hợp đồng. Đợt và lịch sử được giữ nguyên.";return RedirectToAction(nameof(Index));}
        catch(Exception ex) when(ex is BusinessException or DbUpdateException or SqlException){Error(ex);}
        var vm=await query.ContractDetails(number);return vm==null?NotFound():View("Delete",vm);
    }
}
