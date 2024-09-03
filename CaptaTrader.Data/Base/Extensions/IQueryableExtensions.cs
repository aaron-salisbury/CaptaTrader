using CaptaTrader.Data.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace CaptaTrader.Data.Base.Extensions
{
    internal static class IQueryableExtensions
    {
        /// <summary>
        /// Recursively include all navigation entity properties annotated with the RelationshipAttribute.
        /// </summary>
        internal static IQueryable<TEntity> IncludeAllRecursively<TEntity>(this IQueryable<TEntity> queryable, int maxDepth = int.MaxValue, bool addSeenTypesToIgnoreList = true, HashSet<Type>? ignoreTypes = null) where TEntity : class
        {
            // https://stackoverflow.com/a/64695007

            var type = typeof(TEntity);
            var includes = new List<string>();
            ignoreTypes ??= new HashSet<Type>();
            GetIncludeTypes(ref includes, maxDepth, prefix: string.Empty, type, ref ignoreTypes, addSeenTypesToIgnoreList);
            foreach (var include in includes)
            {
                queryable = queryable.Include(include);
            }

            return queryable;
        }

        private static void GetIncludeTypes(ref List<string> includes, int maxDepth, string prefix, Type type, ref HashSet<Type> ignoreSubTypes, bool addSeenTypesToIgnoreList = true)
        {
            var properties = type.GetProperties().Where(pi => pi
                .GetCustomAttributes(typeof(RelationshipAttribute), true)
                .Cast<RelationshipAttribute>()
                .FirstOrDefault() != null);

            foreach (var property in properties)
            {
                var getter = property.GetGetMethod();
                if (getter != null)
                {
                    var propPath = (prefix + "." + property.Name).TrimStart('.');
                    if (maxDepth <= propPath.Count(c => c == '.')) { break; }

                    includes.Add(propPath);

                    var subType = property.PropertyType;
                    if (ignoreSubTypes.Contains(subType))
                    {
                        continue;
                    }
                    else if (addSeenTypesToIgnoreList)
                    {
                        // add each type that we have processed to ignore list to prevent recursions
                        ignoreSubTypes.Add(type);
                    }

                    var isEnumerableType = subType.GetInterface(nameof(IEnumerable)) != null;
                    var genericArgs = subType.GetGenericArguments();
                    if (isEnumerableType && genericArgs.Length == 1)
                    {
                        // sub property is collection, use collection type and drill down
                        var subTypeCollection = genericArgs[0];
                        if (subTypeCollection != null)
                        {
                            GetIncludeTypes(ref includes, maxDepth, propPath, subTypeCollection, ref ignoreSubTypes, addSeenTypesToIgnoreList);
                        }
                    }
                    else
                    {
                        // sub property is no collection, drill down directly
                        GetIncludeTypes(ref includes, maxDepth, propPath, subType, ref ignoreSubTypes, addSeenTypesToIgnoreList);
                    }
                }
            }
        }
    }
}
