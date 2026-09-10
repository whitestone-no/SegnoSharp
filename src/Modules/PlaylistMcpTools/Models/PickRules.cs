namespace Whitestone.SegnoSharp.Modules.PlaylistMcpTools.Models
{
    public record PickRules(
        int MinutesBetweenTrackRepeat,
        int MinutesBetweenAlbumRepeat,
        bool UseWeights);
}
