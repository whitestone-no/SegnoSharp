using System;

namespace Whitestone.SegnoSharp.Common.Extensions
{
    public static class StringExtensions
    {
        public static string TrimStart(this string source, string value, StringComparison comparisonType = StringComparison.InvariantCulture)
        {
            if (source == null)
            {
                return null;
            }

            int valueLength = value.Length;
            var startIndex = 0;
            while (source.IndexOf(value, startIndex, comparisonType) == startIndex)
            {
                startIndex += valueLength;
            }

            return source.Substring(startIndex);
        }
    }
}