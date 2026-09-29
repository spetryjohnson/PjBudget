using System.Collections.Specialized;
using System.Globalization;

namespace PjBudget.Framework {

	/// <summary>
	/// Extension methods for parsing strings to various types, or converting between types.
	/// </summary>
	public static class ParsingAndConversionExtensions {

		/// <summary>
		/// Converts a string to an enum value. The string is first matched against any
		/// StringConstant attributes applied to the enum's values, and then falls back to
		/// matching the enum's names (or numeric values) via Enum.TryParse.
		/// </summary>
		/// <exception cref="ArgumentException">Thrown if the string does not match any enum value.</exception>
		public static T ToEnum<T>(this string s) where T : struct, Enum {
			if (TryToEnum<T>(s, out var result)) {
				return result;
			}

			throw new ArgumentException($"'{s}' is not a valid value for enum {typeof(T).Name}.", nameof(s));
		}

		/// <summary>
		/// Converts a string to an enum value. The string is first matched against any
		/// StringConstant attributes applied to the enum's values, and then falls back to
		/// matching the enum's names (or numeric values) via Enum.TryParse. Returns
		/// <paramref name="defaultVal"/> if no match is found.
		/// </summary>
		public static T ToEnum<T>(this string? s, T defaultVal) where T : struct, Enum {
			return TryToEnum<T>(s, out var result)
				? result
				: defaultVal;
		}

		private static bool TryToEnum<T>(string? s, out T result) where T : struct, Enum {
			result = default;

			if (s == null) {
				return false;
			}

			foreach (var value in Enum.GetValues<T>()) {
				var attributes = value.GetAttributes<StringConstantAttribute>();

				if (attributes.Length > 0 && attributes[0].GetStringConstant() == s) {
					result = value;
					return true;
				}
			}

			// Enum.TryParse accepts any numeric string, so make sure the result is actually defined
			if (Enum.TryParse(s, out result) && Enum.IsDefined(result)) {
				return true;
			}

			result = default;
			return false;
		}
	}
}