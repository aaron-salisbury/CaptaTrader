using CsvHelper;
using CsvHelper.Configuration;
using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ROSIO = RunnethOverStudio.AppToolkit.Modules.DataAccess.IO;

namespace CaptaTrader.Data.Base;

public static class IO
{
    public static async Task WriteToCSVAsync<T>(IEnumerable<T> objects, ILogger logger, string? writePath = null, ClassMap<T>? classMap = null)
    {
        try
        {
            writePath ??= ROSIO.GetAppDirectoryPath();
            string beingWritten = typeof(T).Name.Pluralize().Humanize(LetterCasing.Title);
            string writeTime = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            string fileName = $"{beingWritten} - {writeTime}.csv";
            string fullPath = Path.Join(writePath, fileName);

            using StreamWriter writer = new(fullPath);
            using CsvWriter csv = new(writer, CultureInfo.InvariantCulture);
            if (classMap != null)
            {
                csv.Context.RegisterClassMap(classMap);
            }

            await csv.WriteRecordsAsync(objects);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write objects to CSV file.");
        }
    }
}
