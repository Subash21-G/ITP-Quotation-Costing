namespace ITPQuotation.Api.Services;

public sealed class QuotationConflictException : Exception
{
    public QuotationConflictException(string message) : base(message)
    {
    }

    public QuotationConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class QuotationValidationException(string message) : Exception(message);
