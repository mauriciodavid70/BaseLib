
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
        public static CoreReasonCode Null { get { return CoreServiceReasonCode.Undefined; } }
        public static CoreReasonCode Succeeded { get { return CoreServiceReasonCode.Succeeded; } }
        public static CoreReasonCode Failed { get { return CoreServiceReasonCode.Failed; } }
        public CoreReasonCode(): this(CoreServiceReasonCode.Undefined)
        {
        }

        public static implicit operator CoreReasonCode(Enum enumValue)
            => new CoreReasonCode(Convert.ToInt32(enumValue), enumValue.GetDescription());

        public static bool operator ==(CoreReasonCode reasonCode, Enum enumValue)
            => reasonCode.Value == Convert.ToInt32(enumValue);

        public static bool operator !=(CoreReasonCode reasonCode, Enum enumValue)
            => !(reasonCode == enumValue);

        public static bool operator ==(CoreReasonCode reasonCode, Int32 intValue)
            => reasonCode.Value == intValue;

        public static bool operator !=(CoreReasonCode reasonCode, Int32 intValue)
            => !(reasonCode == intValue);


        public static explicit operator int(CoreReasonCode reasonCode)
            => reasonCode.Value;

        public T ToEnum<T>() where T : struct, Enum
        {
            return (T)Enum.ToObject(typeof(T), Value);
        }

        // Implement IConvertible
        public TypeCode GetTypeCode()
        {
            return TypeCode.Int32;
        }

        public int ToInt32(IFormatProvider? provider)
        {
            return Value;
        }

        public bool ToBoolean(IFormatProvider? provider)
        {
            return Convert.ToBoolean(Value, provider);
        }

        public byte ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(Value, provider);
        }

        public char ToChar(IFormatProvider? provider)
        {
            return Convert.ToChar(Value, provider);
        }

        public DateTime ToDateTime(IFormatProvider? provider)
        {
            return Convert.ToDateTime(Value, provider);
        }

        public decimal ToDecimal(IFormatProvider? provider)
        {
            return Convert.ToDecimal(Value, provider);
        }

        public double ToDouble(IFormatProvider? provider)
        {
            return Convert.ToDouble(Value, provider);
        }

        public short ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(Value, provider);
        }

        public long ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(Value, provider);
        }

        public sbyte ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(Value, provider);
        }

        public float ToSingle(IFormatProvider? provider)
        {
            return Convert.ToSingle(Value, provider);
        }

        public string ToString(IFormatProvider? provider)
        {
            return Convert.ToString(Value, provider);
        }

        public object ToType(Type conversionType, IFormatProvider? provider)
        {
            return Convert.ChangeType(Value, conversionType, provider);
        }

        public ushort ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(Value, provider);
        }

        public uint ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(Value, provider);
        }

        public ulong ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(Value, provider);
        }
    }
}
