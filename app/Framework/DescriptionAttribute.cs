namespace PjBudget.Framework {

	/// <summary>
	/// Used to decorate enum instances with a human-readable description. The .GetDescription()
	/// extension method can be used to convert an enum into its description string.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class DescriptionAttribute : Attribute {
		public string Description { get; set; }

		public DescriptionAttribute(string description) {
			this.Description = description;
		}
	}
}