using Whitestone.SegnoSharp.Common.Extensions;

namespace Whitestone.SegnoSharp.Database.Extensions
{
    public static class AlbumCoverExtensions
    {
        public static string ToFileName(this string input, string mime)
        {
            string filename = input.ToSafeFileName();
            string extension = mime.GetExtensionFromMime();

            return filename + extension;
        }
    }
}
