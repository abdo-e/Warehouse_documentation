namespace WareDocs.Api.Services;

public interface ISkuNormalizerService
{
    /// <summary>
    /// Normalize a SKU string (strip dashes, spaces, case-normalize).
    /// </summary>
    string Normalize(string sku);

    /// <summary>
    /// Compare two SKUs and return whether they likely refer to the same item.
    /// </summary>
    (bool Same, double Confidence) Compare(string sku1, string sku2);
}
