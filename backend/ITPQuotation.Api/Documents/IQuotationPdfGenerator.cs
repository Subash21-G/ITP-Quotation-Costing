using ITPQuotation.Api.DTOs.Quotations;

namespace ITPQuotation.Api.Documents;

public interface IQuotationPdfGenerator
{
    byte[] Generate(QuotationSnapshot quotation);
    byte[] GenerateDetailedCosting(QuotationSnapshot quotation);
}
