using System;
using Whitestone.SegnoSharp.Common.Extensions;
using Whitestone.SegnoSharp.Database.Models;

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

        public static string ToInlineBase64(this AlbumCover cover)
        {
            return $"data:{cover.Mime};base64,{Convert.ToBase64String(cover.AlbumCoverData.Data)}";
        }
    }
}
