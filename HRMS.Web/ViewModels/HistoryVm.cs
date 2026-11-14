using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace HRMS.Web.ViewModels;
public class HistoryVm : PersonnelOptions
{
    [Required,DataType(DataType.Date),Display(Name="Từ ngày")] public DateOnly? From {get;set;}=DateOnly.FromDateTime(DateTime.Today);
    [DataType(DataType.Date),Display(Name="Đến ngày (trống = từ ngày)")] public DateOnly? To {get;set;}
    [Display(Name="Nhân viên")] public int? EmployeeId {get;set;}
    [Display(Name="Công ty trong đợt")] public int? CompanyId {get;set;}
    [Display(Name="Phòng ban trong lịch sử")] public int? DepartmentId {get;set;}
    [StringLength(100),Display(Name="Tìm mã hoặc họ tên")] public string? Search {get;set;}
    [Display(Name="Làm trọn khoảng trong một đợt")] public bool WholeRange {get;set;}
    public int Page {get;set;}=1;
    [BindNever,ValidateNever] public int Total {get;set;}
    public int Pages=>Math.Max(1,(Total+9)/10);
    [BindNever,ValidateNever] public List<HistoryRow> Rows {get;set;}=[];
}
public class HistoryRow
{
    public int EmployeeId {get;set;}
    public string? Name {get;set;}
    public int PeriodId {get;set;}
    public string? Company {get;set;}
    public int? HistoryId {get;set;}
    public string? Department {get;set;}
    public string? Section {get;set;}
    public string? Position {get;set;}
    public DateOnly PeriodStart {get;set;}
    public DateOnly? PeriodEnd {get;set;}
    public DateOnly? HistoryStart {get;set;}
    public DateOnly? HistoryEnd {get;set;}

    public string PeriodRange => FormatRange(PeriodStart, PeriodEnd);
    public string? HistoryRange => HistoryId.HasValue ? FormatRange(HistoryStart, HistoryEnd) : null;
    public bool HistoryOutsidePeriod => HistoryId.HasValue && (HistoryStart < PeriodStart ||
        (PeriodEnd.HasValue && (!HistoryEnd.HasValue || HistoryEnd > PeriodEnd)));
    public string? HistoryWarning => !HistoryId.HasValue
        ? "Có đợt nhưng thiếu lịch sử phù hợp; chưa xác định phòng ban/chức vụ."
        : HistoryOutsidePeriod ? "Lịch sử ngoài đợt; cần kiểm tra dữ liệu." : null;

    private static string FormatRange(DateOnly? start, DateOnly? end) =>
        $"{DisplayText.Date(start)} → {end?.ToString("dd/MM/yyyy") ?? "Chưa kết thúc"}";
}
