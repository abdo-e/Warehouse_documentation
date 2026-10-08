using SkiaSharp;
using WareDocs.Api.Models;
using ZXing;
using ZXing.SkiaSharp;

namespace WareDocs.Api.Services;

public class BarcodeService : IBarcodeService
{
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(ILogger<BarcodeService> logger)
    {
        _logger = logger;
    }

    public async Task<BarcodeResult?> DecodeAsync(Stream imageStream, string fileName)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        ms.Position = 0;

        using var image = SKBitmap.Decode(ms);

        if (image == null)
            return null;

        var reader = new BarcodeReader()
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[]
                {
                    BarcodeFormat.EAN_13,
                    BarcodeFormat.EAN_8,
                    BarcodeFormat.CODE_128,
                    BarcodeFormat.CODE_39,
                    BarcodeFormat.QR_CODE,
                    BarcodeFormat.DATA_MATRIX,
                    BarcodeFormat.UPC_A,
                    BarcodeFormat.UPC_E,
                    BarcodeFormat.ITF
                }
            }
        };

        var result = reader.Decode(image);

        if (result == null)
        {
            _logger.LogWarning("No barcode found in {FileName}", fileName);
            return null;
        }

        _logger.LogInformation("Decoded {Format} barcode: {Text}", result.BarcodeFormat, result.Text);

        return new BarcodeResult
        {
            Barcode = result.Text,
            Format = result.BarcodeFormat.ToString()
        };
    }
}
