using System.Collections.Generic;
using Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models.Enums;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models;

/// <summary>
/// One distinct role name, with every level it applies to. The underlying PersonGroups table
/// holds a row per name and type ("Artist" exists for both albums and tracks), but the role
/// filter on the tools is matched as a plain string against the name, so the pair is a single
/// value as far as a caller is concerned. <see cref="AppliesTo"/> uses the same name and values
/// as a credit's, so a caller reads them as one concept.
/// </summary>
public record RoleResult(
    string RoleName,
    IReadOnlyList<CreditLevel> AppliesTo);