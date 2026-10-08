using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using WareDocs.Api.Models;
using WareDocs.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Configure JSON serialization ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// --- Register services ---
builder.Services.AddSingleton<IOcrService, TesseractOcrService>();
builder.Services.AddSingleton<ISkuNormalizerService, SkuNormalizerService>();
builder.Services.AddSingleton<IDocumentMatchingService, DocumentMatchingService>();
builder.Services.AddSingleton<IBarcodeService, BarcodeService>();
builder.Services.AddSingleton<IAiExtractionService, OpenRouterExtractionService>();
builder.Services.AddHttpClient("OpenRouter");

// --- AWS Lambda Hosting ---
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// --- Swagger ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "WareDocs API",
        Version = "v1",
        Description = "Warehouse document processing API — Extract, detect, and match POs, delivery notes, and invoices. Get ERP-ready JSON with discrepancy detection."
    });
});

// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

// --- Middleware ---
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WareDocs API v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

// --- Global error handler ---
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ArgumentException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (Microsoft.AspNetCore.Http.BadHttpRequestException)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = "Invalid request format or missing file. Please ensure you uploaded a file." });
    }
    catch (HttpRequestException ex)
    {
        context.Response.StatusCode = 502;
        await context.Response.WriteAsJsonAsync(new { error = "AI service error", details = ex.Message });
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
    }
});

// =====================================================================
// ENDPOINTS
// =====================================================================

// --- POST /extract ---
app.MapPost("/extract", async (IFormFile file, IOcrService ocr, IAiExtractionService ai) =>
{

    await using var stream = file.OpenReadStream();
    var text = await ocr.ExtractTextAsync(stream, file.FileName);

    if (string.IsNullOrWhiteSpace(text))
        return Results.BadRequest(new { error = "Could not extract text from the document." });

    var result = await ai.ExtractAsync(text);
    return Results.Ok(result);
})
.WithName("ExtractDocument")
.WithTags("Extraction")
.WithDescription("Upload a warehouse document (PDF/image) and get structured JSON with supplier, items, quantities, etc.")
.DisableAntiforgery()
.Produces<ExtractionResult>(200)
.Produces(400);

// --- POST /detect ---
app.MapPost("/detect", async (IFormFile file, IOcrService ocr, IAiExtractionService ai) =>
{

    await using var stream = file.OpenReadStream();
    var text = await ocr.ExtractTextAsync(stream, file.FileName);

    if (string.IsNullOrWhiteSpace(text))
        return Results.BadRequest(new { error = "Could not extract text from the document." });

    var result = await ai.DetectTypeAsync(text);
    return Results.Ok(result);
})
.WithName("DetectDocumentType")
.WithTags("Detection")
.WithDescription("Upload a warehouse document and detect its type (delivery note, purchase order, or invoice) with confidence score.")
.DisableAntiforgery()
.Produces<DetectionResult>(200)
.Produces(400);

// --- POST /match ---
app.MapPost("/match", async (IFormFile? purchaseOrder, IFormFile? deliveryNote, IFormFile? invoice, IOcrService ocr, IAiExtractionService ai, IDocumentMatchingService matcher) =>
{
    var poFile = purchaseOrder;
    var dnFile = deliveryNote;
    var invFile = invoice;

    if (poFile == null && dnFile == null && invFile == null)
        throw new ArgumentException("Upload at least 2 documents as: purchaseOrder, deliveryNote, invoice (multipart/form-data field names).");

    var uploadedCount = (poFile != null ? 1 : 0) + (dnFile != null ? 1 : 0) + (invFile != null ? 1 : 0);
    if (uploadedCount < 2)
        throw new ArgumentException("At least 2 documents are required for matching.");

    // Extract all documents in parallel
    async Task<ExtractionResult?> ExtractFile(IFormFile? file, DocumentType hint)
    {
        if (file == null) return null;
        await using var stream = file.OpenReadStream();
        var text = await ocr.ExtractTextAsync(stream, file.FileName);
        return string.IsNullOrWhiteSpace(text) ? null : await ai.ExtractAsync(text, hint);
    }

    var poTask = ExtractFile(poFile, DocumentType.PurchaseOrder);
    var dnTask = ExtractFile(dnFile, DocumentType.DeliveryNote);
    var invTask = ExtractFile(invFile, DocumentType.Invoice);

    await Task.WhenAll(poTask, dnTask, invTask);

    var result = matcher.Match(poTask.Result, dnTask.Result, invTask.Result);
    return Results.Ok(result);
})
.WithName("MatchDocuments")
.WithTags("Matching")
.WithDescription("Upload 2-3 warehouse documents (PO, delivery note, invoice) and detect discrepancies: missing items, over-shipments, price mismatches.")
.DisableAntiforgery()
.Produces<MatchResult>(200)
.Produces(400);

// --- POST /barcode ---
app.MapPost("/barcode", async (IFormFile file, IBarcodeService barcodeService) =>
{

    await using var stream = file.OpenReadStream();
    var result = await barcodeService.DecodeAsync(stream, file.FileName);

    if (result == null)
        return Results.NotFound(new { error = "No barcode found in the uploaded image." });

    return Results.Ok(result);
})
.WithName("ReadBarcode")
.WithTags("Barcode")
.WithDescription("Upload a warehouse label image and decode the barcode/QR code.")
.DisableAntiforgery()
.Produces<BarcodeResult>(200)
.Produces(404);

// --- POST /sku-match ---
app.MapPost("/sku-match", (SkuMatchRequest request, ISkuNormalizerService skuService) =>
{
    if (string.IsNullOrWhiteSpace(request.Sku1) || string.IsNullOrWhiteSpace(request.Sku2))
        throw new ArgumentException("Both sku1 and sku2 are required.");

    var (same, confidence) = skuService.Compare(request.Sku1, request.Sku2);
    return Results.Ok(new SkuMatchResult { Same = same, Confidence = confidence });
})
.WithName("MatchSkus")
.WithTags("SKU")
.WithDescription("Compare two SKU strings to determine if they refer to the same item (handles dashes, spaces, case differences).")
.Produces<SkuMatchResult>(200)
.Produces(400);

// --- Health check ---
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "WareDocs API",
    version = "1.0.0",
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithTags("System");

app.Run();
