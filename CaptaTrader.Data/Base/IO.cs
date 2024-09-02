using CsvHelper;
using Humanizer;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CaptaTrader.Data.Base
{
    public static class IO
    {
        private static readonly InternalStorage _internalStorage = Task.Run(() => Json.ReadInternalStorageAsync()).Result;

        public static string GetCurrentUserStorageDirectory() => _internalStorage.UserStorageDirectory;

        public static async Task<bool> UpdateUserStorageDirectoryAsync<T>(string newUserDirectory, ILogger logger)
        {
            bool isSuccessful = false;

            try
            {
                if (!string.IsNullOrEmpty(newUserDirectory))
                {
                    MoveOrDeleteAppData<T>(newUserDirectory, logger);

                    _internalStorage.UserStorageDirectory = newUserDirectory;
                    await Json.UpdateDataAsync<InternalStorage>(_internalStorage, logger);

                    isSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "Failed to update internal storage.");
            }

            return isSuccessful;
        }

        private static void MoveOrDeleteAppData<T>(string userDirectory, ILogger logger)
        {
            try
            {
                if (!string.IsNullOrEmpty(userDirectory))
                {
                    string appDataFileName = Json.GetJsonFileNameForType(typeof(T));
                    string oldAppDataFilePath = Path.Combine(IO.GetCurrentUserStorageDirectory(), appDataFileName);
                    string newAppDataFilePath = Path.Combine(userDirectory, appDataFileName);

                    if (File.Exists(oldAppDataFilePath))
                    {
                        if (!File.Exists(newAppDataFilePath))
                        {
                            Directory.Move(oldAppDataFilePath, newAppDataFilePath);
                        }
                        else
                        {
                            // If the file already exists at the new location, they chose it for a reason; delete the previous one.
                            Json.DeleteDomain<T>(logger);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to move/delete application data.");
            }
        }

        internal static string GetIODirectoryPath<T>()
        {
            if (typeof(T) == typeof(InternalStorage))
            {
                return GetAppDirectoryPath();
            }
            else
            {
                return GetCurrentUserStorageDirectory();
            }
        }

        public static string GetAppDirectoryPath()
        {
            string localAppDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string thisAppName = AppDomain.CurrentDomain.FriendlyName;
            string appDirectory = Path.Combine(localAppDirectory, thisAppName);

            Directory.CreateDirectory(appDirectory);

            return appDirectory;
        }

        public static async Task WriteToCSVAsync<T>(IEnumerable<T> objects, ILogger logger, string? writePath = null)
        {
            try
            {
                writePath ??= GetAppDirectoryPath();
                string beingWritten = typeof(T).Name.Pluralize().Humanize(LetterCasing.Title);
                string writeTime = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
                string fileName = $"{beingWritten} - {writeTime}.csv";
                string fullPath = Path.Join(writePath, fileName);

                using (StreamWriter writer = new(fullPath))
                using (CsvWriter csv = new(writer, CultureInfo.InvariantCulture))
                {
                    await csv.WriteRecordsAsync(objects);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write objects to CSV file.");
            }
        }
    }
}
