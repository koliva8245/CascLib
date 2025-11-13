using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CASCLib
{
    internal static class Utils
    {
        public static string MakeCDNPath(string cdnPath, string folder, string fileName)
        {
            return $"{cdnPath}/{folder}/{fileName.Substring(0, 2)}/{fileName.Substring(2, 2)}/{fileName}";
        }

        public static string MakeCDNPath(string cdnPath, string fileName)
        {
            return $"{cdnPath}/{fileName.Substring(0, 2)}/{fileName.Substring(2, 2)}/{fileName}";
        }

        public static string MakeCDNUrl(string cdnHost, string cdnPath)
        {
            return $"http://{cdnHost}/{cdnPath}";
        }

        public static HttpResponseMessage HttpWebResponseHead(Func<string> getUrlFunc)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Head, getUrlFunc());

            return HttpClientService.Instance.SendAsync(httpRequestMessage).Result;
        }

        public static HttpResponseMessage HttpWebResponseGet(Func<string> getUrlFunc)
        {
            return HttpClientService.Instance.GetAsync(getUrlFunc()).Result;
        }

        public static HttpResponseMessage HttpWebResponseGetWithRange(Func<string> getUrlFunc, int from, int to)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, getUrlFunc());
            httpRequestMessage.Headers.Range = new RangeHeaderValue(from, to);

            return HttpClientService.Instance.SendAsync(httpRequestMessage).Result;
        }

        // copies whole stream
        public static Stream CopyToMemoryStream(this Stream src, long length, ProgressReporter worker = null)
        {
            MemoryStream ms = new MemoryStream();
            src.CopyToStream(ms, length, worker);
            ms.Position = 0;
            return ms;
        }

        // copies whole stream
        public static MemoryStream CopyToMemoryStream(this Stream src)
        {
            MemoryStream ms = new MemoryStream();
            src.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }

        // copies only numBytes bytes
        public static Stream CopyBytesToMemoryStream(this Stream src, int numBytes)
        {
            MemoryStream ms = new MemoryStream(numBytes);
            src.CopyBytes(ms, numBytes);
            ms.Position = 0;
            return ms;
        }
    }
}
