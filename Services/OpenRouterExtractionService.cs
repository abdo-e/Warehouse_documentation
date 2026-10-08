using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WareDocs.Api.Models;

namespace WareDocs.Api.Services;

public class OpenRouterExtractionService : IAiExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OpenRouterExtractionService> _logger;

    public OpenRouterExtractionService(IConfiguration config, IHttpClientFactory httpClientFactory,
        ILogger<OpenRouterExtractionService> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("OpenRouter");

        var apiKey = config["OpenRouter:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
            ?? throw new InvalidOperationException("OpenRouter API key not configured. Set OpenRouter:ApiKey in appsettings.json or OPENROUTER_API_KEY env variable.");

        _model = config["OpenRouter:Model"] ?? "openrouter/auto";

        _httpClient.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://waredocs.api");
        _httpClient.DefaultRequestHeaders.Add("X-Title", "WareDocs API");
    }

    public async Task<DetectionResult> DetectTypeAsync(string text)
    {
        var systemPrompt = @"You are a warehouse document classifier. Given the text extracted from a document, determine if it is one of these types:
- delivery_note: A delivery note / bon de livraison (BL) listing delivered goods
- purchase_order: A purchase order (PO) listing ordered goods  
- invoice: An invoice listing billed items and amounts

Respond with ONLY valid JSON in this exact format:
{""documentType"": ""delivery_note|purchase_order|invoice|unknown"", ""confidence"": 0.0}

The confidence should be between 0.0 and 1.0.";

        var truncatedText = text.Length > 3000 ? text[..3000] : text;

        var responseText = await CallOpenRouterAsync(systemPrompt,
            $"Classify this warehouse document:\n\n{truncatedText}");

        try
        {
            var json = ExtractJson(responseText);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var typeStr = root.GetProperty("documentType").GetString() ?? "unknown";
            var confidence = root.TryGetProperty("confidence", out var confEl) ? confEl.GetDouble() : 0.5;

            var docType = typeStr.ToLowerInvariant() switch
            {
                "delivery_note" => DocumentType.DeliveryNote,
                "purchase_order" => DocumentType.PurchaseOrder,
                "invoice" => DocumentType.Invoice,
                _ => DocumentType.Unknown
            };

            return new DetectionResult { DocumentType = docType, Confidence = confidence };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse detection response: {Response}", responseText);
            return new DetectionResult { DocumentType = DocumentType.Unknown, Confidence = 0 };
        }
    }

    public async Task<ExtractionResult> ExtractAsync(string text, DocumentType? typeHint = null)
    {
        var systemPrompt = @"You are a warehouse document data extraction expert. Extract structured information from the document text.

Return ONLY valid JSON in this exact format:
{
  ""documentType"": ""delivery_note|purchase_order|invoice"",
  ""supplier"": ""supplier name"",
  ""documentNumber"": ""document reference number"",
  ""date"": ""YYYY-MM-DD"",
  ""totalAmount"": null,
  ""items"": [
    {
      ""sku"": ""item SKU or reference code"",
      ""description"": ""item description"",
      ""quantity"": 0,
      ""unitPrice"": null
    }
  ]
}

Rules:
- For delivery notes: extract supplier, deliveryNumber as documentNumber, date, and items with SKU/description/quantity
- For purchase orders: extract supplier, PO number as documentNumber, date, and ordered items
- For invoices: extract supplier, invoice number as documentNumber, totalAmount, date, and line items with unit prices
- If a field cannot be determined, use empty string for strings, null for numbers, empty array for items
- For dates, convert to YYYY-MM-DD format
- totalAmount should only be set for invoices";

        var hintText = typeHint.HasValue ? $"\nThe document has been identified as: {typeHint.Value}" : "";
        var truncatedText = text.Length > 4000 ? text[..4000] : text;

        var responseText = await CallOpenRouterAsync(systemPrompt,
            $"Extract data from this warehouse document:{hintText}\n\n{truncatedText}");

        try
        {
            var json = ExtractJson(responseText);
            var result = JsonSerializer.Deserialize<ExtractionResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? new ExtractionResult { DocumentType = DocumentType.Unknown };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse extraction response: {Response}", responseText);
            return new ExtractionResult { DocumentType = DocumentType.Unknown };
        }
    }

    private async Task<string> CallOpenRouterAsync(string systemPrompt, string userMessage)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.1,
            max_tokens = 2000
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        _logger.LogInformation("Calling OpenRouter model: {Model}", _model);

        var response = await _httpClient.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenRouter API error {Status}: {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"OpenRouter API error: {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var messageContent = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return messageContent ?? string.Empty;
    }

    /// <summary>
    /// Extract JSON from a response that might contain markdown code fences or extra text.
    /// </summary>
    private static string ExtractJson(string text)
    {
        text = text.Trim();

        // Remove markdown code fences
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline > 0)
                text = text[(firstNewline + 1)..];

            var lastFence = text.LastIndexOf("```");
            if (lastFence > 0)
                text = text[..lastFence];
        }

        // Find the first { and last } for JSON object
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        if (start >= 0 && end > start)
            return text[start..(end + 1)];

        return text.Trim();
    }
}
