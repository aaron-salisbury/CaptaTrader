using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Data.Base
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
