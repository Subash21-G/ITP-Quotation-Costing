using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ITPQuotation.Api.Services;

public sealed record PdfTextExtractionResult(string Text, int PageCount);

public interface IRfqPdfTextExtractor
{
    PdfTextExtractionResult Extract(Stream pdfStream);
}

public sealed class PdfPigRfqTextExtractor : IRfqPdfTextExtractor
{
    private const int MaximumPages = 50;
    private const int MaximumExtractedCharacters = 100_000;

    public PdfTextExtractionResult Extract(Stream pdfStream)
    {
        try
        {
            using var document = PdfDocument.Open(pdfStream);
            if (document.NumberOfPages is < 1 or > MaximumPages)
            {
                throw new RfqImportValidationException(
                    $"PDF must contain between 1 and {MaximumPages} pages.");
            }

            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                if (text.Length > 0) text.AppendLine();
                text.Append(ContentOrderTextExtractor.GetText(page));
                if (text.Length > MaximumExtractedCharacters)
                {
                    throw new RfqImportValidationException(
                        "Extracted PDF text is too large to review safely.");
                }
            }

            var result = text.ToString().Trim();
            if (string.IsNullOrWhiteSpace(result))
            {
                throw new RfqPdfExtractionException(
                    "No readable text was found. Scanned PDFs require OCR before import.");
            }

            return new PdfTextExtractionResult(result, document.NumberOfPages);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RfqImportValidationException)
        {
            throw;
        }
        catch (RfqPdfExtractionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new RfqPdfExtractionException(
                "The PDF is encrypted, damaged, or uses an unsupported encoding.",
                exception);
        }
    }
}
