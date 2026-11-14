using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace HRMS.Web.Services;
public class RewardDecisionService(HrmsContext db)
{
    // Both legacy DELETE_BY and DELETE_DATE mark a record as deleted.
    public static bool Deleted(RewardDecision d) => d.DELETE_DATE.HasValue || d.DELETE_BY.HasValue;
    public static string Version(RewardDecision d) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new {d.SOQUYETDINH,d.MANV,d.LOAI,d.NGAY,d.LYDO,d.NOIDUNG,d.TUNGAY,d.DENNGAY,
            d.CREATE_BY,d.CREATE_DATE,d.UPDATE_BY,d.UPDATE_DATE,d.DELETE_BY,d.DELETE_DATE}))));
    public async Task<List<SelectListItem>> Employees(bool currentOnly=false)
    {
        if (currentOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var employees = await db.Employees.AsNoTracking()
                .OrderBy(x => x.MANV)
                .Select(x => new Employee { MANV = x.MANV, HOTEN = x.HOTEN, MACTY = x.MACTY, DATHOIVIEC = x.DATHOIVIEC })
                .ToListAsync();
            var periods = (await db.WorkPeriods.AsNoTracking().ToListAsync()).ToLookup(x => x.MANV);
            var terms = await db.Terminations.AsNoTracking()
                .Select(x => new TerminationDecision { MANV = x.MANV, IDDOT = x.IDDOT, NGAYNGHI = x.NGAYNGHI })
                .ToListAsync();
            var choices = new List<SelectListItem>();
            foreach (var employee in employees)
            {
                var own = periods[employee.MANV].ToList();
                var ownTerms = terms.Where(t => t.MANV == employee.MANV || own.Any(p => p.IDDOT == t.IDDOT)).ToList();
                var open = own.Where(p => p.NGAYBATDAU <= today && p.NGAYKETTHUC == null &&
                    !ownTerms.Any(t => t.IDDOT == p.IDDOT)).ToList();
                var working = own.Count == 0 ? employee.DATHOIVIEC == false :
                    open.Count == 1 && (employee.DATHOIVIEC != true ||
                        DecisionEligibility.Evaluate(employee, open[0], own, ownTerms, false, today).Allowed);
                if (working)
                    choices.Add(new SelectListItem($"{employee.MANV} – {employee.HOTEN ?? "Chưa có tên"}", employee.MANV.ToString()));
            }
            return choices;
        }
        var rows=await db.Employees.AsNoTracking().OrderBy(x=>x.MANV).Select(x=>new{x.MANV,x.HOTEN,x.DATHOIVIEC}).ToListAsync();
        return rows.Select(x=>new SelectListItem($"{x.MANV} – {DisplayText.Value(x.HOTEN)}" +
            (x.DATHOIVIEC==true?" (Đã thôi việc)":x.DATHOIVIEC==null?" (Chưa xác định trạng thái)":""),x.MANV.ToString())).ToList();
    }
    public Task<RewardDecision?> Get(string number) => db.RewardDecisions.AsNoTracking().Include(x=>x.Employee).SingleOrDefaultAsync(x=>x.SOQUYETDINH==number);
    public IQueryable<RewardDecision> Filtered(RewardListVm vm)
    {
        var q=db.RewardDecisions.AsNoTracking().Include(x=>x.Employee).AsQueryable();
        if(!vm.IncludeDeleted) q=q.Where(x=>x.DELETE_DATE==null && x.DELETE_BY==null);
        var search=vm.Search?.Trim();
        if(!string.IsNullOrEmpty(search)) q=q.Where(x=>x.SOQUYETDINH.Contains(search) ||
            (x.MANV.HasValue && x.MANV.Value.ToString().Contains(search)) || (x.Employee!=null && x.Employee.HOTEN!=null && x.Employee.HOTEN.Contains(search)));
        if(vm.EmployeeId.HasValue) q=q.Where(x=>x.MANV==vm.EmployeeId);
        if(vm.Type=="unknown") q=q.Where(x=>x.LOAI==null || (x.LOAI!=RewardTypes.Reward && x.LOAI!=RewardTypes.Discipline));
        else if(int.TryParse(vm.Type,out var type)) q=q.Where(x=>x.LOAI==type);
        if(vm.From.HasValue) { var from=vm.From.Value.Date; q=q.Where(x=>x.NGAY>=from); }
        if(vm.To.HasValue) { var to=vm.To.Value.Date; q=q.Where(x=>x.NGAY.HasValue && x.NGAY.Value.Date<=to); }
        return q;
    }
    public IOrderedQueryable<RewardDecision> Ordered(RewardListVm vm) =>
        Filtered(vm).OrderByDescending(x => x.NGAY).ThenBy(x => x.SOQUYETDINH);

    public async Task List(RewardListVm vm)
    {
        var q=Ordered(vm);
        vm.Total=await q.CountAsync(); vm.Page=Math.Clamp(vm.Page,1,vm.Pages);
        vm.Rows=await q.Skip((vm.Page-1)*20).Take(20).ToListAsync();
    }
    public static RewardForm Form(RewardDecision d) => new() { IsEdit=true,Number=d.SOQUYETDINH,EmployeeId=d.MANV,Type=d.LOAI,
        DecisionDate=d.NGAY,Reason=d.LYDO,Content=d.NOIDUNG,Version=Version(d) };
    public Task<string> NextNumber(int? type,DateTime? date=null) => new DocumentNumberService(db).NextReward(type,date);
    public async Task<List<(string Field,string Message)>> Validate(RewardForm vm,bool create)
    {
        var errors=new List<(string,string)>();
        if(create && !vm.EmployeeId.HasValue) errors.Add((nameof(vm.EmployeeId),"Vui lòng chọn nhân viên."));
        if(vm.EmployeeId.HasValue && !await db.Employees.AnyAsync(x=>x.MANV==vm.EmployeeId)) errors.Add((nameof(vm.EmployeeId),"Nhân viên không tồn tại."));
        else if(create && vm.EmployeeId.HasValue && !(await Employees(true)).Any(x=>x.Value==vm.EmployeeId.ToString()))
            errors.Add((nameof(vm.EmployeeId),"Chỉ lập quyết định mới cho nhân viên đang làm việc. Vui lòng chọn lại nhân viên."));
        if(!RewardTypes.IsKnown(vm.Type)) errors.Add((nameof(vm.Type),"Vui lòng chọn Khen thưởng hoặc Kỷ luật đã được xác định."));
        if(create && !vm.DecisionDate.HasValue) errors.Add((nameof(vm.DecisionDate),"Vui lòng nhập ngày quyết định."));
        if(create && string.IsNullOrWhiteSpace(vm.Reason)) errors.Add((nameof(vm.Reason),"Vui lòng nhập lý do."));
        return errors;
    }
    public async Task<string> Save(RewardForm vm, string? number)
    {
        var submittedNumber=vm.Number;string savedNumber="";
        try {
        await PersonnelWriteTransaction.Execute(db,async () =>
        {
        if(number==null) vm.Number=await NextNumber(vm.Type,vm.DecisionDate);
        PersonnelService.Validate(vm);
        var create=number==null;
        var errors=await Validate(vm,create);
        if(errors.Count>0) throw new BusinessException(errors[0].Message,errors[0].Field);
        RewardDecision d;
        if(create) { d=new() { SOQUYETDINH=vm.Number.Trim(),CREATE_DATE=DateTime.Now }; db.RewardDecisions.Add(d); }
        else {
            d=await db.RewardDecisions.SingleOrDefaultAsync(x=>x.SOQUYETDINH==number)
                ?? throw new BusinessException("Quyết định không còn tồn tại.");
            if(Deleted(d)) throw new BusinessException("Quyết định đã xóa mềm, không thể sửa.");
            if(vm.Number!=d.SOQUYETDINH) throw new BusinessException("Không được thay đổi số quyết định.",nameof(vm.Number));
            if(vm.Version!=Version(d)) throw new BusinessException("Dữ liệu đã thay đổi từ khi mở form. Mở lại trang Sửa để xem bản mới nhất.");
            d.UPDATE_DATE=DateTime.Now;
            d.UPDATE_BY=null;
        }
        d.MANV=vm.EmployeeId; d.LOAI=vm.Type; d.LYDO=vm.Reason; d.NOIDUNG=vm.Content;
        // Keep legacy time-of-day when editing an unchanged calendar day.
        d.NGAY=KeepTime(d.NGAY,vm.DecisionDate);
        // Removed date-range inputs: new decisions leave them NULL; edits preserve legacy values.
        // No authenticated user: new *_BY and current UPDATE_BY remain NULL; preserve CREATE_BY.
        savedNumber=d.SOQUYETDINH;
        });
        }
        catch {vm.Number=submittedNumber;throw;}
        return savedNumber;
    }
    private static DateTime? KeepTime(DateTime? old,DateTime? value) => old?.Date==value?.Date ? old : value;
    public async Task Delete(string number,string? version)
    {
        var d=await db.RewardDecisions.SingleOrDefaultAsync(x=>x.SOQUYETDINH==number)
            ?? throw new BusinessException("Quyết định không còn tồn tại.");
        if(Deleted(d)) throw new BusinessException("Quyết định đã được xóa mềm trước đó.");
        if(version!=Version(d)) throw new BusinessException("Dữ liệu đã thay đổi. Mở lại trang xác nhận xóa.");
        d.DELETE_DATE=DateTime.Now;
        await db.SaveChangesAsync();
    }
}
