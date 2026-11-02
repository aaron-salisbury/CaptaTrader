using System.IO;

namespace CaptaTrader.CLIApp.Base;

/// <summary>
/// Only one copy of the app may run per user. Two copies would share one ledger: one could mark the
/// other's in-flight orders as abandoned, and a demo would delete a running demo's folder.
/// </summary>
internal static class SingleInstance
{
    internal const string LOCK_FILE_NAME = "captatrader.lock";

    /// <summary>
    /// Opens the lock file exclusively. The OS releases it when the process exits, even after a crash, so a
    /// stale lock cannot block the next start.
    /// </summary>
    /// <returns>The open lock to hold for the life of the process, or null if another copy holds it.</returns>
    internal static FileStream? TryAcquire(string directory)
    {
        try
        {
            return new FileStream(Path.Combine(directory, LOCK_FILE_NAME), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
