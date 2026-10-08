using WareDocs.Api.Models;

namespace WareDocs.Api.Services;

public interface IBarcodeService
{
    /// <summary>
    /// Decode barcodes/QR codes from an uploaded image.
    /// </summary>
    Task<BarcodeResult?> DecodeAsync(Stream imageStream, string fileName);
}
