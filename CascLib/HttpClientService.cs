using System;
using System.Net;
using System.Net.Http;
using System.Threading;

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
        return new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
        })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }
}
