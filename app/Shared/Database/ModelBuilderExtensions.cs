using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PjBudget.Shared.Database;

public static class ModelBuilderExtensions
{
	/// <summary>
	/// Stores every enum property as its string constant. Call this after all entities are configured, so every
	/// property (including owned types) is visible.
	/// </summary>
	public static ModelBuilder UseStringConstantsForEnums(this ModelBuilder modelBuilder)
	{
		foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
		{
			var enumType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
			if (!enumType.IsEnum)
			{
				continue;
			}

			var converterType = typeof(StringConstantEnumConverter<>).MakeGenericType(enumType);
			property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
		}

		return modelBuilder;
	}

	/// <summary>
	/// Makes <see cref="IVersionedEntity.Version"/> an optimistic concurrency token on every versioned entity.
	/// </summary>
	public static ModelBuilder UseVersionConcurrencyTokens(this ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (typeof(IVersionedEntity).IsAssignableFrom(entityType.ClrType))
			{
				modelBuilder.Entity(entityType.ClrType)
					.Property(nameof(IVersionedEntity.Version))
					.IsConcurrencyToken();
			}
		}

		return modelBuilder;
	}
}
