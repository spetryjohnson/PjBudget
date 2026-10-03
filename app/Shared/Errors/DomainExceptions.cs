namespace PjBudget.Shared.Errors;

/// <summary>
/// Input that violates a business rule. Keys in <see cref="Errors"/> are camelCase property paths, so the UI can
/// attach each message to its field; an empty key means the message applies to the request as a whole.
/// </summary>
public sealed class DomainValidationException : Exception
{
	public IReadOnlyDictionary<string, string[]> Errors { get; }

	public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
		: base("One or more validation errors occurred.")
	{
		Errors = errors;
	}

	public DomainValidationException(string property, string message)
		: this(new Dictionary<string, string[]> { [property] = [message] })
	{
	}
}

public sealed class NotFoundException : Exception
{
	public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// The request can't be applied to the current state, e.g. a stale version or deleting something still in use.
/// </summary>
public sealed class ConflictException : Exception
{
	public ConflictException(string message) : base(message) { }
}
