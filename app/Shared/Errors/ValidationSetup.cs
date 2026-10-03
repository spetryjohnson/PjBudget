using System.Text.RegularExpressions;
using FluentValidation;

namespace PjBudget.Shared.Errors;

public static partial class ValidationSetup
{
	/// <summary>
	/// Makes default validation messages read naturally ("Annual salary must not be empty"). FastEndpoints camelCases
	/// property names for the error keys, and FluentValidation would otherwise split that into "annual Salary".
	/// </summary>
	public static void UseSentenceCaseDisplayNames()
	{
		ValidatorOptions.Global.DisplayNameResolver = (_, member, _) => member is null ? null : SentenceCase(member.Name);
	}

	private static string SentenceCase(string name)
	{
		var words = WordBoundary().Replace(name, " ");
		return words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..].ToLowerInvariant();
	}

	// Zero-width split points before each capitalized word, so "W4Credits" becomes "W4 Credits".
	[GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
	private static partial Regex WordBoundary();
}
