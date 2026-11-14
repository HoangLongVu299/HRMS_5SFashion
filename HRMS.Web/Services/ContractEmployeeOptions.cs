using HRMS.Web.Data;
using HRMS.Web.Models;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;

public static class ContractEmployeeOptions
{
    public record Choice(int Id, string Text, bool FirstEmployment);

    public static IQueryable<EmployeeEmployment.Row> Eligible(HrmsContext db, IQueryable<Employee> employees, bool rehire, DateOnly today) =>
        EmployeeEmployment.WithStatus(db, employees, today)
            .Where(row => row.Status == (rehire ? "left" : "pending"));

    public static async Task<List<Choice>> LoadForCreate(HrmsContext db, bool rehire, List<string> issues)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var employees = await Eligible(db, db.Employees.AsNoTracking(), rehire, today)
            .OrderBy(row => row.Employee.MANV)
            .Select(row => new { row.Employee.MANV, row.Employee.HOTEN, row.Status })
            .ToListAsync();
        if (rehire && employees.Count > 0)
        {
            var retiredIds = employees.Select(row => row.MANV).ToList();
            var periods = await db.WorkPeriods.AsNoTracking().Where(p => retiredIds.Contains(p.MANV)).ToListAsync();
            var histories = await db.WorkHistorys.AsNoTracking().Where(h => retiredIds.Contains(h.WorkPeriod.MANV)).ToListAsync();
            var unlinked = await ContractEmploymentPolicy.UnlinkedDecisionEmployees(db).ToListAsync();
            var periodsByEmployee = periods.ToLookup(p => p.MANV);
            foreach (var employee in employees)
            {
                var ownPeriods = periodsByEmployee[employee.MANV].ToList();
                try
                {
                    ContractEmploymentPolicy.ValidateRehire(ownPeriods, unlinked.Contains(employee.MANV), today, today);
                    ContractEmploymentPolicy.ValidateClosedHistories(ownPeriods, histories);
                }
                catch (BusinessException ex)
                {
                    issues.Add($"Nhân viên #{employee.MANV} – {employee.HOTEN}: {ex.Message}");
                }
            }
        }

        return employees.Select(row => new Choice(row.MANV,
            $"{row.MANV} – {row.HOTEN ?? "Chưa có tên"} ({DisplayText.EmploymentStatus(row.Status)})",
            !rehire)).ToList();
    }
}
