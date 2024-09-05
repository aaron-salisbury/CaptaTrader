using CaptaTrader.Business.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services.Domain
{
    public interface IUserService
    {
        EndUser CreateNewDefaultUser();

        IEnumerable<ValidationResult> Authenticate(NetworkCredential networkCredentials);

        IEnumerable<ValidationResult> Register(NetworkCredential networkCredentials);

        EndUser? GetEndUser(int? id);

        IEnumerable<ValidationResult> SaveEndUser(EndUser user);
    }
}
