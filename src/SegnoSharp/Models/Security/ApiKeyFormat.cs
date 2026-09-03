using System;
using System.Security.Cryptography;
using System.Text;

namespace Whitestone.SegnoSharp.Models.Security;

public static class ApiKeyFormat
{
    public const string PrefixMarker = "sgns_";
    public const char Separator = '.';
    public const int PrefixRandomBytes = 6;
    public const int SecretRandomBytes = 32;
    public const int PrefixLength = 17;                 // "sgns_" + 12 hex chars
    public const int SecretLength = 43;                 // base64url of 32 bytes, unpadded
    public const int TotalLength = PrefixLength + 1 + SecretLength;

    public static byte[] HashSecret(ReadOnlySpan<char> secret)
    {
        int maxBytes = Encoding.UTF8.GetMaxByteCount(secret.Length);
        Span<byte> bytes = maxBytes <= 128 ? stackalloc byte[128] : new byte[maxBytes];

        int written = Encoding.UTF8.GetBytes(secret, bytes);
        return SHA256.HashData(bytes[..written]);
    }

    /// <summary>
    /// Rejects malformed input before any I/O — most scanner traffic stops here.
    /// </summary>
    public static bool TryParse(
        ReadOnlySpan<char> presented,
        out ReadOnlySpan<char> prefix,
        out ReadOnlySpan<char> secret)
    {
        prefix = secret = default;

        if (presented.Length != TotalLength) return false;
        if (!presented.StartsWith(PrefixMarker)) return false;
        if (presented[PrefixLength] != Separator) return false;

        for (int i = PrefixMarker.Length; i < PrefixLength; i++)
            if (!char.IsAsciiHexDigitLower(presented[i])) return false;

        for (int i = PrefixLength + 1; i < TotalLength; i++)
        {
            char c = presented[i];
            if (!char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_') return false;
        }

        prefix = presented[..PrefixLength];
        secret = presented[(PrefixLength + 1)..];
        return true;
    }
}