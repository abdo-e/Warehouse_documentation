using WareDocs.Api.Models;

namespace WareDocs.Api.Services;

public interface IDocumentMatchingService
{
    /// <summary>
    /// Match documents (PO, delivery note, invoice) and detect discrepancies.
    /// At least two different document types are required.
    /// </summary>
    MatchResult Match(ExtractionResult? purchaseOrder, ExtractionResult? deliveryNote, ExtractionResult? invoice);
}
