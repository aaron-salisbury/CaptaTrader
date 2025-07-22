using CaptaTrader.Data.Base;
using System.IO;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Services;

public interface IWebRequester
{
    Task<string?> SendHTTPJsonRequestAsync(string url, HTTPJsonRequest? requestData = null);

    Task DownloadFileAsync(string url, string fullFilePath, bool deletePreexisting = false);

    Task<Stream> GetWebRequestStreamAsync(string url);

    Task<byte[]> GetWebRequestSerializedAsync(string url);
}
