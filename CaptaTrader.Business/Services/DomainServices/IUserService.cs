using CaptaTrader.Business.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public interface IUserService
    {
        (EndUser?, IEnumerable<ValidationResult>) Authenticate(NetworkCredential networkCredentials);

        (EndUser?, IEnumerable<ValidationResult>) Register(NetworkCredential networkCredentials);

        IEnumerable<ValidationResult> Save(EndUser user);
    }
}
