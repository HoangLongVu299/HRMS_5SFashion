using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HRMS.Web.Models;

[Table("tb_HOPDONG", Schema="dbo")]
public class LaborContract
{
    [Key]
    [Column(TypeName="nvarchar(50)")]
    public string SOHD { get; set; } = "";
    [Column(TypeName="datetime")]
    public DateTime? NGAYBATDAU { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? NGAYKETTHUC { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? NGAYKY { get; set; }
    [Column(TypeName="nvarchar(max)")]
    public string? NOIDUNG { get; set; }
    [Column(TypeName="int")]
    public int? LANKY { get; set; }
    [Column(TypeName="nvarchar(50)")]
    public string? THOIHAN { get; set; }
    [Column(TypeName="float")]
    public double? HESOLUONG { get; set; }
    [Column(TypeName="int")]
    public int? LUONGCOBAN { get; set; }
    [Column(TypeName="int")]
    public int? MANV { get; set; }
    [Column(TypeName="int")]
    public int? MACTY { get; set; }
    [Column(TypeName="int")]
    public int? DELETED_BY { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? DELETED_DATE { get; set; }
    [Column(TypeName="int")]
    public int? UPDATED_BY { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? UPDATED_DATE { get; set; }
    [Column(TypeName="int")]
    public int? CREATED_BY { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? CREATED_DATE { get; set; }
    [Column(TypeName="int")]
    public int? IDDOT { get; set; }
    public Employee? Employee { get; set; }
    public WorkPeriod? WorkPeriod { get; set; }
    public Company? Company { get; set; }
}
