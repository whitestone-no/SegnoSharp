namespace Whitestone.SegnoSharp.Modules.AiAgentTools.Models
{
    public record PickRules(
        int MinutesBetweenTrackRepeat,
        int MinutesBetweenAlbumRepeat,
        bool UseWeights);
}
