using System.Reflection;

namespace CaptaTrader.Data.Base.Extensions
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Return the Types of all children of BaseEntity within Assembly.
        /// </summary>
        public static Type[] GetEntityTypes(this Assembly layerWithEntities)
        {
            return layerWithEntities.GetExportedTypes()
                .Where(t =>typeof(BaseEntity).IsAssignableFrom(t) && t != typeof(BaseEntity))
                .ToArray();
        }
    }
}
