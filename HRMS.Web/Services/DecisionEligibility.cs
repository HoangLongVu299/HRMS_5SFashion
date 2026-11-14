using HRMS.Web.Data;
using HRMS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;

// Context-specific policy: only NEW transfer/termination decisions use this class.
// tb_NHANVIEN in No05 has no soft-delete columns. Never invent a mapped deletion flag.
public static class DecisionEligibility
{
    public const string Unavailable = "Nhân viên đã nghỉ việc hoặc không còn đợt làm việc hợp lệ. Vui lòng chọn lại nhân viên.";
    public record Result(bool Allowed, string? Reason = null, string? Warning = null);
    public record Choice(int EmployeeId, string? EmployeeName, int PeriodId, int CompanyId,
        string? CompanyName, DateOnly Start, string? Warning);

    public static Result Evaluate(Employee employee, WorkPeriod period,
        IReadOnlyList<WorkPeriod> periods, IReadOnlyList<TerminationDecision> terminations,
        bool hasUnlinkedDecision, DateOnly today)
    {
        if (period.MANV != employee.MANV) return new(false, "Đợt làm việc không thuộc nhân viên đã chọn.");
        if (period.NGAYBATDAU > today || period.NGAYKETTHUC < today) return new(false, Unavailable);
        // Existing write rules require an OPEN period and an open final history segment.
        // A dated end today/in the future is a closed period, requiring data review, not reopening.
        if (period.NGAYKETTHUC != null)
            return new(false, "Đợt đã ghi nhận ngày kết thúc. Cần xác minh đợt và lịch sử trước khi lập quyết định mới.");
        if (employee.MACTY != period.MACTY)
            return new(false, "Công ty trên hồ sơ không khớp công ty của đợt làm việc. Cần kiểm tra dữ liệu.");
        if (periods.Count(x => x.NGAYKETTHUC == null) != 1 ||
            periods.Any(x => x.IDDOT != period.IDDOT &&
                (x.NGAYKETTHUC == null || x.NGAYKETTHUC >= period.NGAYBATDAU)))
            return new(false, "Có nhiều đợt mở, đợt chồng lấn hoặc đợt phát sinh sau đó. Cần kiểm tra dữ liệu.");
        // Even a legacy soft-deleted decision may already have closed history. Do not revive it.
        if (terminations.Any(x => x.IDDOT == period.IDDOT)) return new(false, Unavailable);
        if (hasUnlinkedDecision)
            return new(false, "Nhân viên còn quyết định cũ chưa liên kết đợt. Cần xác minh liên kết trước khi lập quyết định mới.");

        if (employee.DATHOIVIEC == true)
        {
            // Only accept a stale profile flag when an earlier, terminated employment period
            // actually proves re-employment. An old termination alone must not exclude a rehire.
            bool rehire = periods.Any(old => old.IDDOT != period.IDDOT &&
                old.NGAYKETTHUC is {} end && end < period.NGAYBATDAU &&
                terminations.Any(t => t.IDDOT == old.IDDOT && t.MANV == employee.MANV &&
                    t.NGAYNGHI is {} leave && DateOnly.FromDateTime(leave) == end.AddDays(1)));
            if (!rehire)
                return new(false, "Hồ sơ đánh dấu đã thôi việc nhưng đợt vẫn mở và chưa đủ căn cứ tái tuyển dụng. Cần xác minh dữ liệu.");
            return new(true, Warning: "Nhân viên có đợt tái tuyển dụng hợp lệ; cờ đã thôi việc trên hồ sơ chưa đồng bộ. Hệ thống dùng đợt mới và giữ nguyên dữ liệu cũ.");
        }
        return new(true);
    }

    public static async Task<List<Choice>> LoadAsync(HrmsContext db, int? employeeId = null, List<string>? issues = null)
    {
        // Do not load employee photos, contracts or personal details for dropdowns.
        var employees = await db.Employees.AsNoTracking()
            .Where(x => employeeId == null || x.MANV == employeeId)
            .Select(x => new Employee { MANV=x.MANV, HOTEN=x.HOTEN, MACTY=x.MACTY, DATHOIVIEC=x.DATHOIVIEC })
            .ToListAsync();
        var periods = await db.WorkPeriods.AsNoTracking()
            .Where(x => employeeId == null || x.MANV == employeeId).ToListAsync();
        var terminations = await db.Terminations.AsNoTracking()
            .Where(x => employeeId == null || x.MANV == employeeId ||
                db.WorkPeriods.Any(p => p.IDDOT == x.IDDOT && p.MANV == employeeId))
            .Select(x => new TerminationDecision { MANV=x.MANV, IDDOT=x.IDDOT, NGAYNGHI=x.NGAYNGHI,
                DELETED_DATE=x.DELETED_DATE, DELETED_BY=x.DELETED_BY }).ToListAsync();
        var unlinkedTransfers = await db.Transfers.AsNoTracking()
            .Where(x => (employeeId == null || x.MANV == employeeId) && x.IDDOT == null &&
                x.DELETED_DATE == null && x.DELETED_BY == null).Select(x => x.MANV).ToListAsync();
        var companies = await db.Companys.AsNoTracking().ToDictionaryAsync(x => x.MACTY, x => x.TENCTY);
        var histories = await db.WorkHistorys.AsNoTracking()
            .Where(x => employeeId == null || x.WorkPeriod.MANV == employeeId).ToListAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var choices = new List<Choice>();
        foreach (var employee in employees)
        {
            var ownPeriods = periods.Where(x => x.MANV == employee.MANV).ToList();
            var ownTerminations = terminations.Where(x => x.MANV == employee.MANV ||
                ownPeriods.Any(p => p.IDDOT == x.IDDOT)).ToList();
            bool unlinked = unlinkedTransfers.Contains(employee.MANV) || ownTerminations.Any(x =>
                x.IDDOT == null && x.DELETED_DATE == null && x.DELETED_BY == null);
            foreach (var period in ownPeriods)
            {
                var result = Evaluate(employee, period, ownPeriods, ownTerminations, unlinked, today);
                if (!result.Allowed && result.Reason != Unavailable && period.NGAYBATDAU <= today &&
                    (period.NGAYKETTHUC == null || period.NGAYKETTHUC >= today))
                    issues?.Add($"Nhân viên #{employee.MANV} – {employee.HOTEN}, đợt #{period.IDDOT}: {result.Reason}");
                if (result.Allowed && companies.TryGetValue(period.MACTY, out var company))
                {
                    var timeline = histories.Where(x => x.IDDOT == period.IDDOT)
                        .OrderBy(x => x.TUNGAY).ThenBy(x => x.IDLS).ToList();
                    try { PersonnelService.ValidateTimeline(period, timeline); }
                    catch (BusinessException ex)
                    {
                        issues?.Add($"Nhân viên #{employee.MANV} – {employee.HOTEN}, đợt #{period.IDDOT}: {ex.Message}");
                        continue; // Do not repair or invent old history.
                    }
                    if (timeline[^1].TUNGAY > today)
                    {
                        issues?.Add($"Nhân viên #{employee.MANV} – {employee.HOTEN}: lịch sử cuối bắt đầu trong tương lai. Cần kiểm tra dữ liệu.");
                        continue;
                    }
                    choices.Add(new(employee.MANV, employee.HOTEN, period.IDDOT, period.MACTY,
                        company, period.NGAYBATDAU, result.Warning));
                }
            }
        }
        return choices.OrderBy(x => x.EmployeeId).ThenBy(x => x.PeriodId).ToList();
    }
}
