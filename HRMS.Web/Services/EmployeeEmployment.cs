using HRMS.Web.Data;
using HRMS.Web.Models;

namespace HRMS.Web.Services;

public static class EmployeeEmployment
{
    public sealed record Row
    {
        public Employee Employee { get; init; } = null!;
        public string Status { get; init; } = "";
    }

    public static bool IsUnassigned(Employee employee) => employee.DATHOIVIEC == null
        && employee.MACTY == null && employee.IDPB == null && employee.IDBP == null && employee.IDCV == null;

    public static IQueryable<Row> WithStatus(HrmsContext db, IQueryable<Employee> employees, DateOnly today) =>
        employees.Select(employee => new Row
        {
            Employee = employee,
            Status = employee.DATHOIVIEC == true ? "left"
                : employee.DATHOIVIEC == false
                    ? employee.WorkPeriods.Any(period => period.NGAYKETTHUC == null && period.NGAYBATDAU > today)
                        && !employee.WorkPeriods.Any(period => period.NGAYBATDAU <= today
                            && (period.NGAYKETTHUC == null || period.NGAYKETTHUC >= today))
                        ? "scheduled" : "working"
                    : employee.MACTY == null && employee.IDPB == null && employee.IDBP == null && employee.IDCV == null
                        && !employee.WorkPeriods.Any()
                        && !db.Contracts.Any(document => document.MANV == employee.MANV)
                        && !db.Transfers.Any(document => document.MANV == employee.MANV)
                        && !db.Terminations.Any(document => document.MANV == employee.MANV)
                        && !db.RewardDecisions.Any(document => document.MANV == employee.MANV)
                        ? "pending" : "unknown"
        });
}
