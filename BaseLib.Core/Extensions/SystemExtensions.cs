using System.ComponentModel;

namespace System
{
    /// <summary>Extension methods for core .NET types.</summary>
    public static class SystemExtensions
    {
        /// <summary>
        /// Returns the <see cref="System.ComponentModel.DescriptionAttribute"/> value for an enum member,
        /// falling back to the member name if no attribute is present.
        /// Supports <see cref="FlagsAttribute"/> enums with multiple combined values.
        /// </summary>
        public static string GetDescription(this Enum @enum)
        {
            var enumString = @enum.ToString();
            var type = @enum.GetType();

            // Handle Flags enums with multiple values
            var memberNames = enumString.Contains(',')
                ? enumString.Split(new[] { ", " }, StringSplitOptions.None)
                : [enumString];

            return string.Join(", ", memberNames.Select(memberName =>
            {
                var memberInfo = type.GetMember(memberName)[0];
                if (memberInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .FirstOrDefault() is DescriptionAttribute attr)
                {
                    return attr.Description;
                }

                return @enum.ToString();
            }));
        }

    }
}
