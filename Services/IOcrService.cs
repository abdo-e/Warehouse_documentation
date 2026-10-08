namespace WareDocs.Api.Services;

public interface IOcrService
{
    /// <summary>
    /// Extract text from an uploaded file (PDF or image).
    /// </summary>
    Task<string> ExtractTextAsync(Stream fileStream, string fileName);
}
