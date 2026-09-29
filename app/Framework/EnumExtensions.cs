using System;

namespace PjBudget.Framework {

	public static class EnumExtensions {

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
		/// .ToString() result if no string constant attribute is applied.
		/// </summary>
		public static string ToStringConstant(this Enum @enum) {
			var attributes = @enum.GetAttributes<StringConstantAttribute>();

			return attributes.Length > 0
				? attributes[0].GetStringConstant()
				: @enum.ToString();
		}
	}
}