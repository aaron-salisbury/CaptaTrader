using CaptaTrader.Business.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public interface IUserService
    {
        EndUser CreateNewDefaultUser();

        IEnumerable<ValidationResult> Authenticate(NetworkCredential networkCredentials);

        IEnumerable<ValidationResult> Register(NetworkCredential networkCredentials);

        IEnumerable<ValidationResult> Save(EndUser user);
    }
}
