using System.Collections.Concurrent;
using System.Reflection;

namespace PjBudget.Framework {

	/// <summary>
	/// Cached, two-way lookup between an enum type's values and their string constants. Conversions run for
	/// every enum column EF reads or writes, so the reflection is done once per type instead of once per call.
	/// </summary>
	internal sealed class StringConstantMap {
		private static readonly ConcurrentDictionary<Type, StringConstantMap> Cache = new();

		private readonly Type _enumType;
		private readonly Dictionary<Enum, string> _constantsByValue = new();
		private readonly Dictionary<string, Enum> _valuesByConstant = new(StringComparer.Ordinal);
		private readonly List<(ulong Bit, Enum Value)> _singleBitMembers = new();

		public bool IsFlags { get; }

		public static StringConstantMap For(Type enumType) {
			return Cache.GetOrAdd(enumType, t => new StringConstantMap(t));
		}

		private StringConstantMap(Type enumType) {
			this._enumType = enumType;
			this.IsFlags = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

			foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static)) {
				var value = (Enum)field.GetValue(null)!;
				var attribute = field.GetCustomAttribute<StringConstantAttribute>();
				var constant = attribute?.GetStringConstant() ?? field.Name;

				// Aliases (two names, one value) keep the first declared name.
				this._constantsByValue.TryAdd(value, constant);

				if (attribute != null && !this._valuesByConstant.TryAdd(constant, value)) {
					throw new InvalidOperationException(
						$"String constant '{constant}' is used by more than one member of {enumType.Name}.");
				}

				var bits = ToBits(value);
				if (this.IsFlags && bits != 0 && (bits & (bits - 1)) == 0) {
					this._singleBitMembers.Add((bits, value));
				}
			}

			this._singleBitMembers.Sort((a, b) => a.Bit.CompareTo(b.Bit));
		}

		public string ToConstant(Enum value) {
			var bits = ToBits(value);
			var isSingleBitOrZero = (bits & (bits - 1)) == 0;

			if ((!this.IsFlags || isSingleBitOrZero) && this._constantsByValue.TryGetValue(value, out var constant)) {
				return constant;
			}

			if (!this.IsFlags) {
				return value.ToString();
			}

			// Combinations are stored as their individual flags, even when a composite member exists for them,
			// so renaming or removing a composite member never strands data that is already stored.
			var parts = new List<string>();
			foreach (var (bit, member) in this._singleBitMembers) {
				if ((bits & bit) == bit) {
					parts.Add(this._constantsByValue[member]);
					bits &= ~bit;
				}
			}

			if (bits != 0) {
				throw new ArgumentException(
					$"{this._enumType.Name} value {value} includes bits that don't correspond to a defined flag.");
			}

			return parts.Count > 0
				? string.Join(EnumExtensions.FlagSeparator, parts)
				: value.ToString();
		}

		public bool TryParse(string s, out Enum? result) {
			if (!this.IsFlags) {
				return this.TryParseSingle(s, out result);
			}

			ulong bits = 0;
			foreach (var token in s.Split(EnumExtensions.FlagSeparator)) {
				if (!this.TryParseSingle(token.Trim(), out var part)) {
					result = null;
					return false;
				}

				bits |= ToBits(part!);
			}

			result = (Enum)Enum.ToObject(this._enumType, bits);
			return true;
		}

		private bool TryParseSingle(string s, out Enum? result) {
			if (this._valuesByConstant.TryGetValue(s, out result)) {
				return true;
			}

			// Member names (and numeric strings) still parse, so values written before a constant was assigned
			// keep loading. Enum.TryParse accepts any number, so the result must also be a defined member.
			if (Enum.TryParse(this._enumType, s, out var parsed) && Enum.IsDefined(this._enumType, parsed!)) {
				result = (Enum)parsed!;
				return true;
			}

			result = null;
			return false;
		}

		private static ulong ToBits(Enum value) {
			return Type.GetTypeCode(Enum.GetUnderlyingType(value.GetType())) == TypeCode.UInt64
				? Convert.ToUInt64(value)
				: unchecked((ulong)Convert.ToInt64(value));
		}
	}
}
