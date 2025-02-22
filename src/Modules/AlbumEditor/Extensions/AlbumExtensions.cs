using System;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Modules.AlbumEditor.Extensions
{
    public static class AlbumExtensions
    {
        public static string GetCoverImage(this Album album)
        {
            if (album.AlbumCover == null)
            {
                return null;
            }

            return $"data:{album.AlbumCover.Mime};base64,{Convert.ToBase64String(album.AlbumCover.AlbumCoverData.Data)}";
        }
    }
}
