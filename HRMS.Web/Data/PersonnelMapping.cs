using HRMS.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Data;
public static class PersonnelMapping
{
    public static void Configure(ModelBuilder b)
    {
        b.Entity<WorkPeriod>().ToTable("tb_DOTLAMVIEC", "dbo", t=>t.UseSqlOutputClause(false));
        b.Entity<WorkHistory>().ToTable("tb_LICHSU_CONGTAC", "dbo", t=>t.UseSqlOutputClause(false));
        b.Entity<WorkPeriod>().HasAlternateKey(x => new { x.IDDOT, x.MANV }).HasName("UQ_DOT_NHANVIEN");
        b.Entity<WorkPeriod>().HasAlternateKey(x => new { x.IDDOT, x.MANV, x.MACTY }).HasName("UQ_DOT_NHANVIEN_CONGTY");
        b.Entity<LaborContract>().HasOne(x=>x.Employee).WithMany().HasForeignKey(x=>x.MANV).OnDelete(DeleteBehavior.NoAction);
        b.Entity<LaborContract>().HasOne(x=>x.Company).WithMany().HasForeignKey(x=>x.MACTY).OnDelete(DeleteBehavior.NoAction);
        b.Entity<LaborContract>().HasOne(x=>x.WorkPeriod).WithMany().HasForeignKey(x=>new {x.IDDOT,x.MANV,x.MACTY})
            .HasPrincipalKey(x=>new {x.IDDOT,x.MANV,x.MACTY}).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TransferDecision>().HasOne(x=>x.Employee).WithMany().HasForeignKey(x=>x.MANV).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TransferDecision>().HasOne(x=>x.WorkPeriod).WithMany().HasForeignKey(x=>new {x.IDDOT,x.MANV})
            .HasPrincipalKey(x=>new {x.IDDOT,x.MANV}).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TransferDecision>().HasOne(x=>x.OldDepartment).WithMany().HasForeignKey(x=>x.MAPB).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TransferDecision>().HasOne(x=>x.NewDepartment).WithMany().HasForeignKey(x=>x.MAPB2).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TerminationDecision>().HasOne(x=>x.Employee).WithMany().HasForeignKey(x=>x.MANV).OnDelete(DeleteBehavior.NoAction);
        b.Entity<TerminationDecision>().HasOne(x=>x.WorkPeriod).WithMany().HasForeignKey(x=>new {x.IDDOT,x.MANV})
            .HasPrincipalKey(x=>new {x.IDDOT,x.MANV}).OnDelete(DeleteBehavior.NoAction);
        // FK_LS_DIEUCHUYEN (SOQD_DIEUCHUYEN, IDDOT) remains enforced by SQL Server.
        // Do not declare the nullable TransferDecision.IDDOT as an EF alternate key:
        // EF requires key values; legacy decisions without IDDOT must remain readable.
        // The service inserts the decision before its history in the same transaction.
    }
}
