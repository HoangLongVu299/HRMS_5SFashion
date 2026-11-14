using HRMS.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Data;
public static class CatalogRewardMapping
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<RewardDecision>(e => {
            e.ToTable("tb_KHENTHUONG_KYLUAT", "dbo");
            e.HasKey(x => x.SOQUYETDINH).HasName("PK_tb_KHENTHUONG_KYLUAT");
            e.Property(x => x.SOQUYETDINH).HasColumnType("nvarchar(50)").ValueGeneratedNever();
            e.Property(x => x.LYDO).HasColumnType("nvarchar(200)");
            e.Property(x => x.NOIDUNG).HasColumnType("nvarchar(500)");
            foreach (var name in new[] { "NGAY", "TUNGAY", "DENNGAY", "CREATE_DATE", "UPDATE_DATE", "DELETE_DATE" })
                e.Property<DateTime?>(name).HasColumnType("datetime");
            // Existing nullable columns act as optimistic concurrency tokens; no schema change.
            e.Property(x => x.UPDATE_DATE).IsConcurrencyToken();
            e.Property(x => x.DELETE_DATE).IsConcurrencyToken();
            e.Property(x => x.DELETE_BY).IsConcurrencyToken();
            foreach (var name in new[] { "MANV", "LOAI", "NGAY", "LYDO", "NOIDUNG", "TUNGAY", "DENNGAY" })
                e.Property(name).IsConcurrencyToken();
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.MANV)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_KHENTHUONG_KYLUAT_tb_NHANVIEN");
        });
    }
}
