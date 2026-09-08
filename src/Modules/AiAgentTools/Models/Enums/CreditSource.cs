using System.Text.Json.Serialization;

namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models.Enums;

/// <summary>Where an effective credit came from: the track itself, or inherited from its album.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CreditSource>))]
public enum CreditSource
{
    Track,
    Album
}