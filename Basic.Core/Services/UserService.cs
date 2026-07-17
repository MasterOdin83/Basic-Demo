using Basic.Core.Entities;
using Basic.Core.Repositories;
using Basic.Core.Security;

namespace Basic.Core.Services;

public class UserService(IUserRepository users)
{
    public async Task<User> RegisterAsync(string username, string password)
    {
        username = username?.Trim() ?? "";
        if (username.Length == 0) throw new ArgumentException("Username is required.");
        if (password is null || password.Length < 8) throw new ArgumentException("Password must be at least 8 characters.");
        if (await users.GetByUsernameAsync(username) is not null) throw new ArgumentException("Username is already taken.");

        return await users.AddAsync(new User { Username = username, PasswordHash = PasswordHasher.Hash(password) });
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await users.GetByUsernameAsync(username?.Trim() ?? "");
        return user is not null && PasswordHasher.Verify(password ?? "", user.PasswordHash) ? user : null;
    }
}
