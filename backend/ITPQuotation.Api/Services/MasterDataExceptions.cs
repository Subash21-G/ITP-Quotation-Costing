namespace ITPQuotation.Api.Services;

public sealed class MasterDataConflictException : Exception
{
    public MasterDataConflictException(string message) : base(message)
    {
    }

    public MasterDataConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class MasterDataValidationException(string message) : Exception(message);
