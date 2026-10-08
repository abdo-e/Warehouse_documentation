using System.Text.Json.Serialization;

namespace WareDocs.Api.Models;

public class MatchResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok"; // "ok" or "warning"

    [JsonPropertyName("missingItems")]
    public List<DiscrepancyItem> MissingItems { get; set; } = [];

    [JsonPropertyName("overShipments")]
    public List<DiscrepancyItem> OverShipments { get; set; } = [];

    [JsonPropertyName("priceMismatches")]
    public List<PriceMismatch> PriceMismatches { get; set; } = [];
}

public class DiscrepancyItem
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("ordered")]
    public decimal Ordered { get; set; }

    [JsonPropertyName("received")]
    public decimal Received { get; set; }

    [JsonPropertyName("invoiced")]
    public decimal Invoiced { get; set; }
}

public class PriceMismatch
{
    [JsonPropertyName("sku")]
    public string Sku { get; set; } = string.Empty;

    [JsonPropertyName("poPrice")]
    public decimal? PoPrice { get; set; }

    [JsonPropertyName("invoicePrice")]
    public decimal? InvoicePrice { get; set; }
}
