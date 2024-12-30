using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.IO.Compression;

namespace MACSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : Controller
    {
        [HttpPost("uploadZip")]
        public async Task<IActionResult> UploadZipAsync(IFormFile zipFile)
        {
            ILogger<FileController> _logger = HttpContext.RequestServices.GetRequiredService<ILogger<FileController>>();

            if (zipFile == null || zipFile.Length == 0)
            {
                _logger.LogWarning("User attempted to upload an empty or null file.");
                return BadRequest(new { message = "Không có file tải lên" });
            }

            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("User uploaded a file that is not a .zip: {FileName}", zipFile.FileName);
                return BadRequest(new { message = "Vui lòng tải lên file .zip" });
            }

            try
            {
                // Lấy đường dẫn thư mục wwwroot/UploadedFiles
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var targetFolder = Path.Combine(wwwrootPath, "UploadedFiles");
                var targetFolder2 = Path.Combine(wwwrootPath, "Logs");

                var csvFilePath = Path.Combine(targetFolder2, "UploadHistory.csv");

                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                    _logger.LogInformation("Created directory: {Directory}", targetFolder);
                }

                // Danh sách chứa tên các tệp đã lưu
                var savedFiles = new List<string>();

                // Đọc file .zip từ stream
                using (var stream = new MemoryStream())
                {
                    await zipFile.CopyToAsync(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    using (var archive = new ZipArchive(stream))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            // Bỏ qua các thư mục (entry không có Name là thư mục)
                            if (string.IsNullOrEmpty(entry.Name)) continue;

                            // Kiểm tra tên file hợp lệ
                            var fileName = Path.GetInvalidFileNameChars()
                                .Aggregate(entry.Name, (current, c) => current.Replace(c.ToString(), "_"));

                            var destinationFileName = Path.Combine(targetFolder, fileName);

                            // Đảm bảo không ghi đè nếu file đã tồn tại
                            if (System.IO.File.Exists(destinationFileName))
                            {
                                destinationFileName = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}");
                            }

                            // Giải nén file
                            using (var entryStream = entry.Open())
                            using (var fileStream = new FileStream(destinationFileName, FileMode.Create))
                            {
                                await entryStream.CopyToAsync(fileStream);
                            }

                            // Thêm đường dẫn tương đối vào danh sách
                            savedFiles.Add(Path.Combine("UploadedFiles", Path.GetFileName(destinationFileName)).Replace("\\", "/"));
                        }
                    }
                }

                // Ghi log thông tin chi tiết về file upload
                
                var username = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Username")?.Value ?? "Anonymous";
                var fullname = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Anonymous";
                var fileSizeInKB = zipFile.Length / 1024.0;
                DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); ;
                string formattedDate = now.ToString("dd/MM/yyyy HH:mm:ss");
                // Lấy danh sách tên file từ savedFiles
                string savedFileList = string.Join(", ", savedFiles.Select(file => Path.GetFileName(file)));
                using (_logger.BeginScope(new Dictionary<string, object> { { "UploadContext", true } }))
                {
                    _logger.LogInformation("Tài khoản {UserName} tên {FullName} uploaded file {FileName} dung lượng {FileSize:0.00} KB chứa {FileCount} file vào lúc {UploadTime}. Chi tiết file: {ExtractedFiles}",
                                            username,
                                            fullname,
                                            zipFile.FileName,
                                            fileSizeInKB,
                                            savedFiles.Count,
                                            formattedDate,
                                            savedFileList);
                }

                // Ghi thông tin vào file CSV
                AppendToCsv(csvFilePath, username, fullname, zipFile.FileName, fileSizeInKB, savedFiles.Count, formattedDate, savedFileList);
                return Ok(new
                {
                    message = "Xử lý thành công .zip",
                    files = savedFiles,
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Permission denied while processing .zip upload.");
                return StatusCode(403, new { message = "Không đủ quyền để ghi vào thư mục", error = ex.Message });
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error while processing .zip upload.");
                return StatusCode(500, new { message = "Lỗi IO khi xử lý file", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing .zip upload.");
                return StatusCode(500, new { message = "Lỗi xử lý .zip", error = ex.Message });
            }
        }


        [HttpGet("readUploadHistory")]
        public IActionResult ReadUploadHistory()
        {
            // Đường dẫn tới file CSV
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Logs", "UploadHistory.csv");

            // Kiểm tra file tồn tại
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "File lịch sử không tồn tại." });

            var data = new List<dynamic>();

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string[] headers = null;
                    int lineNumber = 0;

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        // Dùng Split với tham số StringSplitOptions.RemoveEmptyEntries để đảm bảo không bị lỗi do khoảng trắng
                        var values = line.Split(new[] { ',' }, StringSplitOptions.None);

                        if (lineNumber == 0)
                        {
                            // Đọc dòng đầu tiên làm tiêu đề cột
                            headers = values;
                        }
                        else
                        {
                            // Đọc từng dòng và chuyển thành object
                            var record = new Dictionary<string, object>();
                            for (int i = 0; i < headers.Length - 1; i++) // Lặp qua các cột ngoài cột cuối
                            {
                                record[headers[i].Trim()] = values[i].Trim();
                            }

                            // Cột cuối cùng (FileList): Lấy từ cột thứ [headers.Length - 1] và ghép tất cả các giá trị còn lại
                            var fileListIndex = headers.Length - 1;
                            record[headers[fileListIndex].Trim()] = values
                                .Skip(fileListIndex) // Bỏ qua các cột trước đó
                                .Select(file => file.Trim()) // Loại bỏ khoảng trắng
                                .ToList(); // Chuyển thành danh sách

                            data.Add(record);
                        }
                        lineNumber++;
                    }
                }

                return Ok(data); // Trả dữ liệu JSON
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi đọc file CSV", error = ex.Message });
            }
        }



        private void AppendToCsv(string csvFilePath, string username, string fullname, string fileName, double fileSize, int fileCount, string uploadTime, string fileList)
        {
            var fileExists = System.IO.File.Exists(csvFilePath);

            using (var writer = new StreamWriter(csvFilePath, append: true))
            {
                // Nếu file chưa tồn tại, ghi tiêu đề cột
                if (!fileExists)
                {
                    writer.WriteLine("Username,FullName,FileName,FileSizeKB,FileCount,UploadTime,FileList");
                }

                // Ghi thông tin chi tiết vào CSV
                writer.WriteLine($"{username},{fullname},{fileName},{fileSize:F2},{fileCount},{uploadTime},{fileList}");
            }
        }



    }
}
