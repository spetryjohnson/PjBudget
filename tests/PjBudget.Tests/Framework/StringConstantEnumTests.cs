using PjBudget.Framework;

namespace PjBudget.Tests.Framework;

public class StringConstantEnumTests
{
	public enum Color
	{
		[StringConstant("RED")] Red,
		[StringConstant("GREEN")] Green,
		Blue,
	}

	[Flags]
	public enum Access
	{
		[StringConstant("NONE")] None = 0,
		[StringConstant("R")] Read = 1,
		[StringConstant("W")] Write = 2,
		[StringConstant("X")] Execute = 4,
		[StringConstant("RW")] ReadWrite = Read | Write,
	}

	[TestCase(Color.Red, "RED")]
	[TestCase(Color.Green, "GREEN")]
	[TestCase(Color.Blue, "Blue")]
	public void ToStringConstant_UsesConstantOrFallsBackToName(Color value, string expected)
	{
		Assert.That(value.ToStringConstant(), Is.EqualTo(expected));
	}

	[TestCase("RED", Color.Red)]
	[TestCase("Green", Color.Green)]
	[TestCase("Blue", Color.Blue)]
	public void ToEnum_MatchesConstantsThenNames(string input, Color expected)
	{
		Assert.That(input.ToEnum<Color>(), Is.EqualTo(expected));
	}

	[TestCase("PURPLE")]
	[TestCase("7")]
	[TestCase("")]
	public void ToEnum_RejectsUnknownValues(string input)
	{
		Assert.Throws<ArgumentException>(() => input.ToEnum<Color>());
		Assert.That(input.ToEnum(Color.Green), Is.EqualTo(Color.Green));
	}

	[Test]
	public void Flags_CombinationIsWrittenAsIndividualConstantsInValueOrder()
	{
		Assert.That((Access.Execute | Access.Read).ToStringConstant(), Is.EqualTo("R|X"));
	}

	[Test]
	public void Flags_CompositeMemberIsDecomposedSoRenamingItCantStrandData()
	{
		Assert.That(Access.ReadWrite.ToStringConstant(), Is.EqualTo("R|W"));
	}

	[Test]
	public void Flags_ZeroUsesTheZeroMembersConstant()
	{
		Assert.That(Access.None.ToStringConstant(), Is.EqualTo("NONE"));
		Assert.That("NONE".ToEnum<Access>(), Is.EqualTo(Access.None));
	}

	[TestCase("R|X", Access.Read | Access.Execute)]
	[TestCase("X | R", Access.Read | Access.Execute)]
	[TestCase("RW", Access.ReadWrite)]
	[TestCase("Write|Execute", Access.Write | Access.Execute)]
	public void Flags_ParseInAnyOrder(string input, Access expected)
	{
		Assert.That(input.ToEnum<Access>(), Is.EqualTo(expected));
	}

	[TestCase("R|Q")]
	[TestCase("R|")]
	public void Flags_RejectUnknownTokens(string input)
	{
		Assert.Throws<ArgumentException>(() => input.ToEnum<Access>());
	}

	[Test]
	public void Flags_UndefinedBitsCannotBeWritten()
	{
		Assert.Throws<ArgumentException>(() => ((Access)8).ToStringConstant());
	}
}
