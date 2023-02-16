using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using zip;

namespace Predator
{
    public class DownloadResult
    {
        public long Size { get; set; }
        public string FilePath { get; set; }
        public TimeSpan TimeTaken { get; set; }
        public int ParallelDownloads { get; set; }
    }

    internal class Range
    {
        public long Start { get; set; }
        public long End { get; set; }
    }

    public static class Downloader
    {
        public static bool extractInProgress;
        public static bool cancel = false;

        static Downloader()
        {
            ServicePointManager.DefaultConnectionLimit = 100;
        }

        public static long getFileSize(string url)
        {
            var webRequest = WebRequest.Create(url);
            webRequest.Method = "HEAD";
            using (var webResponse = webRequest.GetResponse())
            {
                return long.Parse(webResponse.Headers.Get("Content-Length"));
            }
        }

        public static DownloadResult Download(string url, string out_dir, int chunks = 0)
        {
            extractInProgress = true;
            Directory.CreateDirectory(out_dir);
            var filepath = Path.Combine(out_dir, new Uri(url).Segments.Last());
            var result = new DownloadResult { FilePath = filepath };
            var responseLength = getFileSize(url);
            result.Size = responseLength;
            chunks = 10;
            if (responseLength < 1000000) chunks = 2;
            if (chunks < 2) chunks = Environment.ProcessorCount;
            if (File.Exists(filepath)) File.Delete(filepath);
            var temp = new ConcurrentDictionary<int, string>();

            #region Calculate ranges

            var readRanges = new List<Range>();
            var index_list = new Dictionary<int, int>();
            for (var chunk = 0; chunk < chunks - 1; chunk++)
            {
                var range = new Range
                {
                    Start = chunk * (responseLength / chunks),
                    End = (chunk + 1) * (responseLength / chunks) - 1
                };
                readRanges.Add(range);
                index_list.Add((int)(chunk * (responseLength / chunks)), chunk);
            }

            readRanges.Add(new Range
            {
                Start = readRanges.Any() ? readRanges.Last().End + 1 : 0,
                End = responseLength - 1
            });
            index_list.Add((int)readRanges.Last().Start, chunks - 1);

            #endregion

            var startTime = DateTime.Now;

            #region Parallel download

            var thread = new List<Thread>();
            foreach (var readRange in readRanges)
                thread.Add(new Thread(() =>
                {
                    var tempfile = $"{result.FilePath}{(int)readRange.Start}";
                    temp.TryAdd(index_list[(int)readRange.Start], tempfile);
                    while (true)
                        try
                        {
                            if (cancel) return;
                            var httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                            httpWebRequest.Method = "GET";
                            httpWebRequest.Timeout =
                                Timeout
                                    .Infinite;
                            httpWebRequest.AddRange(readRange.Start, readRange.End);
                            using (var httpWebResponse = httpWebRequest.GetResponse())
                            {
                                using (var fileStream = new FileStream(tempfile, FileMode.Create, FileAccess.Write,
                                           FileShare.Write))
                                {
                                    httpWebResponse.GetResponseStream().CopyTo(fileStream);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            h.log(ex);
                        }
                }));
            foreach (var t in thread) t.Start();
            foreach (var t in thread) t.Join();
            result.ParallelDownloads = chunks;

            #endregion

            result.TimeTaken = DateTime.Now.Subtract(startTime);

            #region Merge to single file

            using (var outFile = new FileStream(filepath, FileMode.Append))
            {
                for (var i = 0; i < temp.Count; i++)
                {
                    var tempFilename = temp.ElementAt(i).Value;
                    using (var tempFile = new FileStream(tempFilename, FileMode.Open))
                    {
                        tempFile.CopyTo(outFile);
                    }

                    File.Delete(tempFilename);
                }
            }

            #endregion

            #region Extract

            if (result.FilePath.EndsWith(".zip"))
            {
                using (var extractor = ZipStorer.Open(result.FilePath, FileAccess.Read))
                {
                    foreach (var entry in extractor.ReadCentralDir())
                        extractor.ExtractFile(entry,
                            Path.Combine(Path.GetDirectoryName(result.FilePath), entry.FilenameInZip));
                }

                File.Delete(result.FilePath);
            }

            extractInProgress = false;

            #endregion

            return result;
        }
    }
}