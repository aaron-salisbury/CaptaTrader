using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;

namespace CaptaTrader.Presentation.CLI.Base
{
    public class Session : ISessionValueProvider
    {
        private EndUser? _user;

        public EndUser? GetUser()
        {
            return _user;
        }

        public void SetUser(EndUser user)
        {
            _user = user;
        }
    }
}
