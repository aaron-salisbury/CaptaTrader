using System.Reflection;

namespace CaptaTrader.Business.Base.Extensions
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Return the Types of all classes that implement IEntity within Assembly.
        /// </summary>
        public static Type[] GetEntityTypes(this Assembly layerWithEntities)
        {
            return layerWithEntities.GetExportedTypes()
                .Where(t =>typeof(IEntity).IsAssignableFrom(t) && t != typeof(IEntity))
                .ToArray();
        }
    }
}
