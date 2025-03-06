namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface IHashingUtil
    {
        byte[] Hash(string input);
        string GetAlbumCoverUri(int albumId, int width = 500);
        string GetAlbumCoverHash(int albumId, int width = 500);
    }
}
