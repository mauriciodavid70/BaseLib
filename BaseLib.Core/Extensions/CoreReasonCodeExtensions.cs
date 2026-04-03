using System;
using BaseLib.Core.Models;

namespace BaseLib.Core.Extensions
{
    /// <summary>Extension methods for <see cref="CoreReasonCode"/>.</summary>
    public static class CoreReasonCodeExtensions
    {
        /// <summary>
        /// Checks if the CoreReasonCode has the specified flag set.
        /// </summary>
        /// <typeparam name="T">The enum type to check against</typeparam>
        /// <param name="reasonCode">The CoreReasonCode instance</param>
        /// <param name="flag">The flag to check</param>
        /// <returns>True if the flag is set, false otherwise</returns>
        public static bool HasFlag<T>(this CoreReasonCode reasonCode, T flag)
            where T : struct, Enum
        {
            // Convert CoreReasonCode to the specified enum type
            T enumValue = reasonCode.ToEnum<T>();
            
            // Use the enum's HasFlag method
            return enumValue.HasFlag(flag);
        }
    }
}
