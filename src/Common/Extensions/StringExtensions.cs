using System;
using System.Collections.Generic;
using System.Linq;

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

            return source[startIndex..];
        }
        public static string ToSafeFileName(this string input)
        {
            return string.Concat(input.Where(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'));
        }

        public static string GetExtensionFromMime(this string mime)
        {
            // Regex to switch these around with Notepad++
            // Search: { "(\.\w*)", "(\w*\/[\.\+\w-]*)" },?
            // Replace: { "\2", "\1" },
            Dictionary<string, string> mappings = new(StringComparer.OrdinalIgnoreCase)
            {
                { "audio/aac", ".aac" },
                { "audio/flac", ".flac" },
                { "audio/mpeg", ".mp3" },
                { "audio/mp4", ".m4a" },
                { "audio/ogg", ".ogg" },
                { "audio/wav", ".wav" },
                { "audio/x-ms-wma", ".wma" },
                { "image/bmp", ".bmp" },
                { "image/gif", ".gif" },
                { "image/pjpeg", ".pjpg" },
                { "image/jpeg", ".jpg" },
                { "image/png", ".png" },
                { "image/tiff", ".tif" },
                { "image/webp", ".webp" },
            };

            mappings.TryGetValue(mime, out string extension);

            return extension ?? ".bin";
        }
    }
}