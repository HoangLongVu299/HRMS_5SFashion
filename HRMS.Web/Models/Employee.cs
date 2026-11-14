namespace HRMS.Web.Models;

// Ánh xạ từ script SQL gốc; không dùng để tạo database.
public partial class Employee
{
    public int MANV { get; set; }
    public string? HOTEN { get; set; }
    public bool? GIOITINH { get; set; }
    public string? DIENTHOAI { get; set; }
    public string? CCCD { get; set; }
    public string? DIACHI { get; set; }
    public byte[]? HINHANH { get; set; }
    public int? IDPB { get; set; }
    public int? IDBP { get; set; }
    public int? IDCV { get; set; }
    public int? IDTD { get; set; }
    public int? IDDT { get; set; }
    public int? IDTG { get; set; }
    public int? MACTY { get; set; }
    public DateTime? NGAYSINH { get; set; }
    public bool? DATHOIVIEC { get; set; }
    public Department? Department { get; set; }
    public Section? Section { get; set; }
    public Position? Position { get; set; }
    public Education? Education { get; set; }
    public Ethnicity? Ethnicity { get; set; }
    public Religion? Religion { get; set; }
    public Company? Company { get; set; }
    public ICollection<WorkPeriod> WorkPeriods { get; set; } = new List<WorkPeriod>();
}
