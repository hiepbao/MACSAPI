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
                //foreach (var claim in HttpContext.User.Claims)
                //{
                //    _logger.LogInformation("Claim Type: {Type}, Claim Value: {Value}", claim.Type, claim.Value);
                //    Console.WriteLine(claim);
                //}

                var username = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Username")?.Value ?? "Anonymous";
                var fullname = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Anonymous";
                var fileSizeInKB = zipFile.Length / 1024.0;
                DateTime now = DateTime.Now;
                string formattedDate = now.ToString("dd/MM/yyyy HH:mm:ss");
                using (_logger.BeginScope(new Dictionary<string, object> { { "UploadContext", true } }))
                {
                    _logger.LogInformation("Tài khoản {UserName} tên {FullName} uploaded file {FileName} dung lượng {FileSize:0.00} KB chứa {FileCount} file vào lúc {UploadTime}. Chi tiết file: {ExtractedFiles}",
                                            username,
                                            fullname,
                                            zipFile.FileName,
                                            fileSizeInKB,
                                            savedFiles.Count,
                                            formattedDate,
                                            string.Join(", ", savedFiles));
                }
                return Ok(new
                {
                    message = "Xử lý thành công .zip",
                    files = savedFiles
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



    }
}
