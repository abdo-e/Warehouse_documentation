using System.Text.Json.Serialization;

namespace WareDocs.Api.Models;

public class SkuMatchRequest
{
    [JsonPropertyName("sku1")]
    public string Sku1 { get; set; } = string.Empty;

    [JsonPropertyName("sku2")]
    public string Sku2 { get; set; } = string.Empty;
}

public class SkuMatchResult
{
    [JsonPropertyName("same")]
    public bool Same { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}
