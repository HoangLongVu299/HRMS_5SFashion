using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
namespace HRMS.Web.ViewModels;
public record CatalogDefinition(string Key, string Title);
public static class CatalogDefinitions
{
    public static readonly IReadOnlyList<CatalogDefinition> All = new[] {
        new CatalogDefinition("departments", "Phòng ban"), new CatalogDefinition("sections", "Bộ phận"),
        new CatalogDefinition("positions", "Chức vụ"), new CatalogDefinition("educations", "Trình độ"),
        new CatalogDefinition("ethnicities", "Dân tộc"), new CatalogDefinition("religions", "Tôn giáo"),
        new CatalogDefinition("companies", "Thông tin công ty") };
    public static CatalogDefinition? Find(string? key) => All.FirstOrDefault(x => x.Key == key);
}
public class CatalogRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
public class CatalogListVm
{
    public string Kind { get; set; } = "";
    [StringLength(100, ErrorMessage="Từ khóa tối đa 100 ký tự.")] public string? Search { get; set; }
    public int Page { get; set; } = 1;
    [BindNever] public int Total { get; set; }
    [BindNever] public int AllCount { get; set; }
    [BindNever] public int Pages => Math.Max(1, (int)Math.Ceiling(Total / 20d));
    [BindNever] public List<CatalogRow> Rows { get; set; } = [];
    [BindNever] public string Title => CatalogDefinitions.Find(Kind)?.Title ?? "Danh mục";
}
public record CatalogCard(CatalogDefinition Definition, int Count);
// The two-argument constructor remains compatible with the company card partial.
public record CatalogDetailVm(CatalogDefinition Definition, CatalogRow Row)
{
    public CatalogEmployeeFilter Filter { get; init; } = new();
    public int RelatedCount { get; set; }
    public int FilteredCount { get; set; }
    public int Pages => Math.Max(1, (int)Math.Ceiling(FilteredCount / 10d));
    public List<CatalogEmployeeRow> Employees { get; set; } = [];
}
public class CatalogEmployeeFilter
{
    [StringLength(100, ErrorMessage="Từ khóa tối đa 100 ký tự.")]
    public string? Search { get; set; }
    [Required(ErrorMessage="Vui lòng chọn trạng thái.")]
    [RegularExpression("^(all|working|left|pending|scheduled|unknown)$", ErrorMessage="Trạng thái không hợp lệ.")]
    public string Status { get; set; } = "all";
    public int Page { get; set; } = 1;
}
public class CatalogEmployeeRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? Section { get; set; }
    public string? Position { get; set; }
    public bool? HasLeft { get; set; }
    public string? EmploymentStatus { get; set; }
    public string StatusLabel => EmploymentStatus is {} status
        ? DisplayText.EmploymentStatus(status) : DisplayText.Status(HasLeft);
}
