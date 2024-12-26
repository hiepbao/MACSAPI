using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using ZXing;
using ZXing.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing.ImageSharp;


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
                {
                    // Đọc hình ảnh bằng ImageSharp
                    using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream))
                    {
                        // Chuyển đổi ảnh sang grayscale
                        image.Mutate(x => x.Grayscale());

                        // Lấy kích thước ảnh
                        int width = image.Width;
                        int height = image.Height;

                        if (width < 150 || height < 150)
                        {
                            return BadRequest(new { message = "Hình ảnh quá nhỏ để quét mã QR. Vui lòng sử dụng ảnh lớn hơn." });
                        }

                        // Chuyển đổi ảnh sang mảng byte
                        byte[] pixelData = new byte[width * height];
                        image.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < height; y++)
                            {
                                var row = accessor.GetRowSpan(y);
                                for (int x = 0; x < width; x++)
                                {
                                    var pixel = row[x];
                                    pixelData[y * width + x] = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                                }
                            }
                        });

                        // Tạo LuminanceSource từ pixelData
                        var luminanceSource = new RGBLuminanceSource(pixelData, width, height);

                        // Khởi tạo BarcodeReader từ ZXing
                        var reader = new ZXing.BarcodeReaderGeneric
                        {
                            AutoRotate = true,
                            TryInverted = true,
                            Options = new ZXing.Common.DecodingOptions
                            {
                                PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
                                TryHarder = true
                            }
                        };

                        // Giải mã
                        var result = reader.Decode(luminanceSource);

                        if (result != null)
                        {
                            var data = result.Text.Contains("|") ? result.Text.Split("|") : new[] { result.Text };
                            return Ok(new { message = "QR Code data found.", data });
                        }
                        else
                        {
                            return BadRequest(new { message = "Không tìm thấy mã QR trong hình ảnh." });
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                return StatusCode(500, new { message = $"Lỗi xảy ra khi đọc mã QR: {ex.Message}. Hãy kiểm tra lại hình ảnh." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi không xác định: {ex.Message}" });
            }
        }



        //[HttpPost("ScanQR")]
        //public IActionResult ScanQR([FromForm] IFormFile qrImage)
        //{
        //    if (qrImage == null || qrImage.Length == 0)
        //    {
        //        return BadRequest(new { message = "Vui lòng tải lên một hình ảnh." });
        //    }

        //    try
        //    {
        //        using (var stream = qrImage.OpenReadStream())
        //        {
        //            // Đọc và xử lý hình ảnh bằng ImageSharp
        //            using (var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream))
        //            {
        //                // Chuyển đổi hình ảnh sang định dạng xám
        //                image.Mutate(x => x.Grayscale());

        //                int width = image.Width;
        //                int height = image.Height;

        //                // Chuyển đổi ảnh thành mảng byte (grayscale)
        //                byte[] pixelData = new byte[width * height];

        //                image.ProcessPixelRows(accessor =>
        //                {
        //                    for (int y = 0; y < height; y++)
        //                    {
        //                        var row = accessor.GetRowSpan(y);
        //                        for (int x = 0; x < width; x++)
        //                        {
        //                            var pixel = row[x];
        //                            // Tính giá trị grayscale
        //                            pixelData[y * width + x] = (byte)((pixel.R + pixel.G + pixel.B) / 3);
        //                        }
        //                    }
        //                });


        //                // Sử dụng ZXing để giải mã
        //                var luminanceSource = new RGBLuminanceSource(pixelData, width, height);
        //                var reader = new BarcodeReaderGeneric
        //                {
        //                    AutoRotate = true,
        //                    TryInverted = true,
        //                    Options = new DecodingOptions
        //                    {
        //                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
        //                        TryHarder = true
        //                    }
        //                };
        //                var result = reader.Decode(luminanceSource);
        //                if (width < 150 || height < 150)
        //                {
        //                    return BadRequest(new { message = "Hình ảnh quá nhỏ, vui lòng tải lên mã QR lớn hơn." });
        //                }
        //                if (result != null)
        //                {
        //                    // Tách dữ liệu mã QR nếu có dấu phân cách
        //                    var data = result.Text.Contains("|") ? result.Text.Split("|") : new[] { result.Text };

        //                    return Ok(new { message = "QR Code data found.", data });
        //                }
        //                else
        //                {
        //                    return BadRequest(new { message = "Không tìm thấy mã QR trong hình ảnh. Vui lòng thử lại." });
        //                }
        //            }
        //        }
        //    }
        //    catch (IndexOutOfRangeException ex)
        //    {
        //        // Xử lý lỗi do truy cập vượt giới hạn
        //        return StatusCode(500, new { message = $"Lỗi xảy ra khi đọc mã QR: {ex.Message}. Hãy kiểm tra lại hình ảnh." });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Xử lý lỗi không xác định
        //        return StatusCode(500, new { message = $"Lỗi không xác định: {ex.Message}" });
        //    }
        //}


    }
}
