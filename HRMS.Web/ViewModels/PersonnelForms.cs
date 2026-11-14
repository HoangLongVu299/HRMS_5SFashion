using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace HRMS.Web.ViewModels;
public class PersonnelOptions
{
    [BindNever, ValidateNever] public Lookups Lookups { get; set; } = new();
    [BindNever, ValidateNever] public List<SelectListItem> Employees { get; set; } = [];
    [BindNever, ValidateNever] public List<SelectListItem> Periods { get; set; } = [];
    [BindNever, ValidateNever] public string? CompanyName { get; set; }
    [BindNever, ValidateNever] public string? EligibilityWarning { get; set; }
    [BindNever, ValidateNever] public List<string> DecisionIssues { get; set; } = [];
}
public class ContractForm : PersonnelOptions, IValidatableObject
{
    [Required(ErrorMessage="Nhập số hợp đồng."), StringLength(50,ErrorMessage="Tối đa 50 ký tự."), Display(Name="Số hợp đồng")]
    public string SOHD { get; set; } = "";
    [Required(ErrorMessage="Chọn nhân viên."), Display(Name="Nhân viên")] public int? MANV { get; set; }
    [Display(Name="Đợt làm việc")] public int? IDDOT { get; set; }
    [Display(Name="Tiếp nhận lại nhân viên đã nghỉ")] public bool Rehire { get; set; }
    [BindNever, ValidateNever] public List<int> FirstEmploymentIds { get; set; } = [];
    [Display(Name="Thời hạn (tháng)")] public int? DurationMonths { get; set; }
    [Display(Name="Công ty tiếp nhận")] public int? RehireCompanyId { get; set; }
    [Display(Name="Phòng ban tiếp nhận")] public int? RehireDepartmentId { get; set; }
    [Display(Name="Bộ phận tiếp nhận")] public int? RehireSectionId { get; set; }
    [Display(Name="Chức vụ tiếp nhận")] public int? RehirePositionId { get; set; }
    [Required(ErrorMessage="Nhập ngày ký."), DataType(DataType.Date), Display(Name="Ngày ký")] public DateTime? NGAYKY { get; set; }
    [Required(ErrorMessage="Nhập ngày bắt đầu."), DataType(DataType.Date), Display(Name="Ngày bắt đầu hợp đồng")] public DateTime? NGAYBATDAU { get; set; }
    [DataType(DataType.Date), Display(Name="Ngày hết hạn theo hợp đồng")] public DateTime? NGAYKETTHUC { get; set; }
    [StringLength(50,ErrorMessage="Tối đa 50 ký tự."), Display(Name="Thời hạn")] public string? THOIHAN { get; set; }
    [Required(ErrorMessage="Vui lòng nhập giá trị."), Range(1,int.MaxValue,ErrorMessage="Lần ký phải từ 1 trở lên."), Display(Name="Lần ký")] public int? LANKY { get; set; } = 1;
    [Range(0,double.MaxValue,ErrorMessage="Hệ số lương không âm."), Display(Name="Hệ số lương")] public double? HESOLUONG { get; set; }
    [Range(0,int.MaxValue,ErrorMessage="Lương cơ bản không âm và phải trong phạm vi số nguyên SQL."), Display(Name="Lương cơ bản")] public int? LUONGCOBAN { get; set; }
    [Display(Name="Nội dung")] public string? NOIDUNG { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (NGAYKY > NGAYBATDAU) yield return new("Ngày ký không được sau ngày bắt đầu.",[nameof(NGAYKY)]);
        if (NGAYKETTHUC < NGAYBATDAU) yield return new("Ngày hết hạn không được trước ngày bắt đầu.",[nameof(NGAYKETTHUC)]);
        foreach(var d in new[]{NGAYKY,NGAYBATDAU,NGAYKETTHUC})
            if(d is {} v && v < new DateTime(1753,1,1)) { yield return new("Ngày phải từ 01/01/1753 để phù hợp kiểu datetime."); break; }
        if(HESOLUONG is {} h && !double.IsFinite(h)) yield return new("Hệ số lương phải là số hữu hạn.");
    }
}
public class TransferForm : PersonnelOptions, IValidatableObject
{
    [Required(ErrorMessage="Nhập số quyết định."), StringLength(50,ErrorMessage="Tối đa 50 ký tự."), Display(Name="Số quyết định")] public string SOQD { get; set; } = "";
    [Required(ErrorMessage="Chọn nhân viên."), Display(Name="Nhân viên")] public int? MANV { get; set; }
    [BindNever, ValidateNever, Display(Name="Đợt làm việc")] public int? IDDOT { get; set; }
    [Required(ErrorMessage="Vui lòng nhập giá trị."), DataType(DataType.Date), Display(Name="Ngày quyết định")] public DateTime? NGAY { get; set; }
    [Required(ErrorMessage="Vui lòng nhập giá trị."), DataType(DataType.Date), Display(Name="Ngày hiệu lực")] public DateOnly? NGAYHIEULUC { get; set; }
    [Required(ErrorMessage="Chọn phòng ban mới."), Display(Name="Phòng ban mới")] public int? MAPB2 { get; set; }
    [Display(Name="Bộ phận sau điều chuyển")] public int? IDBP { get; set; }
    [Required(ErrorMessage="Chọn chức vụ."), Display(Name="Chức vụ sau điều chuyển")] public int? IDCV { get; set; }
    [StringLength(300,ErrorMessage="Tối đa 300 ký tự."), Display(Name="Lý do")] public string? LYDO { get; set; }
    [StringLength(500,ErrorMessage="Tối đa 500 ký tự."), Display(Name="Ghi chú")] public string? GHICHU { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(NGAY is {} d && d < new DateTime(1753,1,1)) yield return new("Ngày quyết định không hợp lệ.");
        if(NGAY is {} q && NGAYHIEULUC is {} e && DateOnly.FromDateTime(q)>e) yield return new("Ngày quyết định không được sau ngày hiệu lực.");
        if(NGAYHIEULUC > DateOnly.FromDateTime(DateTime.Today)) yield return new("Bản demo không nhận điều chuyển tương lai.");
    }
}
public class TerminationForm : PersonnelOptions, IValidatableObject
{
    [Required(ErrorMessage="Nhập số quyết định."), StringLength(50,ErrorMessage="Tối đa 50 ký tự."), Display(Name="Số quyết định")] public string SOQD { get; set; } = "";
    [Required(ErrorMessage="Chọn nhân viên."), Display(Name="Nhân viên")] public int? MANV { get; set; }
    [BindNever, ValidateNever, Display(Name="Đợt làm việc")] public int? IDDOT { get; set; }
    [Required(ErrorMessage="Vui lòng nhập giá trị."), DataType(DataType.Date), Display(Name="Ngày nộp đơn")] public DateTime? NGAYNOPDON { get; set; }
    [Required(ErrorMessage="Vui lòng nhập giá trị."), DataType(DataType.Date), Display(Name="Ngày nghỉ")] public DateTime? NGAYNGHI { get; set; }
    [StringLength(200,ErrorMessage="Tối đa 200 ký tự."), Display(Name="Lý do")] public string? LYDO { get; set; }
    [StringLength(500,ErrorMessage="Tối đa 500 ký tự."), Display(Name="Ghi chú")] public string? GHICHU { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if(NGAYNOPDON>NGAYNGHI) yield return new("Ngày nộp đơn không được sau ngày nghỉ.");
        if(NGAYNGHI?.Date>DateTime.Today) yield return new("Bản demo không nhận ngày nghỉ tương lai.");
        if(NGAYNOPDON < new DateTime(1753,1,1) || NGAYNGHI < new DateTime(1753,1,1)) yield return new("Ngày phải từ 01/01/1753.");
    }
}
public class DecisionNotesForm
{
    [StringLength(300,ErrorMessage="Tối đa 300 ký tự."), Display(Name="Lý do")] public string? LYDO { get; set; }
    [StringLength(500,ErrorMessage="Tối đa 500 ký tự."), Display(Name="Ghi chú")] public string? GHICHU { get; set; }
}
public class PersonnelListVm : PersonnelOptions
{
    [StringLength(100,ErrorMessage="Tối đa 100 ký tự.")] public string? Search { get; set; }
    public int? EmployeeId { get; set; }
    public int? CompanyId { get; set; }
    [DataType(DataType.Date)] public DateTime? From { get; set; }
    [DataType(DataType.Date)] public DateTime? To { get; set; }
    public bool IncludeDeleted { get; set; }
    public int Page { get; set; }=1;
    [BindNever, ValidateNever] public string Title { get; set; }="";
    [BindNever, ValidateNever] public string DateLabel { get; set; }="";
    [BindNever, ValidateNever] public int Total { get; set; }
    public int Pages => Math.Max(1,(Total+9)/10);
    [BindNever, ValidateNever] public List<PersonnelRow> Rows { get; set; }=[];
}
public class PersonnelRow
{
    public string Number { get; set; }="";
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int? PeriodId { get; set; }
    public string? Company { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public DateTime? Date { get; set; }
    public string? Reason { get; set; }
    public DateTime? SignedOn { get; set; }
    public bool Deleted { get; set; }
}
public class PersonnelDetailsVm
{
    public string? PreviousContractContent { get; set; }
    public string Title { get; set; }="";
    public string Number { get; set; }="";
    public int? EmployeeId { get; set; }
    public bool Deleted { get; set; }
    public List<string> Warnings { get; set; }=[];
    public Dictionary<string,string?> Fields { get; set; }=[];
}
