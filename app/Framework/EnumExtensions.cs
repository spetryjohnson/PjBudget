using System;

namespace PjBudget.Framework {

	public static class EnumExtensions {

		/// <summary>
		/// Separates the individual flags when a [Flags] enum value is written as string constants (e.g. "FED|STATE").
		/// </summary>
		public const char FlagSeparator = '|';

		/// <summary>
		/// Returns an array of attributes of the specified type applied to the given enum value.
		/// </summary>
		public static T[] GetAttributes<T>(this Enum @enum) where T : Attribute {
			ArgumentNullException.ThrowIfNull(@enum);
			
			var enumString = @enum.ToString();

			var field = @enum
				.GetType()
				.GetField(enumString);

			if (field == null) {
				return [];
			}
			
			return (T[])field.GetCustomAttributes(typeof(T), false);
		}

		/// <summary>
		/// Returns the value of the Description attribute that decorates this enum, or the
		/// enum's .ToString() result if no such attribute is found.
		/// </summary>
		public static string ToDescription(this Enum @enum) {
			var description = @enum
				.GetAttributes<DescriptionAttribute>()
				.FirstOrDefault();

			return description != null
				? description.Description
				: @enum.ToString();
		}

		/// <summary>
		/// Returns the string constant associated with the specified enum value, or the value's
		/// .ToString() result if no string constant attribute is applied. A combination of [Flags]
		/// values is returned as the constants of its individual flags, joined by <see cref="FlagSeparator"/>.
		/// </summary>
		public static string ToStringConstant(this Enum @enum) {
			ArgumentNullException.ThrowIfNull(@enum);

			return StringConstantMap.For(@enum.GetType()).ToConstant(@enum);
		}
	}
}