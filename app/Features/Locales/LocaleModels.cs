using FluentValidation;
using PjBudget.Shared.Domain;

namespace PjBudget.Features.Locales;

public sealed class LocaleModel
{
	public int Id { get; set; }
	public Guid Version { get; set; }
	public string Name { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
	public string StateCode { get; set; } = string.Empty;
	public string? ZipCode { get; set; }
	public decimal MunicipalTaxRate { get; set; }
	public string? SchoolDistrictName { get; set; }
	public string? SchoolDistrictNumber { get; set; }
	public decimal? SchoolDistrictTaxRate { get; set; }
	public SchoolDistrictTaxBase? SchoolDistrictTaxBase { get; set; }
}

public sealed class LocaleModelValidator : AbstractValidator<LocaleModel>
{
	public LocaleModelValidator()
	{
		RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
		RuleFor(x => x.City).NotEmpty().MaximumLength(100);
		RuleFor(x => x.StateCode).NotEmpty().Matches("^[A-Z]{2}$").WithMessage("Use the two-letter state code, e.g. OH.");
		RuleFor(x => x.ZipCode).Matches(@"^\d{5}(-\d{4})?$").When(x => !string.IsNullOrEmpty(x.ZipCode))
			.WithMessage("Use a 5-digit ZIP code (or ZIP+4).");
		RuleFor(x => x.MunicipalTaxRate).InclusiveBetween(0m, 0.2m)
			.WithMessage("Enter the city rate as a fraction, e.g. 0.02 for 2%.");
		RuleFor(x => x.SchoolDistrictName).MaximumLength(100);
		RuleFor(x => x.SchoolDistrictNumber).MaximumLength(10);
		RuleFor(x => x.SchoolDistrictTaxRate).InclusiveBetween(0m, 0.2m)
			.WithMessage("Enter the school district rate as a fraction, e.g. 0.0075 for 0.75%.");
		RuleFor(x => x.SchoolDistrictTaxBase).NotNull().When(x => x.SchoolDistrictTaxRate > 0)
			.WithMessage("Choose whether the district taxes earned income or uses the traditional base.");
	}
}
