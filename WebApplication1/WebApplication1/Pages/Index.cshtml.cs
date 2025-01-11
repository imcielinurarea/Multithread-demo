using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public string? DownloadResult { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Có thể trả về một trang thông tin hoặc dữ liệu ban đầu
        }

        public async Task<IActionResult> OnPostAsync(string urls, int connections)
        {
            var urlList = urls.Split(',')
                              .Select(url => url.Trim())
                              .ToArray();

            try
            {
                // Tạo danh sách các tác vụ tải file
                var downloadTasks = urlList.Select(url =>
                {
                    return Task.Run(() =>
                    {
                        try
                        {
                            // Khởi tạo MultiFileDownload trực tiếp trong phương thức này
                            var downloader = new MultiFileDownload(url, connections);
                            downloader.DownloadFile();
                            _logger.LogInformation($"Download successful for: {url}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error downloading {url}: {ex.Message}");
                        }
                    });
                }).ToArray();

                // Chờ tất cả các tác vụ hoàn thành
                await Task.WhenAll(downloadTasks);

                DownloadResult = "Files downloaded successfully!";
            }
            catch (Exception ex)
            {
                DownloadResult = $"Error: {ex.Message}";
                _logger.LogError($"An error occurred while downloading files: {ex.Message}");
            }

            return Page(); // Trả về trang hiện tại với kết quả tải
        }
    }
}
