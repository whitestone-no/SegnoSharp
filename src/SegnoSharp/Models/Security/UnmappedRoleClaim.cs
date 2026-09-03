using System;

namespace Whitestone.SegnoSharp.Models.Security;

public sealed record UnmappedRoleClaim(string Value, string Label, DateTime LastSeen);