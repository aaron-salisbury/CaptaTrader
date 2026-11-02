using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.Integrations;

/// <summary>
/// An <see cref="IHttpClientFactory"/> whose clients answer from a delegate and record every request.
/// </summary>
internal sealed class FakeHttp : IHttpClientFactory
{
    private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _respond;

    internal FakeHttp(Func<HttpRequestMessage, string?, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    internal List<(HttpMethod Method, string Path, string? Body, HttpRequestMessage Request)> Requests { get; } = [];

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new Handler(this), disposeHandler: true);
    }

    internal static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private sealed class Handler : HttpMessageHandler
    {
        private readonly FakeHttp _owner;

        internal Handler(FakeHttp owner)
        {
            _owner = owner;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            lock (_owner.Requests)
            {
                _owner.Requests.Add((request.Method, request.RequestUri!.AbsolutePath, body, request));
            }

            return _owner._respond(request, body);
        }
    }
}
