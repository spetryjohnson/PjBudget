using System.Text.Json;
using System.Text.Json.Serialization;
using PjBudget.Framework;

namespace PjBudget.Shared.Json;

/// <summary>
/// Serializes enums as their string constants, matching what the database stores. [Flags] enums become arrays of
/// individual flag constants (e.g. ["FED","STATE"]), which are easier for the UI to bind than a delimited string.
/// </summary>
public sealed class StringConstantEnumJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var converterType = typeToConvert.IsDefined(typeof(FlagsAttribute), inherit: false)
			? typeof(FlagsConverter<>)
			: typeof(ValueConverter<>);

		return (JsonConverter)Activator.CreateInstance(converterType.MakeGenericType(typeToConvert))!;
	}

	private sealed class ValueConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
	{
		public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			=> Parse<TEnum>(ReadString(ref reader));

		public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToStringConstant());
	}

	private sealed class FlagsConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
	{
		public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
			{
				throw new JsonException($"Expected an array of {typeof(TEnum).Name} constants.");
			}

			var constants = new List<string>();
			while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
			{
				constants.Add(ReadString(ref reader));
			}

			return constants.Count == 0
				? default
				: Parse<TEnum>(string.Join(EnumExtensions.FlagSeparator, constants));
		}

		public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
		{
			writer.WriteStartArray();
			if (!EqualityComparer<TEnum>.Default.Equals(value, default))
			{
				foreach (var constant in value.ToStringConstant().Split(EnumExtensions.FlagSeparator))
				{
					writer.WriteStringValue(constant);
				}
			}
			writer.WriteEndArray();
		}
	}

	private static string ReadString(ref Utf8JsonReader reader)
		=> reader.TokenType == JsonTokenType.String
			? reader.GetString()!
			: throw new JsonException($"Expected a string constant but found {reader.TokenType}.");

	private static TEnum Parse<TEnum>(string constant) where TEnum : struct, Enum
	{
		try
		{
			return constant.ToEnum<TEnum>();
		}
		catch (ArgumentException e)
		{
			throw new JsonException(e.Message, e);
		}
	}
}
