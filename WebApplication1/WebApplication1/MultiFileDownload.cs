using System;
using System.IO;
using System.Net;
using System.Threading;

namespace WebApplication1
{
    public class MultiFileDownload
    {
        private readonly string url;
        private readonly int threadCount;
        private ManualResetEvent[] resetEvents;

        public MultiFileDownload(string url, int threadCount)
        {
            this.url = url;
            this.threadCount = threadCount;
        }

        public void DownloadFile()
        {
            // Tạo tên file từ URL
            string fileName = Path.GetFileName(url);
            // Lấy thư mục Downloads của người dùng
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            // Tạo đường dẫn đầy đủ cho file
            string outputFile = Path.Combine(downloadsPath, fileName);

            string directoryPath = Path.GetDirectoryName(outputFile);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            long fileSize = GetFileSize(url); // Lấy kích thước file từ URL (nếu có)

            if (fileSize <= 0)
            {
                Console.WriteLine("Unable to retrieve file size or file size is unknown.");
            }
            else
            {
                Console.WriteLine($"File size: {fileSize} bytes.");
            }

            long chunkSize = fileSize / threadCount;
            resetEvents = new ManualResetEvent[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                long start = i * chunkSize;
                long end = (i == threadCount - 1) ? fileSize - 1 : start + chunkSize - 1;

                resetEvents[i] = new ManualResetEvent(false);
                var downloader = new ChunkDownloader(url, outputFile, start, end, resetEvents[i], i);
                Thread thread = new Thread(downloader.DownloadChunk);
                thread.Start();
            }

            WaitHandle.WaitAll(resetEvents);
            Console.WriteLine("Download completed!");
        }

        // Lấy kích thước file từ header nếu có
        private long GetFileSize(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET"; // Chuyển sang GET

                request.Timeout = 60000;
                request.ReadWriteTimeout = 60000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Đọc dữ liệu từ luồng để tính kích thước
                        MemoryStream memoryStream = new MemoryStream();
                        responseStream.CopyTo(memoryStream);
                        return memoryStream.Length; // Trả về kích thước tệp
                    }
                    else
                    {
                        Console.WriteLine($"Server returned status code: {response.StatusCode}");
                        return -1;
                    }
                }
            }
            catch (WebException webEx)
            {
                Console.WriteLine($"WebException: {webEx.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file size: {ex.Message}");
                return -1;
            }
        }
    }

    class ChunkDownloader
    {
        private readonly string url;
        private readonly string outputFile;
        private readonly long start;
        private readonly long end;
        private readonly ManualResetEvent resetEvent;
        private readonly int chunkIndex;

        public ChunkDownloader(string url, string outputFile, long start, long end, ManualResetEvent resetEvent, int chunkIndex)
        {
            this.url = url;
            this.outputFile = SanitizeFileName(outputFile); // Gọi hàm lọc ký tự không hợp lệ
            this.start = start;
            this.end = end;
            this.resetEvent = resetEvent;
            this.chunkIndex = chunkIndex;
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }

        public void DownloadChunk()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AddRange(start, end); // Thêm header Range để tải phần tệp

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (FileStream fileStream = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    long position = start;
                    fileStream.Seek(position, SeekOrigin.Begin);

                    // Đọc và ghi dữ liệu vào file mà không cần biết trước kích thước tệp
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        lock (fileStream)
                        {
                            fileStream.Write(buffer, 0, bytesRead);
                        }
                        position += bytesRead;
                    }
                }

                Console.WriteLine($"Chunk {chunkIndex} downloaded from byte {start} to {end}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading chunk {chunkIndex}: {ex.Message}");
            }
            finally
            {
                resetEvent.Set(); // Đánh dấu chunk này đã hoàn thành
            }
        }
    }
}