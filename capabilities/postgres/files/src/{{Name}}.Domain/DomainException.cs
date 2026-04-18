namespace {{Name}}.Domain;

/// <summary>
/// Thrown when an aggregate rejects input that violates a domain
/// invariant. Distinct from <see cref="ArgumentException"/> so the
/// API layer can map it to a 422 ProblemDetails without catching
/// framework-level validation failures.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
    public DomainException() { }
}
