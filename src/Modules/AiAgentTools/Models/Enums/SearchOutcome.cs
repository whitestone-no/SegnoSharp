using System.Text.Json.Serialization;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;

/// <summary>Outcome of a track search, so the agent can branch (e.g. go to external lookup on a miss).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SearchOutcome>))]
public enum SearchOutcome
{
    /// <summary>At least one candidate scored at or above the requested minScore.</summary>
    Matched,

    /// <summary>Candidates exist but the best one is below minScore. Verify before acting, or resolve the exact title externally.</summary>
    WeakMatch,

    /// <summary>Nothing matched in this scope. Resolve the concrete title (external lookup) and search again.</summary>
    NoMatch
}