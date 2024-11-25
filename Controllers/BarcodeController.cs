using GenerateCode.Models;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;

namespace GenerateCode.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BarcodeController : Controller
    {
        [HttpPost("generate")]
        public IActionResult Generate([FromBody] BarcodeRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
            {
                return BadRequest("Invalid data provided. Please provide data to encode.");
            }

            var barcodeWriter = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = request.Width > 0 ? request.Width : 700,
                    Height = request.Height > 0 ? request.Height : 100
                }
            };

            var pixelData = barcodeWriter.Write(request.DeviceId);

            using (var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb))
            {
                using (var ms = new MemoryStream())
                {
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                        ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0,
                            pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    bitmap.Save(ms, ImageFormat.Png);
                    var base64Image = Convert.ToBase64String(ms.ToArray());
                    var imageUrl = $"{Request.Scheme}://{Request.Host}/api/barcode/get/{base64Image}";

                    return Ok(new { Base64Image = base64Image });
                }
            }
        }

        [HttpGet("get/{base64Image}")]
        public IActionResult GetBarcodeImage(string base64Image)
        {
            var imageData = Convert.FromBase64String(base64Image);
            return File(imageData, "image/png");
        }
    }
}
