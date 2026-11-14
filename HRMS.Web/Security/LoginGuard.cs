namespace HRMS.Web.Security;
// A single local demo account: 5 consecutive failures lock all attempts for 5 minutes.
public sealed class LoginGuard
{
    private readonly object gate = new();
    private int failures;
    private DateTimeOffset lockedUntil;
    public bool Attempt(Func<bool> verify, out bool locked)
    {
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            locked = now < lockedUntil;
            if (locked) return false;
            if (lockedUntil != default) { failures = 0; lockedUntil = default; }
            if (verify()) { failures = 0; return true; }
            if (++failures >= 5) lockedUntil = now.AddMinutes(5);
            return false;
        }
    }
}
