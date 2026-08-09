using Basic.Core.Entities;

namespace Basic.Core.Repositories;

// Deliberately its own interface, not folded into IUserRepository — session/identity
// state is a different concern from user profile data, even though the demo-scoped
// EfSessionStore implementation happens to persist both in the same database.
public interface ISessionStore
{
    Task CreateAsync(Session session);
    Task<Session?> GetAsync(string id);
    Task RemoveAsync(string id);
}
