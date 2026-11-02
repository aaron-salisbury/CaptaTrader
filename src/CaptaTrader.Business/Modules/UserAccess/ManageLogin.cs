using CaptaTrader.Business.Modules.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess;

/// <summary>
/// Creates the single local login and signs the user in against it.
/// </summary>
/// <remarks>
/// The login gates use of the app. It is not encryption: the database is a local file, and API keys
/// are protected by the OS credential store for the current OS user. See docs/decisions.md.
/// </remarks>
public sealed class ManageLogin
{
    public const int MINIMUM_PASSWORD_LENGTH = 8;
    private const int MAXIMUM_USER_NAME_LENGTH = 64;

    private readonly IUserAccountStore _accountStore;
    private readonly UserSession _session;
    private readonly TimeProvider _timeProvider;

    public ManageLogin(IUserAccountStore accountStore, UserSession session, TimeProvider timeProvider)
    {
        _accountStore = accountStore ?? throw new ArgumentNullException(nameof(accountStore));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<bool> LoginExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _accountStore.GetAsync(cancellationToken) is not null;
    }

    public async Task<CapabilityResult<string>> CreateLoginAsync(string userName, string password, string passwordConfirmation, CancellationToken cancellationToken = default)
    {
        string trimmedUserName = (userName ?? string.Empty).Trim();
        string? error = ValidateNewLogin(trimmedUserName, password, passwordConfirmation);

        if (error is not null)
        {
            return CapabilityResult<string>.Failure(CapabilityStatus.InvalidRequest, error, _timeProvider.GetUtcNow());
        }

        if (await LoginExistsAsync(cancellationToken))
        {
            return LoginAlreadyExists();
        }

        // PBKDF2 is deliberately slow; keep it off the caller's thread so the host can animate.
        PasswordHash hash = await Task.Run(() => PasswordHasher.Hash(password), cancellationToken);
        UserAccount account = new(trimmedUserName, hash, _timeProvider.GetUtcNow());

        if (!await _accountStore.TryCreateAsync(account, cancellationToken))
        {
            return LoginAlreadyExists();
        }

        _session.SignIn(trimmedUserName);

        return CapabilityResult<string>.Success(trimmedUserName, _timeProvider.GetUtcNow());
    }

    public async Task<CapabilityResult<string>> LogInAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        UserAccount? account = await _accountStore.GetAsync(cancellationToken);

        if (account is null)
        {
            return CapabilityResult<string>.Failure(CapabilityStatus.NotConfigured, "No login exists yet.", _timeProvider.GetUtcNow());
        }

        bool userNameMatches = string.Equals(account.UserName, (userName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        bool passwordMatches = await Task.Run(() => PasswordHasher.Verify(password, account.Password), cancellationToken);

        if (!userNameMatches || !passwordMatches)
        {
            // One message for both cases so the prompt does not reveal which part was wrong.
            return CapabilityResult<string>.Failure(CapabilityStatus.Unauthorized, "User name or password is incorrect.", _timeProvider.GetUtcNow());
        }

        _session.SignIn(account.UserName);

        return CapabilityResult<string>.Success(account.UserName, _timeProvider.GetUtcNow());
    }

    internal static string? ValidateNewLogin(string userName, string password, string passwordConfirmation)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return "A user name is required.";
        }

        if (userName.Length > MAXIMUM_USER_NAME_LENGTH)
        {
            return $"User name must be {MAXIMUM_USER_NAME_LENGTH} characters or fewer.";
        }

        if (string.IsNullOrEmpty(password) || password.Length < MINIMUM_PASSWORD_LENGTH)
        {
            return $"Password must be at least {MINIMUM_PASSWORD_LENGTH} characters.";
        }

        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
        {
            return "Passwords do not match.";
        }

        return null;
    }

    private CapabilityResult<string> LoginAlreadyExists()
    {
        return CapabilityResult<string>.Failure(CapabilityStatus.Conflict, "A login already exists for this installation.", _timeProvider.GetUtcNow());
    }
}
