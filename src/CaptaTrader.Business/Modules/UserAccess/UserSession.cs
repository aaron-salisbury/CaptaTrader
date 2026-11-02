namespace CaptaTrader.Business.Modules.UserAccess;

/// <summary>
/// Process-wide record of who is signed in. Business capabilities that act on the user's behalf,
/// such as auto-trading, check this rather than trusting the host to have gated them.
/// </summary>
public sealed class UserSession
{
    private readonly object _gate = new();
    private string? _userName;
    private bool _isDemo;

    public bool IsAuthenticated
    {
        get
        {
            lock (_gate)
            {
                return _userName is not null;
            }
        }
    }

    public string? UserName
    {
        get
        {
            lock (_gate)
            {
                return _userName;
            }
        }
    }

    /// <summary>True when the session is a throwaway demo that only uses simulated providers.</summary>
    public bool IsDemo
    {
        get
        {
            lock (_gate)
            {
                return _isDemo;
            }
        }
    }

    internal void SignIn(string userName)
    {
        lock (_gate)
        {
            _userName = userName;
            _isDemo = false;
        }
    }

    public void SignInDemo()
    {
        lock (_gate)
        {
            _userName = "demo";
            _isDemo = true;
        }
    }

    public void SignOut()
    {
        lock (_gate)
        {
            _userName = null;
            _isDemo = false;
        }
    }
}
