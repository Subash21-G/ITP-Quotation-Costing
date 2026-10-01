namespace ITPQuotation.Api.Services;

public sealed class RfqImportValidationException(string message) : Exception(message);

public sealed class RfqImportConflictException(string message) : Exception(message);

public sealed class RfqPdfExtractionException(string message, Exception? innerException = null)
    : Exception(message, innerException);
