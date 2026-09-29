using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CASCLib
{
    internal static class Utils
    {
        public static string MakeCDNPath(string cdnPath, string folder, string fileName)
        {
            return $"{cdnPath}/{folder}/{fileName[..2]}/{fileName.Substring(2, 2)}/{fileName}";
        }

        public static string MakeCDNPath(string cdnPath, string fileName)
        {
            return $"{cdnPath}/{fileName[..2]}/{fileName.Substring(2, 2)}/{fileName}";
        }

        public static string MakeCDNUrl(string cdnHost, string cdnPath)
        {
            return $"http://{cdnHost}/{cdnPath}";
        }

        public static HttpResponseMessage HttpHead(Func<string> getUrlFunc)
        {
            return HttpSend(getUrlFunc, HttpMethod.Head, null, HttpStatusCode.OK);
        }

        public static HttpResponseMessage HttpGet(Func<string> getUrlFunc)
        {
            return HttpSend(getUrlFunc, HttpMethod.Get, null, HttpStatusCode.OK);
        }

        public static HttpResponseMessage HttpGetRange(Func<string> getUrlFunc, long from, long to)
        {
            // a 200 here means the server ignored the Range header and is sending the whole archive
            return HttpSend(getUrlFunc, HttpMethod.Get, new RangeHeaderValue(from, to), HttpStatusCode.PartialContent);
        }

        // GETs the whole body into a seekable MemoryStream and releases the connection
        public static MemoryStream HttpGetBuffered(Func<string> getUrlFunc)
        {
            using HttpResponseMessage response = HttpGet(getUrlFunc);
            using Stream stream = response.Content.ReadAsStream();

            MemoryStream memoryStream = new((int)(response.Content.Headers.ContentLength ?? 0));
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        private static HttpResponseMessage HttpSend(Func<string> getUrlFunc, HttpMethod method, RangeHeaderValue range, HttpStatusCode expectedStatus)
        {
            string url = getUrlFunc();

            HttpRequestMessage request = new(method, url);
            request.Headers.Range = range;

            HttpResponseMessage response;

            try
            {
                response = HttpClientService.Instance.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            }
            catch (TaskCanceledException ex)
            {
                // HttpClient.Timeout surfaces as TaskCanceledException; normalize so callers only catch one type
                throw new HttpRequestException($"{method} {url} timed out", ex);
            }

            if (response.StatusCode != expectedStatus)
            {
                HttpStatusCode status = response.StatusCode;
                response.Dispose();

                throw new HttpRequestException($"{method} {url} returned {(int)status} {status}, expected {(int)expectedStatus}", null, status);
            }

            return response;
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