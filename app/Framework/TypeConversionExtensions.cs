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

		/// <remarks>
		/// For [Flags] enums the string may list several constants separated by
		/// <see cref="EnumExtensions.FlagSeparator"/>, in any order.
		/// </remarks>
		private static bool TryToEnum<T>(string? s, out T result) where T : struct, Enum {
			result = default;

			if (s == null || !StringConstantMap.For(typeof(T)).TryParse(s, out var value)) {
				return false;
			}

			result = (T)value!;
			return true;
		}
	}
}