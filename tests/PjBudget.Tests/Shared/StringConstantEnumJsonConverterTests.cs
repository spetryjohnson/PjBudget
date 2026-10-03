using System.Text.Json;
using PjBudget.Framework;
using PjBudget.Shared.Json;

namespace PjBudget.Tests.Shared;

public class StringConstantEnumJsonConverterTests
{
	public enum Frequency
	{
		[StringConstant("SEMIMONTHLY")] Semimonthly,
		[StringConstant("BIWEEKLY")] Biweekly,
	}

	[Flags]
	public enum Kinds
	{
		[StringConstant("NONE")] None = 0,
		[StringConstant("FED")] Federal = 1,
		[StringConstant("STATE")] State = 2,
	}

	public record Payload(Frequency Frequency, Frequency? Optional, Kinds Kinds);

	private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		Converters = { new StringConstantEnumJsonConverterFactory() },
	};

	[Test]
	public void WritesConstantsAndFlagArrays()
	{
		var json = JsonSerializer.Serialize(new Payload(Frequency.Biweekly, null, Kinds.Federal | Kinds.State), Options);

		Assert.That(json, Is.EqualTo("""{"frequency":"BIWEEKLY","optional":null,"kinds":["FED","STATE"]}"""));
	}

	[Test]
	public void WritesNoFlagsAsAnEmptyArray()
	{
		var json = JsonSerializer.Serialize(new Payload(Frequency.Semimonthly, Frequency.Biweekly, Kinds.None), Options);

		Assert.That(json, Does.Contain("\"optional\":\"BIWEEKLY\"").And.Contain("\"kinds\":[]"));
	}

	[Test]
	public void ReadsConstantsAndFlagArrays()
	{
		var payload = JsonSerializer.Deserialize<Payload>(
			"""{"frequency":"SEMIMONTHLY","optional":"BIWEEKLY","kinds":["STATE","FED"]}""", Options);

		Assert.That(payload, Is.EqualTo(new Payload(Frequency.Semimonthly, Frequency.Biweekly, Kinds.Federal | Kinds.State)));
	}

	[TestCase("""{"frequency":"WEEKLY","optional":null,"kinds":[]}""")]
	[TestCase("""{"frequency":1,"optional":null,"kinds":[]}""")]
	[TestCase("""{"frequency":"BIWEEKLY","optional":null,"kinds":["CITY"]}""")]
	[TestCase("""{"frequency":"BIWEEKLY","optional":null,"kinds":"FED"}""")]
	public void RejectsUnknownOrMistypedValues(string json)
	{
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Payload>(json, Options));
	}
}
