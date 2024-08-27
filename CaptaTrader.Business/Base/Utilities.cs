using CaptaTrader.Business.Base.Extensions;
using System.Reflection;

namespace CaptaTrader.Business.Base
{
    public static class Utilities
    {
        /// <summary>
        /// Returns value of the DisplayName attribute for the given property.
        /// If property can't be found for the type, the given property name is returned. 
        /// If property is found but it doesn't have the DisplayName attribute, MemberInfo.Name is returned.
        /// </summary>
        public static string GetDisplayName<T>(string propertyName)
        {
            MemberInfo? modelInfo = typeof(T).GetProperty(propertyName);

            if (modelInfo == null)
            {
                return propertyName;
            }

            return modelInfo.GetDisplayName();
        }
    }
}
