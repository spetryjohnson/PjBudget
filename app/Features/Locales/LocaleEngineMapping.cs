using PjBudget.Features.Payroll.Engine;

namespace PjBudget.Features.Locales;

public static class LocaleEngineMapping
{
	public static WorkLocation ToWorkLocation(this Locale locale) => new(locale.StateCode, locale.MunicipalTaxRate);

	public static HomeLocation ToHomeLocation(this Locale locale)
		=> new(locale.StateCode, locale.MunicipalTaxRate, locale.SchoolDistrictTaxRate, locale.SchoolDistrictTaxBase);
}
