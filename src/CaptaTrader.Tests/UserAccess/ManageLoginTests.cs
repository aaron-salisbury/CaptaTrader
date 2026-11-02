using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.UserAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.UserAccess;

[TestClass]
public sealed class ManageLoginTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task FreshDatabase_HasNoLogin_ThenCreateSignsIn()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ManageLogin login = services.GetRequiredService<ManageLogin>();
        UserSession session = services.GetRequiredService<UserSession>();

        Assert.IsFalse(await login.LoginExistsAsync(TestContext.CancellationToken));

        CapabilityResult<string> created = await login.CreateLoginAsync("  aaron ", "correct horse", "correct horse", TestContext.CancellationToken);

        Assert.IsTrue(created.IsSuccess, created.Message);
        Assert.AreEqual("aaron", created.Value);
        Assert.IsTrue(await login.LoginExistsAsync(TestContext.CancellationToken));
        Assert.IsTrue(session.IsAuthenticated);
        Assert.IsFalse(session.IsDemo);
    }

    [TestMethod]
    public async Task SecondLogin_IsRefused_BecauseTheToolIsSingleUser()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ManageLogin login = services.GetRequiredService<ManageLogin>();

        await login.CreateLoginAsync("aaron", "correct horse", "correct horse", TestContext.CancellationToken);
        CapabilityResult<string> second = await login.CreateLoginAsync("someone", "another pass", "another pass", TestContext.CancellationToken);

        Assert.AreEqual(CapabilityStatus.Conflict, second.Status);
    }

    [TestMethod]
    public async Task LogIn_AcceptsCorrectCredentials_CaseInsensitiveUserName()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ManageLogin login = services.GetRequiredService<ManageLogin>();
        UserSession session = services.GetRequiredService<UserSession>();

        await login.CreateLoginAsync("Aaron", "correct horse", "correct horse", TestContext.CancellationToken);
        session.SignOut();

        CapabilityResult<string> result = await login.LogInAsync("aaron", "correct horse", TestContext.CancellationToken);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Aaron", session.UserName);
    }

    [TestMethod]
    [DataRow("aaron", "wrong password")]
    [DataRow("nobody", "correct horse")]
    public async Task LogIn_RejectsWrongCredentials_WithOneMessage(string userName, string password)
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ManageLogin login = services.GetRequiredService<ManageLogin>();
        UserSession session = services.GetRequiredService<UserSession>();

        await login.CreateLoginAsync("aaron", "correct horse", "correct horse", TestContext.CancellationToken);
        session.SignOut();

        CapabilityResult<string> result = await login.LogInAsync(userName, password, TestContext.CancellationToken);

        Assert.AreEqual(CapabilityStatus.Unauthorized, result.Status);
        Assert.AreEqual("User name or password is incorrect.", result.Message);
        Assert.IsFalse(session.IsAuthenticated);
    }

    [TestMethod]
    [DataRow("", "long enough", "long enough")]
    [DataRow("aaron", "short", "short")]
    [DataRow("aaron", "long enough", "different!")]
    public void Validation_RejectsBadInput(string userName, string password, string confirmation)
    {
        Assert.IsNotNull(ManageLogin.ValidateNewLogin(userName, password, confirmation));
    }

    [TestMethod]
    public void PasswordHash_VerifiesOnlyTheOriginal_AndSaltsEachHash()
    {
        PasswordHash first = PasswordHasher.Hash("correct horse", iterations: 1_000);
        PasswordHash second = PasswordHasher.Hash("correct horse", iterations: 1_000);

        Assert.IsTrue(PasswordHasher.Verify("correct horse", first));
        Assert.IsFalse(PasswordHasher.Verify("Correct horse", first));
        Assert.IsFalse(PasswordHasher.Verify(string.Empty, first));
        Assert.AreNotSequenceEqual(first.Hash, second.Hash);
    }
}
