using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace PjBudget.Shared.Errors;

/// <summary>
/// Turns domain exceptions thrown by services into HTTP responses shaped like FastEndpoints' own validation errors
/// (statusCode / message / errors), so the UI handles both kinds of failure the same way.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
	{
		var response = exception switch
		{
			DomainValidationException e => new ErrorBody(StatusCodes.Status400BadRequest, e.Message, e.Errors),
			NotFoundException e => new ErrorBody(StatusCodes.Status404NotFound, e.Message),
			ConflictException e => new ErrorBody(StatusCodes.Status409Conflict, e.Message),
			DbUpdateConcurrencyException => new ErrorBody(StatusCodes.Status409Conflict,
				"This record was changed since you loaded it. Reload and try again."),
			_ => null,
		};

		if (response is null)
		{
			return false;
		}

		httpContext.Response.StatusCode = response.StatusCode;
		await httpContext.Response.WriteAsJsonAsync(response, ct);
		return true;
	}

	private sealed record ErrorBody(int StatusCode, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);
}
