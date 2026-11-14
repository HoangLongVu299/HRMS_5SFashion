using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HRMS.Web.ViewModels;

public class PersonalForm : IValidatableObject
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(50, ErrorMessage = "Họ tên tối đa 50 ký tự.")]
    [Display(Name = "Họ tên")]
    public string? HOTEN { get; set; }
    [Display(Name = "Ngày sinh"), DataType(DataType.Date)]
    public DateTime? NGAYSINH { get; set; }
    [Display(Name = "Giới tính")]
    public bool? GIOITINH { get; set; }
    [StringLength(50, ErrorMessage = "Số điện thoại tối đa 50 ký tự."), Display(Name = "Số điện thoại")]
    public string? DIENTHOAI { get; set; }
    [StringLength(50, ErrorMessage = "CCCD tối đa 50 ký tự."), Display(Name = "CCCD")]
    public string? CCCD { get; set; }
    [StringLength(300, ErrorMessage = "Địa chỉ tối đa 300 ký tự."), Display(Name = "Địa chỉ")]
    public string? DIACHI { get; set; }
    [Display(Name = "Trình độ")] public int? IDTD { get; set; }
    [Display(Name = "Dân tộc")] public int? IDDT { get; set; }
    [Display(Name = "Tôn giáo")] public int? IDTG { get; set; }
    [Display(Name = "Ảnh nhân viên")] public IFormFile? Photo { get; set; }
    [Display(Name = "Gỡ ảnh hiện tại")] public bool RemovePhoto { get; set; }
    [BindNever] public Lookups Lookups { get; set; } = new();
    [BindNever] public int? EmployeeId { get; set; }
    [BindNever] public string? OrganizationSummary { get; set; }
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (NGAYSINH is { } dob && (dob < new DateTime(1753, 1, 1) || dob.Date > DateTime.Today))
            yield return new ValidationResult("Ngày sinh phải từ 01/01/1753 đến hôm nay.", [nameof(NGAYSINH)]);
        if (RemovePhoto && Photo is { Length: > 0 })
            yield return new ValidationResult("Chỉ chọn gỡ ảnh hoặc tải ảnh mới trong một lần lưu.", [nameof(Photo)]);
    }
}

public sealed class EmployeeCreateForm : PersonalForm
{
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var error in base.Validate(validationContext))
            yield return error;

        if (!NGAYSINH.HasValue)
            yield return new ValidationResult("Vui lòng nhập ngày sinh.", [nameof(NGAYSINH)]);
        if (!GIOITINH.HasValue)
            yield return new ValidationResult("Vui lòng chọn giới tính.", [nameof(GIOITINH)]);
        if (string.IsNullOrWhiteSpace(DIENTHOAI))
            yield return new ValidationResult("Vui lòng nhập số điện thoại.", [nameof(DIENTHOAI)]);
        if (string.IsNullOrWhiteSpace(CCCD))
            yield return new ValidationResult("Vui lòng nhập CCCD.", [nameof(CCCD)]);
        if (!IDTD.HasValue)
            yield return new ValidationResult("Vui lòng chọn trình độ.", [nameof(IDTD)]);
    }
}
