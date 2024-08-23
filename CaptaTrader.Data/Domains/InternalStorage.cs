using System;

namespace CaptaTrader.Data.Domains
{
    [Serializable]
    internal class InternalStorage
    {
        public required string UserStorageDirectory { get; set; }
    }
}
