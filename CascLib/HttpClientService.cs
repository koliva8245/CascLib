using System;
using System.Net.Http;

#nullable enable

namespace CASCLib;

internal static class HttpClientService
{
    private static HttpClient? _httpClient;

    public static HttpClient Instance
    {
        get
        {
            if (_httpClient is null)
                throw new InvalidOperationException("HttpClient not set. Please set it via SetHttpClient method before using.");

            return _httpClient;
        }
    }

    public static void SetHttpClient(HttpClient httpClient)
    {
        _httpClient ??= httpClient;
    }
}
