using System;

namespace CaptaTrader.Data.Domains
{
    [Serializable]
    internal class InternalStorage
    {
        public string UserStorageDirectory { get; set; } = null!;
    }
}
