using System.Text.Json.Serialization;

namespace WareDocs.Api.Models;

public class DetectionResult
{
    [JsonPropertyName("documentType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DocumentType DocumentType { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}
