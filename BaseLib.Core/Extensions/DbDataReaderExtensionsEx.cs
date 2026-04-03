namespace System.Data.Common
{
    /// <summary>
    /// Extension methods for DbDataReader to handle null values and type conversions.
    /// null values are converted to default values for the type.
    /// </summary> 
    public static class DbDataReaderExtensionsEx
    {
        /// <summary>Reads a <see cref="string"/> column by name, returning <see langword="null"/> for <c>DBNull</c>.</summary>
        public static string? GetStringEx(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToString(value) : null;
        }

        /// <summary>Reads an <see cref="long"/> column by name, returning <c>0</c> for <c>DBNull</c>.</summary>
        public static long GetInt64Ex(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToInt64(value) : 0;
        }

        /// <summary>Reads an <see cref="int"/> column by name, returning <c>0</c> for <c>DBNull</c>.</summary>
        public static int GetInt32Ex(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToInt32(value) : 0;
        }

        /// <summary>Reads a <see cref="short"/> column by name, returning <c>0</c> for <c>DBNull</c>.</summary>
        public static short GetInt16Ex(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToInt16(value) : (short)0;
        }

        /// <summary>Reads a <see cref="DateTime"/> column by name, returning <see cref="DateTime.MinValue"/> for <c>DBNull</c>.</summary>
        public static DateTime GetDateTimeEx(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToDateTime(value) : DateTime.MinValue;
        }

        /// <summary>Reads a <see cref="bool"/> column by name, returning <see langword="false"/> for <c>DBNull</c>.</summary>
        public static bool GetBooleanEx(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToBoolean(value) : false;
        }

        /// <summary>Reads a <see cref="double"/> column by name, returning <c>0</c> for <c>DBNull</c>.</summary>
        public static double GetDoubleEx(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToDouble(value) : 0;
        }

        /// <summary>Reads a <see cref="decimal"/> column by name, returning <c>0</c> for <c>DBNull</c>.</summary>
        public static decimal GetDecimalEx(this DbDataReader reader, string name)
        {
            var value = reader[name];
            return (value != null && value != DBNull.Value) ? Convert.ToDecimal(value) : 0;
        }

        /// <summary>Reads a string column and parses it as <typeparamref name="EnumT"/>, returning the default value for <c>DBNull</c> or an unrecognised string.</summary>
        public static EnumT GetEnumEx<EnumT>(this DbDataReader reader, string name) where EnumT : struct, Enum
        {
            var value = reader.GetStringEx(name);
            return Enum.TryParse(value, out EnumT result) ? result : default;
        }
    }
}
