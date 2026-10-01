namespace ITPQuotation.Api.Services;

public sealed class CostSheetConflictException : Exception
{
    public CostSheetConflictException(string message) : base(message)
    {
    }

    public CostSheetConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class CostSheetValidationException(string message) : Exception(message);
