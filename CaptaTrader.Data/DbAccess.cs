using CaptaTrader.Data.Database;
using CaptaTrader.Models;
using CaptaTrader.Data.Base;

namespace CaptaTrader.Data
{
    public class DbAccess
    {
        public required ApplicationDbContext DbContext { get; set; }


    }
}
