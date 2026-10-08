using System.Text.Json.Serialization;

namespace WareDocs.Api.Models;

// --- Specific document extraction results ---

public class DeliveryNoteResult
{
    [JsonPropertyName("supplier")]
    public string Supplier { get; set; } = string.Empty;

    [JsonPropertyName("deliveryNumber")]
    public string DeliveryNumber { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<LineItem> Items { get; set; } = [];
}

public class PurchaseOrderResult
{
    [JsonPropertyName("poNumber")]
    public string PoNumber { get; set; } = string.Empty;

    [JsonPropertyName("supplier")]
    public string Supplier { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<LineItem> Items { get; set; } = [];
}

public class InvoiceResult
{
    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("supplier")]
    public string Supplier { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<LineItem> Items { get; set; } = [];
}

// --- Unified extraction result ---

public class ExtractionResult
{
    [JsonPropertyName("documentType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DocumentType DocumentType { get; set; }

    [JsonPropertyName("supplier")]
    public string Supplier { get; set; } = string.Empty;

    [JsonPropertyName("documentNumber")]
    public string DocumentNumber { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }

    [JsonPropertyName("items")]
    public List<LineItem> Items { get; set; } = [];
}
