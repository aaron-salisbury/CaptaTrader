using System;

namespace CaptaTrader.Data.DataAnnotations
{
    /// <summary>
    /// Specifies the property as a relationship that <see cref="DynamicEmbeddedDataAccess"/> needs to include when doing recursive reads.
    /// </summary>
    public class RelationshipAttribute : Attribute { }
}
