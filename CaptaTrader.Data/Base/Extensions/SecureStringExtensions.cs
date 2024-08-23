using System.Net;
using System.Security;
using System.Text;

namespace CaptaTrader.Data.Base.Extensions
{
    public static class SecureStringExtensions
    {
        /// <summary>
        /// Convert to plain string.
        /// </summary>
        public static string ToPlainString(this SecureString secureString)
        {
            // From Matt's answer at https://stackoverflow.com/questions/818704/how-to-convert-securestring-to-system-string

            NetworkCredential secureStringAsNetworkCredential = new(string.Empty, secureString);

            return secureStringAsNetworkCredential.Password;
        }

        /// <summary>
        /// Convert to byte array.
        /// </summary>
        public static byte[] ToBytes(this SecureString secureString)
        {
            return Encoding.ASCII.GetBytes(secureString.ToPlainString());
        }
    }
}
