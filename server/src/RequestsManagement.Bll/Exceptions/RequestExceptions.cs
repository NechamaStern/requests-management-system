namespace RequestsManagement.Bll.Exceptions;

// Expected business failures. GlobalExceptionHandler maps each one to an HTTP status.

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);

public sealed class BusinessValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
