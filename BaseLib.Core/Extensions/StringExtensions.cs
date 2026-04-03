using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace System
{
    /// <summary>General-purpose extension methods for <see cref="string"/>.</summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Parses a delimited key=value string into a dictionary.
        /// Defaults to <c>key=value;key2=value2</c> format.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(this string text, char valueSeparator = '=', char pairSeparator = ';')
        {
            return text.Split(new char[] { pairSeparator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Split(new char[] { valueSeparator }, 2))
                .ToDictionary(t => t[0].Trim(), t => t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>Converts the string to a <see cref="Stream"/> encoded as UTF-8.</summary>
        public static Stream ToStream(this string s)
        {
            return s.ToStream(Encoding.UTF8);
        }

        /// <summary>Converts the string to a <see cref="Stream"/> using the specified <paramref name="encoding"/>.</summary>
        public static Stream ToStream(this string s, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(s));
        }

        /// <summary>Attempts to decode a Base64 string. Returns <see langword="false"/> if the input is not valid Base64.</summary>
        public static bool TryConvertFromBase64String(string s, out byte[] bytes)
        {
            try
            {
                bytes = Convert.FromBase64String(s);
                return true;
            }
            catch
            {
            }

            bytes = new byte[0];
            return false;

        }

        /// <summary>Converts the string to camelCase, normalising special characters first.</summary>
        public static string ToCamelCase(this string text)
        {
            return GetCasedString(text.NormalizeSpecialChars(), false);
        }

        /// <summary>Converts the string to PascalCase, normalising special characters first.</summary>
        public static string ToPascalCase(this string text)
        {
            return GetCasedString(text.NormalizeSpecialChars());
        }

        private static string GetCasedString(string text, bool pascalCasing = true)
        {
            StringBuilder sb = new StringBuilder();
            var count = 0;

            var match = Regex.Match(text, @"\w+");
            while (match.Success)
            {
                foreach (Group group in match.Groups)
                {
                    char[] buffer = group.Value.ToLowerInvariant().ToCharArray();
                    if (pascalCasing || count > 0)
                    {
                        buffer[0] = char.ToUpperInvariant(buffer[0]);
                    }
                    sb.Append(new string(buffer));
                    count++;
                }
                match = match.NextMatch();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Replaces accented Latin characters (U+00C0–U+00FF) with their ASCII equivalents
        /// and converts underscores to spaces. Prefixes a leading underscore if the string starts with a digit.
        /// </summary>
        public static string NormalizeSpecialChars(this string text)
        {
            var buffer = text.ToCharArray();
            var bufferLength = buffer.Length;

            var startWithDigit = Char.IsDigit(buffer[0]);

            for (int i = 0; i < bufferLength; i++)
            {
                var currentChar = buffer[i];
                if (currentChar >= 0x00C0 && currentChar <= 0x00FF)
                {
                    buffer[i] = LatinToUnicodeTable[currentChar - 0x00C0];
                }
                else if (currentChar == '_')
                {
                    buffer[i] = ' ';
                }

            }

            var str = new string(buffer);
            if (startWithDigit)
            {
                return string.Format("_{0}", str);
            }

            return str;
        }

        //Translate table
        private static readonly char[] LatinToUnicodeTable = new char[]{
            'A','A','A','A','A','A','A','C',
            'E','E','E','E','I','I','I','I',
            'E','N','O','O','O','O','O','*',
            'O','U','U','U','U','Y','T','s',
            'a','a','a','a','a','a','a','c',
            'e','e','e','e','i','i','i','i',
            'e','n','o','o','o','o','o','/',
            'o','u','u','u','u','y','t','y'
        };

    }
}
