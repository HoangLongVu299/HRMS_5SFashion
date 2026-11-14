using HRMS.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HRMS.Web.ViewModels;
public class Lookups
{
    public List<SelectListItem> Companies { get; set; } = [];
    public List<SelectListItem> Departments { get; set; } = [];
    public List<SelectListItem> Sections { get; set; } = [];
    public List<SelectListItem> Positions { get; set; } = [];
    public List<SelectListItem> Educations { get; set; } = [];
    public List<SelectListItem> Ethnicities { get; set; } = [];
    public List<SelectListItem> Religions { get; set; } = [];
}
public class EmployeeListVm
{
    [StringLength(100, ErrorMessage = "Từ khóa tối đa 100 ký tự.")] public string? Search { get; set; }
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    [RegularExpression("^(working|left|pending|scheduled|unknown)?$", ErrorMessage = "Bộ lọc trạng thái không hợp lệ.")]
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    [BindNever] public int PageSize { get; set; } = 10;
    [BindNever] public int Total { get; set; }
    [BindNever] public int Pages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
    [BindNever] public List<EmployeeRow> Rows { get; set; } = [];
    [BindNever] public Lookups Lookups { get; set; } = new();
}
public class EmployeeRow
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Identity { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Section { get; set; }
    public string? Position { get; set; }
    public string Status { get; set; } = "unknown";
    public bool MissingHistory { get; set; }
}
public class EmployeeDetailsVm
{
    public Employee Employee { get; set; } = null!;
    public string Status { get; set; } = "unknown";
    public List<WorkPeriod> Periods { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
public static class DisplayText
{
    public static string Value(string? value) => string.IsNullOrWhiteSpace(value) ? "Chưa có thông tin" : value;
    // SQL chỉ quy định bit; đây là quy ước trình bày của ứng dụng, không suy ra từ SQL.
    public static string Gender(bool? value) => value switch { true => "Nam", false => "Nữ", null => "Chưa xác định" };
    public static string Status(bool? value) => value switch { true => "Đã thôi việc", false => "Đang làm việc", null => "Chưa xác định" };
    public static string EmploymentStatus(string status) => status switch
    {
        "pending" => "Chờ tiếp nhận",
        "scheduled" => "Chờ vào làm",
        "working" => Status(false),
        "left" => Status(true),
        _ => Status(null)
    };
    public static string EmploymentBadge(string status) => status switch
    {
        "working" => "text-bg-success",
        "left" => "text-bg-secondary",
        _ => "text-bg-warning"
    };
    public static string Date(DateOnly? value) => value?.ToString("dd/MM/yyyy") ?? "Chưa xác định";
}
