using System;

namespace Whitestone.SegnoSharp.Models.Security;

public sealed record ApiKeyCreationResult(string PlainText, string Prefix, byte[] Hash);

public enum ApiKeyValidationOutcome { Success, Malformed, UnknownPrefix, BadSecret, Inactive }

public sealed record ApiKeyValidationResult(
    ApiKeyValidationOutcome Outcome,
    int KeyId = 0,
    int ApiClientId = 0,
    string DisplayName = "",
    string Prefix = "");

public sealed record ApiKeyRecord(
    int KeyId,
    int ApiClientId,
    string Prefix,
    byte[] Hash,
    DateTime? RevokedUtc,
    DateTime? ExpiresUtc,
    string DisplayName,
    bool ClientEnabled);