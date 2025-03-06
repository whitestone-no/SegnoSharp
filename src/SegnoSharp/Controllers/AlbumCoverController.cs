using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SkiaSharp;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Controllers
{
    [ApiController]
    [Route("/img/[controller]")]
    public class AlbumCoverController(
        IDbContextFactory<SegnoSharpDbContext> dbContextFactory,
        IDistributedCache cache,
        IHashingUtil hashingUtil) : ControllerBase
    {
        private DistributedCacheEntryOptions _cacheOptions = new()
        {
            SlidingExpiration = TimeSpan.FromHours(2),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
        };

        [HttpGet]
        [Route("{albumId:int}")]
        public async Task<IActionResult> Index([FromRoute] int albumId, [FromQuery] string hash, [FromQuery(Name = "w")] int width = 500)
        {
            SegnoSharpDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

            AlbumCover cover = await dbContext.AlbumCovers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.AlbumId == albumId);

            if (cover == null)
            {
                return NotFound();
            }

            string expectedHash = hashingUtil.GetAlbumCoverHash(albumId, width);
            if (hash != expectedHash)
            {
                return NotFound();
            }

            string cacheKey = GetCacheKey(albumId, width);

            byte[] cachedData = await cache.GetAsync(cacheKey);

            if (cachedData != null)
            {
                return File(cachedData, cover.Mime);
            }

            AlbumCoverData coverData = await dbContext.AlbumCoversData
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == cover.Id);

            if (coverData == null)
            {
                return NotFound();
            }

            if (width == 0)
            {
                await cache.SetAsync(cacheKey, coverData.Data, _cacheOptions);
                return File(coverData.Data, cover.Mime);
            }

            SKImage img = SKImage.FromEncodedData(coverData.Data);

            SKBitmap bmp = SKBitmap.Decode(coverData.Data);
            double aspectRatio = (double)bmp.Width / bmp.Height;

            var newHeight = (int)(width / aspectRatio);

            SKBitmap resized = bmp.Resize(new SKSizeI(width, newHeight), new SKSamplingOptions(SKCubicResampler.CatmullRom));
            SKData encoded = resized.Encode(ParseMime(cover.Mime), 90);

            byte[] final = encoded.ToArray();

            await cache.SetAsync(cacheKey, final, _cacheOptions);

            return File(final, cover.Mime);
        }

        private static string GetCacheKey(int albumId, int? width)
        {
            var key = $"album-cover-{albumId}";

            if (width.HasValue)
            {
                key += $"-w{width}";
            }

            return key;
        }

        private static SKEncodedImageFormat ParseMime(string mime)
        {
            return mime switch
            {
                "image/png" => SKEncodedImageFormat.Png,
                "image/jpeg" => SKEncodedImageFormat.Jpeg,
                _ => throw new NotSupportedException($"Unsupported image format: {mime}")
            };
        }
    }
}
