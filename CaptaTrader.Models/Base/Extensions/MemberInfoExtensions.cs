using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CaptaTrader.Models.Base.Extensions
{
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Returns value of the DisplayName attribute for the given MemberInfo.
        /// If MemberInfo has no DisplayName attribute on it, MemberInfo.Name is returned.
        /// </summary>
        public static string GetDisplayName(this MemberInfo memberInfo)
        {
            // https://stackoverflow.com/a/5015878

            DisplayNameAttribute? attribute = memberInfo
                .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                .Cast<DisplayNameAttribute>()
                .FirstOrDefault();

            if (attribute == null)
            {
                return memberInfo.Name;
            }

            return attribute.DisplayName;
        }

        /// <summary>
        /// Returns true when calling MemberInfo has the Key attribute.
        /// </summary>
        public static bool IsKey(this MemberInfo memberInfo)
        {
            KeyAttribute? attribute = memberInfo
                .GetCustomAttributes(typeof(KeyAttribute), true)
                .Cast<KeyAttribute>()
                .FirstOrDefault();

            return attribute != null;
        }
    }
}
