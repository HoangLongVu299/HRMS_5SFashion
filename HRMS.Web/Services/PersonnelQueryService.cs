using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Services;
public class PersonnelQueryService(HrmsContext db,LookupService lookup)
{
    public async Task DecisionOptions(PersonnelOptions vm, int? employee, int? period)
    {
        vm.Lookups = await lookup.LoadAsync();
        vm.DecisionIssues = [];
        var choices = await DecisionEligibility.LoadAsync(db, issues: vm.DecisionIssues);
        vm.Employees = choices.DistinctBy(x => x.EmployeeId)
            .Select(x => new SelectListItem(x.EmployeeId+" – "+(x.EmployeeName ?? "Chưa có tên"), x.EmployeeId.ToString())).ToList();
        vm.Periods = choices.Where(x => x.EmployeeId == employee)
            .Select(x => new SelectListItem(DecisionPeriodText(x), x.PeriodId.ToString())).ToList();
        var selected = period==null ? choices.SingleOrDefault(x=>x.EmployeeId==employee)
            : choices.SingleOrDefault(x => x.EmployeeId == employee && x.PeriodId == period);
        vm.CompanyName = selected?.CompanyName;
        vm.EligibilityWarning = selected?.Warning ?? choices.FirstOrDefault(x => x.EmployeeId == employee)?.Warning;
    }
    private static string DecisionPeriodText(DecisionEligibility.Choice x) =>
        $"Đợt #{x.PeriodId} – {x.CompanyName} – {x.Start:dd/MM/yyyy} → Đang làm việc";
    public async Task<object> CurrentDecisionInfo(int employee,DateOnly? date)
    {
        var choices=await DecisionEligibility.LoadAsync(db,employee);
        if(choices.Count!=1) return new {available=false,company="",department="",warning="Nhân viên không còn đợt làm việc hiện tại hợp lệ. Vui lòng tải lại danh sách và chọn nhân viên."};
        var current=choices[0];
        return new {available=true,company=current.CompanyName,
            department=date is {} at ? await OldDepartment(employee,current.PeriodId,at) : "Chọn ngày hiệu lực để xem phòng ban cũ.",
            warning=current.Warning};
    }
    public async Task<IReadOnlyList<object>> DecisionPeriodOptions(int employee)
    {
        var choices = await DecisionEligibility.LoadAsync(db, employee);
        return choices.Select(x => (object)new { id=x.PeriodId, company=x.CompanyName,
            text=DecisionPeriodText(x), warning=x.Warning }).ToList();
    }
    public async Task Options(PersonnelOptions vm,int? employee=null,int? period=null)
    {
        vm.Lookups=await lookup.LoadAsync();
        vm.Employees=await db.Employees.AsNoTracking().OrderBy(x=>x.MANV)
            .Select(x=>new SelectListItem(x.MANV+" – "+(x.HOTEN ?? "Chưa có tên"),x.MANV.ToString())).ToListAsync();
        if(employee!=null)
        {
            var rows=await db.WorkPeriods.AsNoTracking().Include(x=>x.Company).Where(x=>x.MANV==employee).OrderBy(x=>x.NGAYBATDAU).ToListAsync();
            vm.Periods=rows.Select(x=>new SelectListItem($"Đợt #{x.IDDOT} – {x.Company.TENCTY} – {x.NGAYBATDAU:dd/MM/yyyy} → {x.NGAYKETTHUC?.ToString("dd/MM/yyyy") ?? "Chưa kết thúc"}",x.IDDOT.ToString())).ToList();
            vm.CompanyName=rows.SingleOrDefault(x=>x.IDDOT==period)?.Company.TENCTY;
        }
    }
    public async Task<IReadOnlyList<object>> PeriodOptions(int employee)
    {
        var rows=await db.WorkPeriods.AsNoTracking().Include(x=>x.Company).Where(x=>x.MANV==employee).OrderBy(x=>x.NGAYBATDAU).ToListAsync();
        return rows.Select(x=>(object)new {id=x.IDDOT, company=x.Company.TENCTY,
            text=$"Đợt #{x.IDDOT} – {x.Company.TENCTY} – {x.NGAYBATDAU:dd/MM/yyyy} → {x.NGAYKETTHUC?.ToString("dd/MM/yyyy") ?? "Chưa kết thúc"}"}).ToList();
    }
    public async Task<string> OldDepartment(int employee,int period,DateOnly date)
    {
        var rows=await db.WorkHistorys.AsNoTracking().Where(x=>x.IDDOT==period && x.WorkPeriod.MANV==employee && x.TUNGAY<=date && (x.DENNGAY==null || x.DENNGAY>=date))
            .Select(x=>x.Department.TENPB).ToListAsync();
        return rows.Count==1 ? rows[0] ?? "Chưa có tên phòng ban" : "Không xác định duy nhất giai đoạn tại ngày này; cần kiểm tra lịch sử.";
    }
    public async Task List(string kind,PersonnelListVm vm,bool allRows=false)
    {
        if (!allRows) await Options(vm);
        if(vm.From>vm.To) throw new BusinessException("Ngày từ không được sau ngày đến.");
        IQueryable<PersonnelRow> rows;
        if(kind=="Contracts")
        {
            vm.Title="Hợp đồng lao động"; vm.DateLabel="Ngày bắt đầu hợp đồng";
            var q=db.Contracts.AsNoTracking();
            if(!vm.IncludeDeleted) q=q.Where(x=>x.DELETED_DATE==null && x.DELETED_BY==null);
            if(vm.CompanyId!=null) q=q.Where(x=>x.MACTY==vm.CompanyId);
            rows=q.Select(x=>new PersonnelRow {Number=x.SOHD,EmployeeId=x.MANV,EmployeeName=x.Employee==null?null:x.Employee.HOTEN,
                PeriodId=x.IDDOT,Company=x.Company==null?null:x.Company.TENCTY,Date=x.NGAYBATDAU,SignedOn=x.NGAYKY,Deleted=x.DELETED_DATE!=null || x.DELETED_BY!=null});
        }
        else if(kind=="Transfers")
        {
            vm.Title="Điều chuyển nhân viên"; vm.DateLabel="Ngày hiệu lực";
            var q=db.Transfers.AsNoTracking();
            if(!vm.IncludeDeleted) q=q.Where(x=>x.DELETED_DATE==null && x.DELETED_BY==null);
            if(vm.CompanyId!=null) q=q.Where(x=>x.WorkPeriod!=null && x.WorkPeriod.MACTY==vm.CompanyId);
            // DateOnly cannot be converted inside all SQL provider projections. Filter before projection.
            if(vm.From is {} from) { var d=DateOnly.FromDateTime(from); q=q.Where(x=>x.NGAYHIEULUC>=d); }
            if(vm.To is {} to) { var d=DateOnly.FromDateTime(to); q=q.Where(x=>x.NGAYHIEULUC<=d); }
            rows=q.Select(x=>new PersonnelRow {Number=x.SOQD,EmployeeId=x.MANV,EmployeeName=x.Employee==null?null:x.Employee.HOTEN,
                PeriodId=x.IDDOT,Company=x.WorkPeriod==null?null:x.WorkPeriod.Company.TENCTY,
                Department=x.NewDepartment==null?null:x.NewDepartment.TENPB,Deleted=x.DELETED_DATE!=null || x.DELETED_BY!=null});
        }
        else
        {
            vm.Title="Nhân viên thôi việc"; vm.DateLabel="Ngày nghỉ";
            var q=db.Terminations.AsNoTracking();
            if(!vm.IncludeDeleted) q=q.Where(x=>x.DELETED_DATE==null && x.DELETED_BY==null);
            if(vm.CompanyId!=null) q=q.Where(x=>x.WorkPeriod!=null && x.WorkPeriod.MACTY==vm.CompanyId);
            rows=q.Select(x=>new PersonnelRow {Number=x.SOQD,EmployeeId=x.MANV,EmployeeName=x.Employee==null?null:x.Employee.HOTEN,
                PeriodId=x.IDDOT,Company=x.WorkPeriod==null?null:x.WorkPeriod.Company.TENCTY,Date=x.NGAYNGHI,
                Reason=x.LYDO,Deleted=x.DELETED_DATE!=null || x.DELETED_BY!=null});
        }
        if(vm.EmployeeId!=null) rows=rows.Where(x=>x.EmployeeId==vm.EmployeeId);
        if(!string.IsNullOrWhiteSpace(vm.Search))
        {
            var word=vm.Search.Trim(); int.TryParse(word,out var id);
            rows=rows.Where(x=>x.Number.Contains(word) || x.EmployeeId==id || (x.EmployeeName!=null && x.EmployeeName.Contains(word)));
        }
        if(kind!="Transfers")
        {
            if(vm.From is {} from) rows=rows.Where(x=>x.Date>=from.Date);
            if(vm.To is {} to) rows=rows.Where(x=>x.Date!=null && x.Date.Value.Date<=to.Date);
        }
        var ordered=kind=="Contracts"
            ? rows.OrderBy(x=>x.SignedOn==null)
                .ThenBy(x=>x.SignedOn.HasValue ? x.SignedOn.Value.Date : (DateTime?)null)
                .ThenBy(x=>x.Number)
            : rows.OrderBy(x=>x.Number);
        if (allRows)
        {
            vm.Rows = await ordered.ToListAsync();
            vm.Total = vm.Rows.Count;
        }
        else
        {
            vm.Total = await rows.CountAsync();
            vm.Page = Math.Clamp(vm.Page, 1, vm.Pages);
            vm.Rows = await ordered.Skip((vm.Page - 1) * 10).Take(10).ToListAsync();
        }

        if (kind == "Terminations")
        {
            await LoadTerminationHistory(vm.Rows);
            return;
        }

        foreach(var row in vm.Rows)
        {
            WorkHistory? history=null;
            if(kind=="Transfers")
            {
                var decision=await db.Transfers.AsNoTracking().SingleAsync(x=>x.SOQD==row.Number);
                row.Date=decision.NGAYHIEULUC?.ToDateTime(TimeOnly.MinValue);
                var hs=await db.WorkHistorys.AsNoTracking().Include(x=>x.Position).Include(x=>x.Department)
                    .Where(x=>x.IDDOT==row.PeriodId && x.SOQD_DIEUCHUYEN==row.Number).Take(2).ToListAsync();
                if(hs.Count==1) history=hs[0];
            }
            else if(row.PeriodId is {} p && row.Date is {} d)
            {
                var at=DateOnly.FromDateTime(d);
                var hs=await db.WorkHistorys.AsNoTracking().Include(x=>x.Position).Include(x=>x.Department)
                    .Where(x=>x.IDDOT==p && x.TUNGAY<=at && (x.DENNGAY==null || x.DENNGAY>=at)).Take(2).ToListAsync();
                if(hs.Count==1) history=hs[0];
            }
            row.Department??=history?.Department.TENPB;
            row.Position=history?.Position.TENCV;
        }
    }
    private async Task LoadTerminationHistory(List<PersonnelRow> rows)
    {
        var periodIds = rows.Where(row => row.PeriodId.HasValue && row.Date.HasValue)
            .Select(row => row.PeriodId!.Value).Distinct().ToArray();
        if (periodIds.Length == 0) return;

        var histories = (await db.WorkHistorys.AsNoTracking()
            .Where(history => periodIds.Contains(history.IDDOT))
            .Select(history => new { history.IDDOT, history.TUNGAY, history.DENNGAY,
                Department = history.Department.TENPB, Position = history.Position.TENCV })
            .ToListAsync()).ToLookup(history => history.IDDOT);
        foreach (var row in rows)
        {
            if (row.PeriodId is not int periodId || row.Date is not DateTime date) continue;
            var day = DateOnly.FromDateTime(date);
            if (day > DateOnly.MinValue) day = day.AddDays(-1);
            var matches = histories[periodId]
                .Where(history => history.TUNGAY <= day && (history.DENNGAY == null || history.DENNGAY >= day))
                .Take(2).ToArray();
            if (matches.Length != 1) continue;

            row.Department = matches[0].Department;
            row.Position = matches[0].Position;
        }
    }

    private static string? Date(DateTime? d)=>d?.ToString("dd/MM/yyyy");
    private static string? Date(DateOnly? d)=>d?.ToString("dd/MM/yyyy");
    private static PersonnelDetailsVm Base(string title,string number,Employee? e,int? manv,int? period,string? company,bool deleted)
    {
        var vm=new PersonnelDetailsVm {Title=title,Number=number,EmployeeId=manv,Deleted=deleted};
        vm.Fields["Số văn bản"]=number; vm.Fields["Nhân viên"]=$"{manv} – {e?.HOTEN ?? "Chưa có liên kết nhân viên"}";
        vm.Fields["Đợt làm việc"]=period?.ToString(); vm.Fields["Công ty"]=company;
        if(period==null) vm.Warnings.Add("Bản ghi cũ thiếu IDDOT. Chỉ hiển thị dữ liệu đã lưu, không tự gán đợt hoặc suy ra tổ chức hiện tại.");
        if(e==null) vm.Warnings.Add("Bản ghi chưa liên kết với hồ sơ nhân viên.");
        if(deleted) vm.Warnings.Add("Bản ghi có dấu xóa mềm; chỉ xem, không sửa.");
        return vm;
    }
    public async Task<PersonnelDetailsVm?> ContractDetails(string id)
    {
        var e=await db.Contracts.AsNoTracking().Include(x=>x.Employee).Include(x=>x.Company).SingleOrDefaultAsync(x=>x.SOHD==id);
        if(e==null) return null;
        var vm=Base("Hợp đồng lao động",e.SOHD,e.Employee,e.MANV,e.IDDOT,e.Company?.TENCTY,e.DELETED_DATE!=null || e.DELETED_BY!=null);
        vm.Fields["Ngày ký"]=Date(e.NGAYKY); vm.Fields["Ngày bắt đầu"]=Date(e.NGAYBATDAU); vm.Fields["Ngày hết hạn theo hợp đồng"]=Date(e.NGAYKETTHUC);
        vm.Fields["Thời hạn"]=e.THOIHAN; vm.Fields["Lần ký"]=e.LANKY?.ToString(); vm.Fields["Hệ số lương"]=e.HESOLUONG?.ToString();
        vm.Fields["Lương cơ bản"]=e.LUONGCOBAN?.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"));
        var document=await new ContractDocumentService(db).Build(id);
        vm.Fields["Nội dung"]=document?.Body;
        vm.PreviousContractContent=document?.PreviousContent;
        if(document!=null) vm.Warnings.AddRange(document.Warnings);
        vm.Fields["Trạng thái theo thời hạn"]=e.NGAYBATDAU==null?"Chưa đủ dữ liệu":e.NGAYBATDAU.Value.Date>DateTime.Today?"Chưa bắt đầu":e.NGAYKETTHUC?.Date<DateTime.Today?"Đã hết hạn":"Trong thời hạn / chưa ghi nhận hạn kết thúc";
        vm.Warnings.Add("Trạng thái theo thời hạn không phải trạng thái làm việc. Thôi việc không tự sửa ngày hết hạn hợp đồng.");
        await Organization(vm,e.IDDOT,e.NGAYBATDAU is {} start?DateOnly.FromDateTime(start):null);
        Audit(vm,e.CREATED_DATE,e.UPDATED_DATE,e.DELETED_DATE,e.CREATED_BY,e.UPDATED_BY,e.DELETED_BY);
        return vm;
    }
    public async Task<PersonnelDetailsVm?> TransferDetails(string id)
    {
        var e=await db.Transfers.AsNoTracking().Include(x=>x.Employee).Include(x=>x.WorkPeriod).ThenInclude(x=>x!.Company)
            .Include(x=>x.OldDepartment).Include(x=>x.NewDepartment).SingleOrDefaultAsync(x=>x.SOQD==id);
        if(e==null) return null;
        var vm=Base("Điều chuyển nhân viên",e.SOQD,e.Employee,e.MANV,e.IDDOT,e.WorkPeriod?.Company.TENCTY,e.DELETED_DATE!=null || e.DELETED_BY!=null);
        vm.Fields["Ngày quyết định"]=Date(e.NGAY); vm.Fields["Ngày hiệu lực"]=Date(e.NGAYHIEULUC);
        vm.Fields["Phòng ban cũ"]=e.OldDepartment?.TENPB; vm.Fields["Phòng ban mới"]=e.NewDepartment?.TENPB;
        vm.Fields["Lý do"]=e.LYDO; vm.Fields["Ghi chú quyết định"]=e.GHICHU;
        var hs=await db.WorkHistorys.AsNoTracking().Include(x=>x.Section).Include(x=>x.Position).Where(x=>x.IDDOT==e.IDDOT && x.SOQD_DIEUCHUYEN==id).Take(2).ToListAsync();
        if(hs.Count==1) {vm.Fields["Bộ phận sau điều chuyển"]=hs[0].Section?.TENBP; vm.Fields["Chức vụ sau điều chuyển"]=hs[0].Position.TENCV;}
        else vm.Warnings.Add("Không xác định duy nhất lịch sử liên kết quyết định; không lấy chức vụ hiện tại thay cho dữ liệu quá khứ.");
        Audit(vm,e.CREATED_DATE,e.UPDATED_DATE,e.DELETED_DATE,e.CREATED_BY,e.UPDATED_BY,e.DELETED_BY);
        return vm;
    }
    public async Task<PersonnelDetailsVm?> TerminationDetails(string id)
    {
        var e=await db.Terminations.AsNoTracking().Include(x=>x.Employee).Include(x=>x.WorkPeriod).ThenInclude(x=>x!.Company).SingleOrDefaultAsync(x=>x.SOQD==id);
        if(e==null) return null;
        var vm=Base("Nhân viên thôi việc",e.SOQD,e.Employee,e.MANV,e.IDDOT,e.WorkPeriod?.Company.TENCTY,e.DELETED_DATE!=null || e.DELETED_BY!=null);
        vm.Fields["Ngày nộp đơn"]=Date(e.NGAYNOPDON); vm.Fields["Ngày nghỉ (ngày đầu không làm việc)"]=Date(e.NGAYNGHI);
        vm.Fields["Ngày kết thúc đợt"]=Date(e.WorkPeriod?.NGAYKETTHUC); vm.Fields["Lý do"]=e.LYDO; vm.Fields["Ghi chú quyết định"]=e.GHICHU;
        await Organization(vm,e.IDDOT,e.NGAYNGHI is {} leave && leave.Date>DateTime.MinValue?DateOnly.FromDateTime(leave).AddDays(-1):null);
        Audit(vm,e.CREATED_DATE,e.UPDATED_DATE,e.DELETED_DATE,e.CREATED_BY,e.UPDATED_BY,e.DELETED_BY);
        return vm;
    }
    private async Task Organization(PersonnelDetailsVm vm,int? period,DateOnly? at)
    {
        if(period==null || at==null) return;
        var hs=await db.WorkHistorys.AsNoTracking().Include(x=>x.Department).Include(x=>x.Section).Include(x=>x.Position)
            .Where(x=>x.IDDOT==period && x.TUNGAY<=at && (x.DENNGAY==null || x.DENNGAY>=at)).Take(2).ToListAsync();
        if(hs.Count!=1) {vm.Warnings.Add("Thiếu hoặc chồng lấn lịch sử tại ngày văn bản; không suy ra từ hồ sơ hiện tại.");return;}
        vm.Fields["Phòng ban tại thời điểm văn bản"]=hs[0].Department.TENPB;
        vm.Fields["Bộ phận tại thời điểm văn bản"]=hs[0].Section?.TENBP;
        vm.Fields["Chức vụ tại thời điểm văn bản"]=hs[0].Position.TENCV;
    }
    private static void Audit(PersonnelDetailsVm vm,DateTime? created,DateTime? updated,DateTime? deleted,int? cb,int? ub,int? db)
    {
        vm.Fields["Tạo lúc"]=created?.ToString("dd/MM/yyyy HH:mm:ss"); vm.Fields["Sửa lúc"]=updated?.ToString("dd/MM/yyyy HH:mm:ss"); vm.Fields["Xóa lúc"]=deleted?.ToString("dd/MM/yyyy HH:mm:ss");
        vm.Fields["Mã người tạo / sửa / xóa (dữ liệu đã lưu)"]=$"{cb?.ToString() ?? "Chưa ghi nhận"} / {ub?.ToString() ?? "Chưa ghi nhận"} / {db?.ToString() ?? "Chưa ghi nhận"}";
    }
}
