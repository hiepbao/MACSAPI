using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using test2.Helpers;
using ZXing;

namespace MACSAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QRCodeController : Controller
    {
        private readonly ILogger<QRCodeController> _logger;

        public QRCodeController(ILogger<QRCodeController> logger)
        {
            _logger = logger;
        }
        private Bitmap PreprocessImage(Bitmap original)
        {
            // Tạo một ảnh bitmap mới với cùng kích thước
            Bitmap processed = new Bitmap(original.Width, original.Height);

            using (Graphics g = Graphics.FromImage(processed))
            {
                g.Clear(Color.White); // Tô màu trắng cho nền
                g.DrawImage(original, 0, 0, original.Width, original.Height); // Vẽ lại ảnh gốc
            }

            return processed;
        }

        [HttpPost("ScanQR")]
        public IActionResult ScanQR([FromForm] IFormFile qrImage)
        {
            if (qrImage == null || qrImage.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng tải lên một hình ảnh." });
            }

            try
            {
                using (var stream = qrImage.OpenReadStream())
                using (var bitmap = new Bitmap(stream))
                {
                    // Xử lý hình ảnh trước khi quét
                    var preprocessedBitmap = PreprocessImage(bitmap);
                    // Sử dụng ZXing.Net để quét mã QR
                    var reader = new BarcodeReaderGeneric
                    {
                        AutoRotate = true,
                        TryInverted = true,
                        Options = new ZXing.Common.DecodingOptions
                        {
                            PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
                            TryHarder = true
                        }
                    };
                    var luminanceSource = new BitmapLuminanceSource(preprocessedBitmap);

                    // Decode trực tiếp từ LuminanceSource
                    var result = reader.Decode(luminanceSource);

                    if (result != null)
                    {
                        // Tách dữ liệu mã QR nếu có dấu phân cách
                        var data = result.Text.Contains("|") ? result.Text.Split("|") : new[] { result.Text };

                        return Ok(new { message = "QR Code data found.", data });
                    }
                    else
                    {
                        return BadRequest(new { message = "Không tìm thấy mã QR trong hình ảnh. Vui lòng thử lại." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi xảy ra khi đọc mã QR: {ex.Message}" });
            }
        }
    }
}
