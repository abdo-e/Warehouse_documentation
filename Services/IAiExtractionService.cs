using WareDocs.Api.Models;

namespace WareDocs.Api.Services;

public interface IAiExtractionService
{
    /// <summary>
    /// Detect the document type from extracted text.
    /// </summary>
    Task<DetectionResult> DetectTypeAsync(string text);

    /// <summary>
    /// Extract structured data from document text.
    /// </summary>
    Task<ExtractionResult> ExtractAsync(string text, DocumentType? typeHint = null);
}
