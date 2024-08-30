using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CaptaTrader.Business.Base.Extensions
{
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// For the given MemberInfo, first checks for DisplayAttribute to return its Name.
        /// Next checks for a DisplayNameAttribute to return its DisplayName.
        /// If neither attribute can be found, MemberInfo.Name is returned.
        /// </summary>
        public static string GetDisplayName(this MemberInfo memberInfo)
        {
            DisplayAttribute? displayAttribute = memberInfo
                .GetCustomAttributes(typeof(DisplayAttribute), true)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();

            if (displayAttribute?.Name != null)
            {
                return displayAttribute.Name;
            }

            DisplayNameAttribute? displayNameAttribute = memberInfo
                .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                .Cast<DisplayNameAttribute>()
                .FirstOrDefault();

            if (displayNameAttribute != null)
            {
                return displayNameAttribute.DisplayName;
            }

            return memberInfo.Name;
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
