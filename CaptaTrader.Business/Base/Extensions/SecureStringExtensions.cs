using System.Net;
using System.Security;

namespace CaptaTrader.Business.Base.Extensions
{
    internal static class SecureStringExtensions
    {
        /// <summary>
        /// Convert to plain string.
        /// </summary>
        public static String ToPlainString(this SecureString secureString)
        {
            // From Matt's answer at https://stackoverflow.com/questions/818704/how-to-convert-securestring-to-system-string

            NetworkCredential secureStringAsNetworkCredential = new(string.Empty, secureString);

            return secureStringAsNetworkCredential.Password;
        }
    }
}
