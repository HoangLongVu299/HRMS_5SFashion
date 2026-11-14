namespace HRMS.Web.Models;

// Ánh xạ từ script SQL gốc; không dùng để tạo database.
public partial class WorkHistory
{
    public int IDLS { get; set; }
    public int IDDOT { get; set; }
    public int IDPB { get; set; }
    public int? IDBP { get; set; }
    public int IDCV { get; set; }
    public DateOnly TUNGAY { get; set; }
    public DateOnly? DENNGAY { get; set; }
    public string? SOQD_DIEUCHUYEN { get; set; }
    public string? GHICHU { get; set; }
    public WorkPeriod WorkPeriod { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public Section? Section { get; set; }
    public Position Position { get; set; } = null!;
}
