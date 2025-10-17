using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
#if NET9_0_OR_GREATER
using System.Threading;
#endif

namespace CASCLib;

internal static class HttpClientService
{
    private static HttpClient _httpClient;

#if NET9_0_OR_GREATER
    private static readonly Lock _lock = new();
#else
    private static readonly object _lock = new();
#endif

    public static HttpClient Instance
    {
        get
        {
            if (_httpClient is not null)
                return _httpClient;

            lock (_lock)
            {
                _httpClient ??= CreateDefaultClient();
            }

            return _httpClient;
        }
    }

    public static void SetHttpClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient ??= httpClient;
    }

    private static HttpClient CreateDefaultClient()
    {
        HttpClient httpClient = new(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CASCLib", "1.0"));
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(modded fork)"));

        return httpClient;
    }
}
