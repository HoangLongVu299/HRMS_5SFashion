namespace HRMS.Web.Models;

// Ánh xạ từ script SQL gốc; không dùng để tạo database.
public partial class Company
{
    public int MACTY { get; set; }
    public string? TENCTY { get; set; }
    public string? DIENTHOAI { get; set; }
    public string? EMAIL { get; set; }
    public string? DIACHI { get; set; }
}
