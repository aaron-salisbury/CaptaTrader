using CaptaTrader.Business;

namespace CaptaTrader.Presentation.CLI.Base;

public class Session : ISessionValueProvider
{
    private int? _endUser;

    public int? GetUserID()
    {
        return _endUser;
    }

    public void SetUserID(int id)
    {
        _endUser = id;
    }
}
