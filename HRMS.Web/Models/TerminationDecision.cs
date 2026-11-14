using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HRMS.Web.Models;

[Table("tb_NHANVIEN_THOIVIEC", Schema="dbo")]
public class TerminationDecision
{
    [Key]
    [Column(TypeName="nvarchar(50)")]
    public string SOQD { get; set; } = "";
    [Column(TypeName="int")]
    public int? MANV { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? NGAYNOPDON { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? NGAYNGHI { get; set; }
    [Column(TypeName="nvarchar(200)")]
    public string? LYDO { get; set; }
    [Column(TypeName="nvarchar(500)")]
    public string? GHICHU { get; set; }
    [Column(TypeName="int")]
    public int? CREATED_BY { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? CREATED_DATE { get; set; }
    [Column(TypeName="int")]
    public int? UPDATED_BY { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? UPDATED_DATE { get; set; }
    [Column(TypeName="int")]
    public int? DELETED_BY { get; set; }
    [Column(TypeName="datetime")]
    public DateTime? DELETED_DATE { get; set; }
    [Column(TypeName="int")]
    public int? IDDOT { get; set; }
    public Employee? Employee { get; set; }
    public WorkPeriod? WorkPeriod { get; set; }
}
