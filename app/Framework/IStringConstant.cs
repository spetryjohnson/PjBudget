namespace PjBudget.Framework {

	/// <summary>
	/// Declares that a class instance maps to a specific string constant, and defines
	/// a method for getting that constant. This is commonly used by attribute classes
	/// that are used with Enums to associate an enum instance with a string representation
	/// that is stored in a database.
	/// </summary>
	public interface IStringConstant {

		string GetStringConstant();

	}

}