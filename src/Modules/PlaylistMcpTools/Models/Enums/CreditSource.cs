using System.Text.Json.Serialization;

namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models.Enums;

/// <summary>Where an effective credit came from: the track itself, or inherited from its album.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CreditSource>))]
public enum CreditSource
{
    Track,
    Album
}