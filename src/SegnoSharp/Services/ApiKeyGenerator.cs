using System;
using Microsoft.AspNetCore.Authentication;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Services;

public sealed class ApiKeyGenerator(IRandomGenerator random)
{
    public ApiKeyCreationResult Create()
    {
        Span<byte> prefixBytes = stackalloc byte[ApiKeyFormat.PrefixRandomBytes];
        random.GetBytes(prefixBytes);

        Span<byte> secretBytes = stackalloc byte[ApiKeyFormat.SecretRandomBytes];
        random.GetBytes(secretBytes);

        string prefix = ApiKeyFormat.PrefixMarker + Convert.ToHexStringLower(prefixBytes);
        string secret = Base64UrlTextEncoder.Encode(secretBytes.ToArray());

        return new ApiKeyCreationResult(
            $"{prefix}{ApiKeyFormat.Separator}{secret}",
            prefix,
            ApiKeyFormat.HashSecret(secret));
    }
}