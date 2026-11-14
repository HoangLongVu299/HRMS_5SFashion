using System.Collections.Concurrent;
namespace HRMS.Web.Security;
// Revokes copied cookies on logout. Restarting the local app ends all sessions.
public sealed class DemoSessions(TimeProvider? timeProvider = null)
{
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
    private sealed class Session { public DateTimeOffset LastSeen; public bool Revoked; }
    private readonly ConcurrentDictionary<string, Session> sessions = new();
    public string Create()
    {
        foreach (var entry in sessions)
            lock (entry.Value) if ((timeProvider ?? TimeProvider.System).GetUtcNow() - entry.Value.LastSeen >= IdleTimeout) sessions.TryRemove(entry.Key, out _);
        var id = Guid.NewGuid().ToString("N"); sessions[id] = new Session { LastSeen = (timeProvider ?? TimeProvider.System).GetUtcNow() }; return id;
    }
    public bool Touch(string? id)
    {
        if (id == null || !sessions.TryGetValue(id, out var session)) return false;
        lock (session)
        {
            var now = (timeProvider ?? TimeProvider.System).GetUtcNow();
            if (session.Revoked || now - session.LastSeen >= IdleTimeout) { sessions.TryRemove(id, out _); return false; }
            session.LastSeen = now; return true;
        }
    }
    public void Revoke(string? id)
    {
        if (id != null && sessions.TryRemove(id, out var session)) lock(session) session.Revoked = true;
    }
}
