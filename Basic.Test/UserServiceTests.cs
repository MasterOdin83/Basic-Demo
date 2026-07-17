using Basic.Core.Services;

namespace Basic.Test;

public class UserServiceTests
{
    private readonly FakeUserRepository _repo = new();
    private readonly UserService _service;

    public UserServiceTests() => _service = new UserService(_repo);

    [Fact]
    public async Task Register_stores_hash_not_password()
    {
        var user = await _service.RegisterAsync("demo", "password123");

        Assert.True(user.Id > 0);
        Assert.NotEqual("password123", user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task Register_trims_username()
    {
        var user = await _service.RegisterAsync("  demo  ", "password123");

        Assert.Equal("demo", user.Username);
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("   ", "password123")]
    [InlineData("demo", "short")]
    public async Task Register_rejects_invalid_input(string username, string password)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterAsync(username, password));
    }

    [Fact]
    public async Task Register_rejects_duplicate_username()
    {
        await _service.RegisterAsync("demo", "password123");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.RegisterAsync("demo", "password456"));
    }

    [Fact]
    public async Task ValidateCredentials_accepts_correct_password()
    {
        await _service.RegisterAsync("demo", "password123");

        var user = await _service.ValidateCredentialsAsync("demo", "password123");

        Assert.NotNull(user);
        Assert.Equal("demo", user.Username);
    }

    [Fact]
    public async Task ValidateCredentials_rejects_wrong_password_and_unknown_user()
    {
        await _service.RegisterAsync("demo", "password123");

        Assert.Null(await _service.ValidateCredentialsAsync("demo", "wrong-password"));
        Assert.Null(await _service.ValidateCredentialsAsync("nobody", "password123"));
    }
}
