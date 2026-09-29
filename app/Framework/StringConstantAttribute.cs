namespace PjBudget.Framework {

	/// <summary>
	/// Maps an enum to a string constant.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class StringConstantAttribute : Attribute, IStringConstant {
		private readonly string _value;

		public StringConstantAttribute(string constant) {
			this._value = constant;
		}

		public string GetStringConstant() {
			return this._value;
		}
	}

}