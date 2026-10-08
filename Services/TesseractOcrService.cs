using System.Text;
using Tesseract;
using UglyToad.PdfPig;

namespace WareDocs.Api.Services;

public class TesseractOcrService : IOcrService, IDisposable
{
    private readonly TesseractEngine _engine;
    private readonly ILogger<TesseractOcrService> _logger;

    public TesseractOcrService(IConfiguration config, ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
        var tessDataPath = config["Ocr:TessDataPath"] ?? "./tessdata";
        var language = config["Ocr:Language"] ?? "eng";
        _engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => await ExtractFromPdfAsync(fileStream),
            ".png" or ".jpg" or ".jpeg" or ".tiff" or ".tif" or ".bmp" or ".webp"
                => await ExtractFromImageAsync(fileStream),
            _ => throw new ArgumentException($"Unsupported file type: {extension}")
        };
    }

    private async Task<string> ExtractFromPdfAsync(Stream stream)
    {
        // First try PdfPig text-layer extraction
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        ms.Position = 0;

        var sb = new StringBuilder();
        try
        {
            using var document = PdfDocument.Open(ms);
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PdfPig text extraction failed, falling back to OCR");
        }

        var textLayerText = sb.ToString().Trim();

        // If text layer has enough content, use it
        if (textLayerText.Length > 50)
        {
            _logger.LogInformation("Extracted {Length} chars from PDF text layer", textLayerText.Length);
            return textLayerText;
        }

        // Fallback: OCR the PDF (treat as image — simplified for MVP)
        _logger.LogInformation("PDF text layer too short ({Length} chars), falling back to OCR", textLayerText.Length);
        ms.Position = 0;
        return await ExtractFromImageAsync(ms);
    }

    private Task<string> ExtractFromImageAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var imageBytes = ms.ToArray();

        using var pix = Pix.LoadFromMemory(imageBytes);
        using var page = _engine.Process(pix);
        var text = page.GetText();

        _logger.LogInformation("OCR extracted {Length} chars (confidence: {Confidence:F1}%)",
            text.Length, page.GetMeanConfidence() * 100);

        return Task.FromResult(text);
    }

    public void Dispose()
    {
        _engine?.Dispose();
        GC.SuppressFinalize(this);
    }
}
