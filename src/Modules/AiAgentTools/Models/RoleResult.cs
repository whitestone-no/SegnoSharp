using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models;

/// <summary>
/// One distinct role name, with every scope it applies to. The underlying PersonGroups table
/// holds a row per name/scope pair ("Artist" exists at both Album and Track scope), but the
/// role filter on the tools is matched as a plain string against the name, so the pair is a
/// single value as far as a caller is concerned.
/// </summary>
public record RoleResult(
    string RoleName,
    IReadOnlyList<CreditSource> Scopes);