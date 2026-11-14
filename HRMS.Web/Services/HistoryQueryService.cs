using HRMS.Web.Data;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Services;
public class HistoryQueryService(HrmsContext db,PersonnelQueryService options)
{
    // Exposed as IQueryable so SQL translation can be verified without writing data.
    public IQueryable<HistoryRow> Query(HistoryVm vm)
    {
        var start=vm.From ?? throw new BusinessException("Nhập ngày bắt đầu.");
        var end=vm.To ?? start;
        if(end<start)throw new BusinessException("Ngày đến không được trước ngày từ.");
        var periods=db.WorkPeriods.AsNoTracking().Where(x=>x.NGAYBATDAU<=end && (x.NGAYKETTHUC==null || x.NGAYKETTHUC>=start));
        if(vm.WholeRange) periods=periods.Where(x=>x.NGAYBATDAU<=start && (x.NGAYKETTHUC==null || x.NGAYKETTHUC>=end));
        if(vm.EmployeeId is {} employee) periods=periods.Where(x=>x.MANV==employee);
        if(vm.CompanyId is {} company) periods=periods.Where(x=>x.MACTY==company);
        if(!string.IsNullOrWhiteSpace(vm.Search))
        {
            var word=vm.Search.Trim();int.TryParse(word,out var id);
            periods=periods.Where(x=>x.MANV==id || (x.Employee.HOTEN!=null && x.Employee.HOTEN.Contains(word)));
        }
        var histories=db.WorkHistorys.AsNoTracking().Where(x=>x.TUNGAY<=end && (x.DENNGAY==null || x.DENNGAY>=start));
        var result=from p in periods
                   join h in histories on p.IDDOT equals h.IDDOT into historyGroup
                   from h in historyGroup.DefaultIfEmpty()
                   where vm.DepartmentId==null || (h!=null && h.IDPB==vm.DepartmentId)
                   select new HistoryRow {
                       EmployeeId=p.MANV,Name=p.Employee.HOTEN,PeriodId=p.IDDOT,Company=p.Company.TENCTY,
                       HistoryId=h==null?null:h.IDLS,Department=h==null?null:h.Department.TENPB,
                       Section=h==null || h.Section==null?null:h.Section.TENBP,Position=h==null?null:h.Position.TENCV,
                       PeriodStart=p.NGAYBATDAU,PeriodEnd=p.NGAYKETTHUC,HistoryStart=h==null?null:h.TUNGAY,HistoryEnd=h==null?null:h.DENNGAY
                   };
        // Never filter DATHOIVIEC or read current employee IDPB/IDCV for historical organization.
        return result;
    }
    public async Task Load(HistoryVm vm)
    {
        await options.Options(vm);
        var rows=Query(vm);vm.Total=await rows.CountAsync();vm.Page=Math.Clamp(vm.Page,1,vm.Pages);
        vm.Rows=await OrderRows(rows)
            .Skip((vm.Page-1)*10).Take(10).ToListAsync();
    }

    public Task<List<HistoryRow>> LoadPrintRows(HistoryVm vm) =>
        OrderRows(Query(vm)).ToListAsync();

    private static IOrderedQueryable<HistoryRow> OrderRows(IQueryable<HistoryRow> rows) =>
        rows.OrderBy(row => row.EmployeeId).ThenBy(row => row.PeriodStart)
            .ThenBy(row => row.HistoryStart).ThenBy(row => row.HistoryId).ThenBy(row => row.PeriodId);
}
