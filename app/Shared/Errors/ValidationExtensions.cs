using FluentValidation;

namespace PjBudget.Shared.Errors;

public static class ValidationExtensions
{
	/// <summary>
	/// Runs a validator and reports failures as a <see cref="DomainValidationException"/>, with property paths
	/// camelCased to match the JSON the UI sent (e.g. "schedules[2].rows[0].rate").
	/// </summary>
	public static async Task ValidateOrThrowAsync<T>(this IValidator<T> validator, T model, CancellationToken ct)
	{
		var result = await validator.ValidateAsync(model, ct);
		if (result.IsValid)
		{
			return;
		}

		var errors = result.Errors
			.GroupBy(e => ToCamelCasePath(e.PropertyName))
			.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

		throw new DomainValidationException(errors);
	}

	private static string ToCamelCasePath(string path)
		=> string.Join('.', path.Split('.').Select(segment =>
			segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));
}
