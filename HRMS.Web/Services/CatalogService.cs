using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Services;
public class CatalogService(HrmsContext db)
{
    // Fixed allowlist; URL values never become table names or SQL fragments.
    public IQueryable<CatalogRow> Query(string kind) => kind switch {
        "departments" => db.Departments.AsNoTracking().Select(x => new CatalogRow { Id=x.IDPB, Name=x.TENPB }),
        "sections" => db.Sections.AsNoTracking().Select(x => new CatalogRow { Id=x.IDBP, Name=x.TENBP }),
        "positions" => db.Positions.AsNoTracking().Select(x => new CatalogRow { Id=x.IDCV, Name=x.TENCV }),
        "educations" => db.Educations.AsNoTracking().Select(x => new CatalogRow { Id=x.IDTD, Name=x.TENTD }),
        "ethnicities" => db.Ethnicitys.AsNoTracking().Select(x => new CatalogRow { Id=x.ID, Name=x.TENDT }),
        "religions" => db.Religions.AsNoTracking().Select(x => new CatalogRow { Id=x.ID, Name=x.TENTG }),
        "companies" => db.Companys.AsNoTracking().Select(x => new CatalogRow { Id=x.MACTY, Name=x.TENCTY, Phone=x.DIENTHOAI, Email=x.EMAIL, Address=x.DIACHI }),
        _ => throw new ArgumentException("Danh mục không hợp lệ.")
    };
    public IQueryable<Employee> RelatedEmployees(string kind, int id)
    {
        var q = db.Employees.AsNoTracking();
        return kind switch {
            "departments" => q.Where(x => x.IDPB == id),
            "sections" => q.Where(x => x.IDBP == id),
            "positions" => q.Where(x => x.IDCV == id),
            "educations" => q.Where(x => x.IDTD == id),
            "ethnicities" => q.Where(x => x.IDDT == id),
            "religions" => q.Where(x => x.IDTG == id),
            "companies" => q.Where(x => x.MACTY == id),
            _ => throw new ArgumentException("Danh mục không hợp lệ.")
        };
    }
    public IQueryable<CatalogEmployeeRow> FilteredEmployees(string kind, int id, CatalogEmployeeFilter filter)
    {
        var q = RelatedEmployees(kind, id);
        var search = filter.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.MANV.ToString().Contains(search) || (x.HOTEN != null && x.HOTEN.Contains(search)));
        var rows = EmployeeEmployment.WithStatus(db, q, DateOnly.FromDateTime(DateTime.Today));
        if (filter.Status != "all") rows = rows.Where(row => row.Status == filter.Status);
        return rows.Select(row => new CatalogEmployeeRow {
            Id = row.Employee.MANV, Name = row.Employee.HOTEN, HasLeft = row.Employee.DATHOIVIEC,
            EmploymentStatus = row.Status,
            Department = row.Employee.Department == null ? null : row.Employee.Department.TENPB,
            Section = row.Employee.Section == null ? null : row.Employee.Section.TENBP,
            Position = row.Employee.Position == null ? null : row.Employee.Position.TENCV
        });
    }
    public async Task LoadEmployees(CatalogDetailVm vm)
    {
        vm.Filter.Search = vm.Filter.Search?.Trim();
        vm.RelatedCount = await RelatedEmployees(vm.Definition.Key, vm.Row.Id).CountAsync();
        var q = FilteredEmployees(vm.Definition.Key, vm.Row.Id, vm.Filter);
        vm.FilteredCount = await q.CountAsync();
        vm.Filter.Page = Math.Clamp(vm.Filter.Page, 1, vm.Pages);
        vm.Employees = await q.OrderBy(x => x.Id).Skip((vm.Filter.Page - 1) * 10).Take(10).ToListAsync();
    }
    public async Task<List<CatalogCard>> Overview()
    {
        var result = new List<CatalogCard>();
        foreach (var item in CatalogDefinitions.All) result.Add(new(item, await Query(item.Key).CountAsync()));
        return result;
    }
    public async Task List(CatalogListVm vm)
    {
        var q = Query(vm.Kind); vm.AllCount = await q.CountAsync();
        var search = vm.Search?.Trim();
        if (!string.IsNullOrEmpty(search)) q=q.Where(x => x.Id.ToString().Contains(search) || (x.Name != null && x.Name.Contains(search)));
        vm.Total=await q.CountAsync(); vm.Page=Math.Clamp(vm.Page,1,vm.Pages);
        vm.Rows=await q.OrderBy(x=>x.Id).Skip((vm.Page-1)*20).Take(20).ToListAsync();
    }
}
