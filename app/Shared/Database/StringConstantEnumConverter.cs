using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PjBudget.Framework;

namespace PjBudget.Shared.Database;

/// <summary>
/// Stores an enum as its string constant (see <see cref="StringConstantAttribute"/>), so the database stays
/// self-describing and enum members can be renamed or reordered without migrating data.
/// </summary>
public sealed class StringConstantEnumConverter<TEnum> : ValueConverter<TEnum, string>
	where TEnum : struct, Enum
{
	public StringConstantEnumConverter()
		: base(value => value.ToStringConstant(), constant => constant.ToEnum<TEnum>())
	{
	}
}
