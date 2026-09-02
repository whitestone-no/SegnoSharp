using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Models.Security;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Services;

public class ApiKeyStore(
    SegnoSharpDbContext db,
    ApiKeyCache cache,
    ISystemClock clock,
    ApiKeyUsageBuffer usage)
{
    private static readonly byte[] DummyHash = new byte[32];

    public async Task<ApiKeyValidationResult> ValidateAsync(
        string presented, CancellationToken ct = default)
    {
        if (!ApiKeyFormat.TryParse(presented, out ReadOnlySpan<char> prefixSpan, out ReadOnlySpan<char> secretSpan))
        {
            return new ApiKeyValidationResult(ApiKeyValidationOutcome.Malformed);
        }

        var prefix = prefixSpan.ToString();
        byte[] candidate = ApiKeyFormat.HashSecret(secretSpan);

        ApiKeyRecord record = await GetByPrefixAsync(prefix, ct);

        // Compare unconditionally so timing does not reveal whether the prefix exists.
        bool matches = CryptographicOperations.FixedTimeEquals(candidate, record?.Hash ?? DummyHash);

        if (record is null)
        {
            return new ApiKeyValidationResult(ApiKeyValidationOutcome.UnknownPrefix);
        }

        if (!matches)
        {
            return new ApiKeyValidationResult(ApiKeyValidationOutcome.BadSecret, Prefix: prefix);
        }

        DateTime now = clock.UtcNow;

        if (!record.ClientEnabled || record.RevokedUtc is not null || record.ExpiresUtc <= now)
        {
            return new ApiKeyValidationResult(ApiKeyValidationOutcome.Inactive, Prefix: prefix);
        }

        usage.Touch(record.KeyId, now);

        return new ApiKeyValidationResult(
            ApiKeyValidationOutcome.Success,
            record.KeyId,
            record.ApiClientId,
            record.DisplayName,
            record.Prefix);
    }

    private async Task<ApiKeyRecord> GetByPrefixAsync(string prefix, CancellationToken ct)
    {
        if (cache.TryGet(prefix, out ApiKeyRecord cached))
        {
            return cached; // may be a cached miss (null)
        }

        ApiKeyRecord record = await db.SecurityApiKeys
            .Where(k => k.Prefix == prefix)
            .Select(k => new ApiKeyRecord(
                k.Id,
                k.SecurityApiClientId,
                k.Prefix,
                k.Hash,
                k.Revoked,
                k.Expires,
                k.Client.Name,
                k.Client.Enabled))
            .FirstOrDefaultAsync(ct);

        cache.Set(prefix, record);
        return record;
    }
}