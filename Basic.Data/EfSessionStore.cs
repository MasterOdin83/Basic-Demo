using Basic.Core.Entities;
using Basic.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Basic.Data;

public class EfSessionStore(AppDbContext db) : ISessionStore
{
    public async Task CreateAsync(Session session)
    {
        db.Sessions.Add(session);
        await db.SaveChangesAsync();
    }

    public async Task<Session?> GetAsync(string id)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        return session is null || session.ExpiresAtUtc <= DateTime.UtcNow ? null : session;
    }

    public async Task RemoveAsync(string id)
    {
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session is null) return;
        db.Sessions.Remove(session);
        await db.SaveChangesAsync();
    }
}
