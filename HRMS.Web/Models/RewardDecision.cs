namespace HRMS.Web.Models;
public class RewardDecision
{
    public string SOQUYETDINH { get; set; } = "";
    public int? MANV { get; set; }
    public int? LOAI { get; set; }
    public DateTime? NGAY { get; set; }
    public string? LYDO { get; set; }
    public string? NOIDUNG { get; set; }
    public DateTime? TUNGAY { get; set; }
    public DateTime? DENNGAY { get; set; }
    public int? CREATE_BY { get; set; }
    public DateTime? CREATE_DATE { get; set; }
    public int? UPDATE_BY { get; set; }
    public DateTime? UPDATE_DATE { get; set; }
    public int? DELETE_BY { get; set; }
    public DateTime? DELETE_DATE { get; set; }
    public Employee? Employee { get; set; }
}
