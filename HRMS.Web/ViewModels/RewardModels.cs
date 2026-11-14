using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using HRMS.Web.Models;
namespace HRMS.Web.ViewModels;
public class RewardForm : IValidatableObject
{
    [Required(ErrorMessage="Vui lòng nhập số quyết định."), StringLength(50,ErrorMessage="Số quyết định tối đa 50 ký tự."), Display(Name="Số quyết định")]
    public string Number { get; set; } = "";
    [Required(ErrorMessage="Vui lòng chọn nhân viên."), Display(Name="Nhân viên")] public int? EmployeeId { get; set; }
    [Display(Name="Loại quyết định")] public int? Type { get; set; }
    [Display(Name="Ngày quyết định"), DataType(DataType.Date)] public DateTime? DecisionDate { get; set; }
    [StringLength(200,ErrorMessage="Lý do tối đa 200 ký tự."), Display(Name="Lý do")] public string? Reason { get; set; }
    [StringLength(500,ErrorMessage="Nội dung tối đa 500 ký tự."), Display(Name="Nội dung")] public string? Content { get; set; }
    public string? Version { get; set; }
    [BindNever] public bool IsEdit { get; set; }
    [BindNever] public List<SelectListItem> Employees { get; set; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach(var item in new[] {(nameof(DecisionDate),DecisionDate)})
            if(item.Item2.HasValue && item.Item2.Value.Year<1753) yield return new("Ngày phải từ 01/01/1753 để lưu vào SQL Server.",[item.Item1]);
    }
}
public class RewardListVm
{
    [StringLength(100,ErrorMessage="Từ khóa tối đa 100 ký tự.")] public string? Search { get; set; }
    public string? Type { get; set; }
    public int? EmployeeId { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date)] public DateTime? To { get; set; }
    public bool IncludeDeleted { get; set; }
    public int Page { get; set; }=1;
    [BindNever] public int Total { get; set; }
    [BindNever] public int Pages => Math.Max(1,(int)Math.Ceiling(Total/20d));
    [BindNever] public List<RewardDecision> Rows { get; set; }=[];
    [BindNever] public List<SelectListItem> Employees { get; set; }=[];
}
public class RewardDeleteVm
{
    public string Number { get; set; }="";
    public string? Version { get; set; }
    [BindNever, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public RewardDecision Decision { get; set; }=null!;
}
