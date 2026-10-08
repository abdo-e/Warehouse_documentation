using WareDocs.Api.Models;

namespace WareDocs.Api.Services;

public class DocumentMatchingService : IDocumentMatchingService
{
    private readonly ISkuNormalizerService _skuNormalizer;

    public DocumentMatchingService(ISkuNormalizerService skuNormalizer)
    {
        _skuNormalizer = skuNormalizer;
    }

    public MatchResult Match(ExtractionResult? po, ExtractionResult? dn, ExtractionResult? inv)
    {
        var result = new MatchResult();

        // Build normalized SKU maps for each document
        var poItems = BuildSkuMap(po?.Items);
        var dnItems = BuildSkuMap(dn?.Items);
        var invItems = BuildSkuMap(inv?.Items);

        // Collect all unique SKUs across all documents
        var allSkus = new HashSet<string>(
            poItems.Keys.Concat(dnItems.Keys).Concat(invItems.Keys),
            StringComparer.OrdinalIgnoreCase);

        foreach (var sku in allSkus)
        {
            var poQty = GetQuantity(poItems, sku);
            var dnQty = GetQuantity(dnItems, sku);
            var invQty = GetQuantity(invItems, sku);
            var desc = GetDescription(poItems, sku)
                    ?? GetDescription(dnItems, sku)
                    ?? GetDescription(invItems, sku)
                    ?? string.Empty;

            // Check PO vs DN discrepancy (missing or over shipments)
            if (po != null && dn != null)
            {
                if (dnQty < poQty)
                {
                    result.MissingItems.Add(new DiscrepancyItem
                    {
                        Sku = sku,
                        Description = desc,
                        Ordered = poQty,
                        Received = dnQty,
                        Invoiced = invQty
                    });
                }
                else if (dnQty > poQty)
                {
                    result.OverShipments.Add(new DiscrepancyItem
                    {
                        Sku = sku,
                        Description = desc,
                        Ordered = poQty,
                        Received = dnQty,
                        Invoiced = invQty
                    });
                }
            }

            // Check PO vs Invoice price mismatches
            if (po != null && inv != null)
            {
                var poPrice = GetUnitPrice(poItems, sku);
                var invPrice = GetUnitPrice(invItems, sku);

                if (poPrice.HasValue && invPrice.HasValue && poPrice.Value != invPrice.Value)
                {
                    result.PriceMismatches.Add(new PriceMismatch
                    {
                        Sku = sku,
                        PoPrice = poPrice,
                        InvoicePrice = invPrice
                    });
                }
            }

            // Check DN vs Invoice quantity discrepancy (billed but not received)
            if (dn != null && inv != null && po == null)
            {
                if (invQty > dnQty)
                {
                    result.MissingItems.Add(new DiscrepancyItem
                    {
                        Sku = sku,
                        Description = desc,
                        Ordered = 0,
                        Received = dnQty,
                        Invoiced = invQty
                    });
                }
            }
        }

        result.Status = (result.MissingItems.Count > 0 || result.OverShipments.Count > 0 || result.PriceMismatches.Count > 0)
            ? "warning"
            : "ok";

        return result;
    }

    private Dictionary<string, LineItem> BuildSkuMap(List<LineItem>? items)
    {
        if (items == null || items.Count == 0)
            return new Dictionary<string, LineItem>(StringComparer.OrdinalIgnoreCase);

        var map = new Dictionary<string, LineItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var normalizedSku = _skuNormalizer.Normalize(item.Sku);
            if (!map.ContainsKey(normalizedSku))
            {
                map[normalizedSku] = item;
            }
            else
            {
                // Merge quantities for duplicate SKUs
                map[normalizedSku] = new LineItem
                {
                    Sku = normalizedSku,
                    Description = map[normalizedSku].Description,
                    Quantity = map[normalizedSku].Quantity + item.Quantity,
                    UnitPrice = item.UnitPrice ?? map[normalizedSku].UnitPrice
                };
            }
        }
        return map;
    }

    private decimal GetQuantity(Dictionary<string, LineItem> map, string sku)
    {
        var normalizedSku = _skuNormalizer.Normalize(sku);
        return map.TryGetValue(normalizedSku, out var item) ? item.Quantity : 0;
    }

    private string? GetDescription(Dictionary<string, LineItem> map, string sku)
    {
        var normalizedSku = _skuNormalizer.Normalize(sku);
        return map.TryGetValue(normalizedSku, out var item) ? item.Description : null;
    }

    private decimal? GetUnitPrice(Dictionary<string, LineItem> map, string sku)
    {
        var normalizedSku = _skuNormalizer.Normalize(sku);
        return map.TryGetValue(normalizedSku, out var item) ? item.UnitPrice : null;
    }
}
