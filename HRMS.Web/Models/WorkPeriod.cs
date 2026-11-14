namespace HRMS.Web.Models;

// Ánh xạ từ script SQL gốc; không dùng để tạo database.
public partial class WorkPeriod
{
    public int IDDOT { get; set; }
    public int MANV { get; set; }
    public int MACTY { get; set; }
    public DateOnly NGAYBATDAU { get; set; }
    public DateOnly? NGAYKETTHUC { get; set; }
    public string? GHICHU { get; set; }
    public DateTime CREATED_DATE { get; set; }
    public Employee Employee { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public ICollection<WorkHistory> Histories { get; set; } = new List<WorkHistory>();
}
