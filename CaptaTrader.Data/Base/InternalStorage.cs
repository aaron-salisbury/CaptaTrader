using System;

namespace CaptaTrader.Data.Base
{
    [Serializable]
    internal class InternalStorage
    {
        public required string UserStorageDirectory { get; set; }
    }
}
