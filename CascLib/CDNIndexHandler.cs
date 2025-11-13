using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.Http;

namespace CASCLib
{
    public struct IndexEntry
    {
        public int Index;
        public int Offset;
        public int Size;

        public bool IsValid => Size != 0;
    }

    public class CDNIndexHandler : IndexHandlerBase
    {
        private ProgressReporter worker;

        private CDNIndexHandler(CASCConfig cascConfig, ProgressReporter worker)
        {
            config = cascConfig;
            this.worker = worker;
        }

        public static CDNIndexHandler Initialize(CASCConfig config, ProgressReporter worker)
        {
            var handler = new CDNIndexHandler(config, worker);

            if (!config.OnlineMode && !string.IsNullOrEmpty(config.ArchiveGroup))
            {
                string groupArchivePath = Path.Combine(config.BasePath, CASCGame.GetDataFolder(config.GameType), "indices", config.ArchiveGroup + ".index");
                if (File.Exists(groupArchivePath))
                {
                    worker?.Start(0, "Loading \"CDN group index\"...", ProgressStage.CDNIndexes);
                    handler.OpenIndexFile(config.ArchiveGroup, -1);
                    worker?.Report(100, progressStage: ProgressStage.CDNIndexes);

                    return handler;
                }
            }

            worker?.Start(0, "Loading \"CDN indexes\"...", ProgressStage.CDNIndexes);

            for (int i = 0; i < config.Archives.Count; i++)
            {
                string archive = config.Archives[i];

                if (config.OnlineMode)
                    handler.DownloadIndexFile(archive, i);
                else
                    handler.OpenIndexFile(archive, i);

                worker?.Report((int)((i + 1) / (float)config.Archives.Count * 100), progressStage: ProgressStage.CDNIndexes);
            }

            return handler;
        }

        public Stream OpenDataFile(IndexEntry entry)
        {
            string archive = config.Archives[entry.Index];

            string file = Utils.MakeCDNPath(config.CDNPath, "data", archive);

            MemoryMappedFile dataFile = CDNCache.Instance.OpenDataFile(file);

            if (dataFile != null)
            {
                var accessor = dataFile.CreateViewStream(entry.Offset, entry.Size, MemoryMappedFileAccess.Read);
                return accessor;
            }

            //using (HttpClient client = new HttpClient())
            //{
            //    client.DefaultRequestHeaders.Range = new RangeHeaderValue(entry.Offset, entry.Offset + entry.Size - 1);

            //    var resp = client.GetStreamAsync(url).Result;

            //    MemoryStream ms = new MemoryStream(entry.Size);
            //    resp.CopyBytes(ms, entry.Size);
            //    ms.Position = 0;
            //    return ms;
            //}

            try
            {
                using (var resp = Utils.HttpWebResponseGetWithRange(() => Utils.MakeCDNUrl(config.CDNHost, file), entry.Offset, entry.Offset + entry.Size - 1))
                using (Stream rstream = resp.Content.ReadAsStream())
                {
                    return rstream.CopyBytesToMemoryStream(entry.Size);
                }
            }
            catch (HttpRequestException exc)
            {
                Logger.WriteLine($"CDNIndexHandler: error while opening {file}: Status {exc.Message}, StatusCode {exc.StatusCode}");
                return null;
            }
        }

        public Stream OpenDataFileDirect(in MD5Hash key)
        {
            var keyStr = key.ToHexString().ToLower();

            worker?.Start(0, string.Format("Downloading \"{0}\" file...", keyStr));

            string file = Utils.MakeCDNPath(config.CDNPath, "data", keyStr);

            Stream stream = CDNCache.Instance.OpenFile(file);

            stream ??= OpenFile(() => Utils.MakeCDNUrl(config.CDNHost, file));

            worker?.Report(100);
            return stream;
        }

        public static Stream OpenConfigFileDirect(CASCConfig cfg, string key)
        {
            string file = Utils.MakeCDNPath(cfg.CDNPath, "config", key);

            Stream stream = CDNCache.Instance.OpenFile(file);

            if (stream != null)
                return stream;

            return OpenFileDirect(() => Utils.MakeCDNUrl(cfg.CDNHost, file));
        }

        public static Stream OpenFileDirect(Func<string> getUrlFunc)
        {
            //using (HttpClient client = new HttpClient())
            //{
            //    var resp = client.GetStreamAsync(url).Result;

            //    MemoryStream ms = new MemoryStream();
            //    resp.CopyTo(ms);
            //    ms.Position = 0;
            //    return ms;
            //}

            using (var resp = Utils.HttpWebResponseGet(getUrlFunc))
            using (Stream stream = resp.Content.ReadAsStream())
            {
                return stream.CopyToMemoryStream(resp.Content.Headers.ContentLength ?? 0);
            }
        }

        public IndexEntry GetIndexInfo(in MD5Hash eKey)
        {
            if (!indexData.TryGetValue(eKey, out IndexEntry result))
                Logger.WriteLine("CDNIndexHandler: missing EKey: {0}", eKey.ToHexString());

            return result;
        }

        public void Clear()
        {
            indexData.Clear();
            indexData = null;

            config = null;
            worker = null;
        }
    }
}
