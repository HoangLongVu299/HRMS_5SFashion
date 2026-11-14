using HRMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Data;

public class HrmsContext(DbContextOptions<HrmsContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Ethnicity> Ethnicitys => Set<Ethnicity>();
    public DbSet<Religion> Religions => Set<Religion>();
    public DbSet<Company> Companys => Set<Company>();
    public DbSet<WorkPeriod> WorkPeriods => Set<WorkPeriod>();
    public DbSet<WorkHistory> WorkHistorys => Set<WorkHistory>();
    public DbSet<LaborContract> Contracts => Set<LaborContract>();
    public DbSet<TransferDecision> Transfers => Set<TransferDecision>();
    public DbSet<TerminationDecision> Terminations => Set<TerminationDecision>();
    public DbSet<RewardDecision> RewardDecisions => Set<RewardDecision>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        PersonnelMapping.Configure(modelBuilder);
        CatalogRewardMapping.Configure(modelBuilder);
        modelBuilder.Entity<WorkPeriod>(e =>
        {
            e.ToTable("tb_DOTLAMVIEC", "dbo");
            e.HasKey(x => x.IDDOT).HasName("PK_tb_DOTLAMVIEC");
            e.Property(x => x.IDDOT).HasColumnName("IDDOT").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.MANV).HasColumnName("MANV").HasColumnType("int");
            e.Property(x => x.MACTY).HasColumnName("MACTY").HasColumnType("int");
            e.Property(x => x.NGAYBATDAU).HasColumnName("NGAYBATDAU").HasColumnType("date");
            e.Property(x => x.NGAYKETTHUC).HasColumnName("NGAYKETTHUC").HasColumnType("date");
            e.Property(x => x.GHICHU).HasColumnName("GHICHU").HasColumnType("nvarchar(500)");
            e.Property(x => x.CREATED_DATE).HasColumnName("CREATED_DATE").HasColumnType("datetime2(7)").HasDefaultValueSql("sysdatetime()");
        });
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("tb_NHANVIEN", "dbo");
            e.HasKey(x => x.MANV).HasName("PK_tb_NHANVIEN");
            e.Property(x => x.MANV).HasColumnName("MANV").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.HOTEN).HasColumnName("HOTEN").HasColumnType("nvarchar(50)");
            e.Property(x => x.GIOITINH).HasColumnName("GIOITINH").HasColumnType("bit");
            e.Property(x => x.DIENTHOAI).HasColumnName("DIENTHOAI").HasColumnType("nvarchar(50)");
            e.Property(x => x.CCCD).HasColumnName("CCCD").HasColumnType("nvarchar(50)");
            e.Property(x => x.DIACHI).HasColumnName("DIACHI").HasColumnType("nvarchar(300)");
            e.Property(x => x.HINHANH).HasColumnName("HINHANH").HasColumnType("varbinary(max)");
            e.Property(x => x.IDPB).HasColumnName("IDPB").HasColumnType("int");
            e.Property(x => x.IDBP).HasColumnName("IDBP").HasColumnType("int");
            e.Property(x => x.IDCV).HasColumnName("IDCV").HasColumnType("int");
            e.Property(x => x.IDTD).HasColumnName("IDTD").HasColumnType("int");
            e.Property(x => x.IDDT).HasColumnName("IDDT").HasColumnType("int");
            e.Property(x => x.IDTG).HasColumnName("IDTG").HasColumnType("int");
            e.Property(x => x.MACTY).HasColumnName("MACTY").HasColumnType("int");
            e.Property(x => x.NGAYSINH).HasColumnName("NGAYSINH").HasColumnType("datetime");
            e.Property(x => x.DATHOIVIEC).HasColumnName("DATHOIVIEC").HasColumnType("bit");
        });
        modelBuilder.Entity<WorkHistory>(e =>
        {
            e.ToTable("tb_LICHSU_CONGTAC", "dbo");
            e.HasKey(x => x.IDLS).HasName("PK_tb_LICHSU_CONGTAC");
            e.Property(x => x.IDLS).HasColumnName("IDLS").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.IDDOT).HasColumnName("IDDOT").HasColumnType("int");
            e.Property(x => x.IDPB).HasColumnName("IDPB").HasColumnType("int");
            e.Property(x => x.IDBP).HasColumnName("IDBP").HasColumnType("int");
            e.Property(x => x.IDCV).HasColumnName("IDCV").HasColumnType("int");
            e.Property(x => x.TUNGAY).HasColumnName("TUNGAY").HasColumnType("date");
            e.Property(x => x.DENNGAY).HasColumnName("DENNGAY").HasColumnType("date");
            e.Property(x => x.SOQD_DIEUCHUYEN).HasColumnName("SOQD_DIEUCHUYEN").HasColumnType("nvarchar(50)");
            e.Property(x => x.GHICHU).HasColumnName("GHICHU").HasColumnType("nvarchar(500)");
        });
        modelBuilder.Entity<Section>(e =>
        {
            e.ToTable("tb_BOPHAN", "dbo");
            e.HasKey(x => x.IDBP).HasName("PK_tb_BOPHAN");
            e.Property(x => x.IDBP).HasColumnName("IDBP").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENBP).HasColumnName("TENBP").HasColumnType("nvarchar(50)");
        });
        modelBuilder.Entity<Position>(e =>
        {
            e.ToTable("tb_CHUCVU", "dbo");
            e.HasKey(x => x.IDCV).HasName("PK_tb_CHUCVU");
            e.Property(x => x.IDCV).HasColumnName("IDCV").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENCV).HasColumnName("TENCV").HasColumnType("nvarchar(50)");
        });
        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("tb_CONGTY", "dbo");
            e.HasKey(x => x.MACTY).HasName("PK_tb_CONGTY");
            e.Property(x => x.MACTY).HasColumnName("MACTY").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENCTY).HasColumnName("TENCTY").HasColumnType("nvarchar(100)");
            e.Property(x => x.DIENTHOAI).HasColumnName("DIENTHOAI").HasColumnType("nvarchar(15)");
            e.Property(x => x.EMAIL).HasColumnName("EMAIL").HasColumnType("nvarchar(100)");
            e.Property(x => x.DIACHI).HasColumnName("DIACHI").HasColumnType("nvarchar(500)");
        });
        modelBuilder.Entity<Ethnicity>(e =>
        {
            e.ToTable("tb_DANTOC", "dbo");
            e.HasKey(x => x.ID).HasName("PK_tb_DANTOC,");
            e.Property(x => x.ID).HasColumnName("ID").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENDT).HasColumnName("TENDT").HasColumnType("nvarchar(50)");
        });
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("tb_PHONGBAN", "dbo");
            e.HasKey(x => x.IDPB).HasName("PK_tb_PHONGBAN");
            e.Property(x => x.IDPB).HasColumnName("IDPB").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENPB).HasColumnName("TENPB").HasColumnType("nvarchar(50)");
        });
        modelBuilder.Entity<Religion>(e =>
        {
            e.ToTable("tb_TONGIAO", "dbo");
            e.HasKey(x => x.ID).HasName("PK_tb_DANTOC");
            e.Property(x => x.ID).HasColumnName("ID").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENTG).HasColumnName("TENTG").HasColumnType("nvarchar(max)");
        });
        modelBuilder.Entity<Education>(e =>
        {
            e.ToTable("tb_TRINHDO", "dbo");
            e.HasKey(x => x.IDTD).HasName("PK_tb_TRINHDO");
            e.Property(x => x.IDTD).HasColumnName("IDTD").HasColumnType("int").UseIdentityColumn();
            e.Property(x => x.TENTD).HasColumnName("TENTD").HasColumnType("nvarchar(50)");
        });
        modelBuilder.Entity<WorkPeriod>().HasOne(x => x.Company).WithMany().HasForeignKey(x => x.MACTY).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DOT_CONGTY");
        modelBuilder.Entity<WorkPeriod>().HasOne(x => x.Employee).WithMany(x => x.WorkPeriods).HasForeignKey(x => x.MANV).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_DOT_NHANVIEN");
        modelBuilder.Entity<WorkHistory>().HasOne(x => x.Section).WithMany().HasForeignKey(x => x.IDBP).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LS_BOPHAN");
        modelBuilder.Entity<WorkHistory>().HasOne(x => x.Position).WithMany().HasForeignKey(x => x.IDCV).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LS_CHUCVU");
        modelBuilder.Entity<WorkHistory>().HasOne(x => x.WorkPeriod).WithMany(x => x.Histories).HasForeignKey(x => x.IDDOT).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LS_DOT");
        modelBuilder.Entity<WorkHistory>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.IDPB).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LS_PHONGBAN");
        modelBuilder.Entity<Employee>().HasOne(x => x.Section).WithMany().HasForeignKey(x => x.IDBP).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_BOPHAN");
        modelBuilder.Entity<Employee>().HasOne(x => x.Position).WithMany().HasForeignKey(x => x.IDCV).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_CHUCVU");
        modelBuilder.Entity<Employee>().HasOne(x => x.Company).WithMany().HasForeignKey(x => x.MACTY).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_CONGTY");
        modelBuilder.Entity<Employee>().HasOne(x => x.Ethnicity).WithMany().HasForeignKey(x => x.IDDT).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_DANTOC");
        modelBuilder.Entity<Employee>().HasOne(x => x.Department).WithMany().HasForeignKey(x => x.IDPB).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_PHONGBAN");
        modelBuilder.Entity<Employee>().HasOne(x => x.Religion).WithMany().HasForeignKey(x => x.IDTG).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_TONGIAO");
        modelBuilder.Entity<Employee>().HasOne(x => x.Education).WithMany().HasForeignKey(x => x.IDTD).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_tb_NHANVIEN_tb_TRINHDO");
    }
}
