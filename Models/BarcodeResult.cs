using System.Text.Json.Serialization;

namespace WareDocs.Api.Models;

public class BarcodeResult
{
    [JsonPropertyName("barcode")]
    public string Barcode { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }
}
