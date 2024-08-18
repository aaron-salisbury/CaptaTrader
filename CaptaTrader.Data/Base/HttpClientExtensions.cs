using System;

namespace CaptaTrader.Data.Base
{
    internal static class HttpClientExtensions
    {
        /// <summary>
        /// Downloads the specified resource to a local file as an asynchronous operation.
        /// </summary>
        internal static async Task DownloadFileTaskAsync(this HttpClient client, Uri address, string fileName)
        {
            // Tony's answer at https://stackoverflow.com/questions/45711428/download-file-with-webclient-or-httpclient

            using (Stream stream = await client.GetStreamAsync(address))
            using (FileStream fileStream = new(fileName, FileMode.CreateNew))
            {
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}
