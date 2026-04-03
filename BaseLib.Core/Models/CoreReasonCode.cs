
using BaseLib.Core.Services;

namespace BaseLib.Core.Models
{
    /// <summary>
    /// Immutable value object that carries a numeric reason code and its human-readable description.
    /// Implicitly converts from any <see cref="Enum"/> so domain enums can be assigned directly to
    /// response <c>ReasonCode</c> properties. Implements <see cref="IConvertible"/> so it can be
    /// used wherever an <see cref="int"/> is expected (e.g. database parameters).
    /// </summary>
    public record CoreReasonCode(int Value, string Description) : IConvertible
    {
        /// <summary>A <see cref="CoreReasonCode"/> that maps to <see cref="CoreServiceReasonCode.Undefined"/> (value 0).</summary>
        public static CoreReasonCode Null { get { return CoreServiceReasonCode.Undefined; } }
        /// <summary>A <see cref="CoreReasonCode"/> that maps to <see cref="CoreServiceReasonCode.Succeeded"/> (value 1).</summary>
        public static CoreReasonCode Succeeded { get { return CoreServiceReasonCode.Succeeded; } }
        /// <summary>A <see cref="CoreReasonCode"/> that maps to <see cref="CoreServiceReasonCode.Failed"/> (value 2).</summary>
        public static CoreReasonCode Failed { get { return CoreServiceReasonCode.Failed; } }

        /// <summary>Initializes a <see cref="CoreReasonCode"/> with the <see cref="CoreServiceReasonCode.Undefined"/> value.</summary>
        public CoreReasonCode(): this(CoreServiceReasonCode.Undefined)
        {
        }

        /// <summary>Implicitly converts any <see cref="Enum"/> value to a <see cref="CoreReasonCode"/>.</summary>
        /// <param name="enumValue">The enum value to convert.</param>
        public static implicit operator CoreReasonCode(Enum enumValue)
            => new CoreReasonCode(Convert.ToInt32(enumValue), enumValue.GetDescription());

        /// <summary>Returns <see langword="true"/> when the reason code's numeric value equals the given enum.</summary>
        public static bool operator ==(CoreReasonCode reasonCode, Enum enumValue)
            => reasonCode.Value == Convert.ToInt32(enumValue);

        /// <summary>Returns <see langword="true"/> when the reason code's numeric value does not equal the given enum.</summary>
        public static bool operator !=(CoreReasonCode reasonCode, Enum enumValue)
            => !(reasonCode == enumValue);

        /// <summary>Returns <see langword="true"/> when the reason code's numeric value equals the given integer.</summary>
        public static bool operator ==(CoreReasonCode reasonCode, Int32 intValue)
            => reasonCode.Value == intValue;

        /// <summary>Returns <see langword="true"/> when the reason code's numeric value does not equal the given integer.</summary>
        public static bool operator !=(CoreReasonCode reasonCode, Int32 intValue)
            => !(reasonCode == intValue);

        /// <summary>Explicitly converts a <see cref="CoreReasonCode"/> to its underlying <see cref="int"/> value.</summary>
        /// <param name="reasonCode">The reason code to convert.</param>
        public static explicit operator int(CoreReasonCode reasonCode)
            => reasonCode.Value;

        /// <summary>Converts <see cref="Value"/> to the specified enum type <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">Target enum type.</typeparam>
        /// <returns>The enum member whose underlying value matches <see cref="Value"/>.</returns>
        public T ToEnum<T>() where T : struct, Enum
        {
            return (T)Enum.ToObject(typeof(T), Value);
        }

        /// <inheritdoc/>
        public TypeCode GetTypeCode()
        {
            return TypeCode.Int32;
        }

        /// <inheritdoc/>
        public int ToInt32(IFormatProvider? provider) => Value;
        /// <inheritdoc/>
        public bool ToBoolean(IFormatProvider? provider) => Convert.ToBoolean(Value, provider);
        /// <inheritdoc/>
        public byte ToByte(IFormatProvider? provider) => Convert.ToByte(Value, provider);
        /// <inheritdoc/>
        public char ToChar(IFormatProvider? provider) => Convert.ToChar(Value, provider);
        /// <inheritdoc/>
        public DateTime ToDateTime(IFormatProvider? provider) => Convert.ToDateTime(Value, provider);
        /// <inheritdoc/>
        public decimal ToDecimal(IFormatProvider? provider) => Convert.ToDecimal(Value, provider);
        /// <inheritdoc/>
        public double ToDouble(IFormatProvider? provider) => Convert.ToDouble(Value, provider);
        /// <inheritdoc/>
        public short ToInt16(IFormatProvider? provider) => Convert.ToInt16(Value, provider);
        /// <inheritdoc/>
        public long ToInt64(IFormatProvider? provider) => Convert.ToInt64(Value, provider);
        /// <inheritdoc/>
        public sbyte ToSByte(IFormatProvider? provider) => Convert.ToSByte(Value, provider);
        /// <inheritdoc/>
        public float ToSingle(IFormatProvider? provider) => Convert.ToSingle(Value, provider);
        /// <inheritdoc/>
        public string ToString(IFormatProvider? provider) => Convert.ToString(Value, provider);
        /// <inheritdoc/>
        public object ToType(Type conversionType, IFormatProvider? provider) => Convert.ChangeType(Value, conversionType, provider);
        /// <inheritdoc/>
        public ushort ToUInt16(IFormatProvider? provider) => Convert.ToUInt16(Value, provider);
        /// <inheritdoc/>
        public uint ToUInt32(IFormatProvider? provider) => Convert.ToUInt32(Value, provider);
        /// <inheritdoc/>
        public ulong ToUInt64(IFormatProvider? provider) => Convert.ToUInt64(Value, provider);
    }
}
