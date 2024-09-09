using System.Net;
using System.Security;
using System.Text;

namespace CaptaTrader.Business.Base.Extensions
{
    public static class SecureStringExtensions
    {
        /// <summary>
        /// Convert to plain string.
        /// </summary>
        public static string ToPlainString(this SecureString secureString)
        {
            // https://stackoverflow.com/a/38016279

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
