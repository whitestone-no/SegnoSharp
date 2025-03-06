using System;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Configuration;

namespace Whitestone.SegnoSharp.Shared.Helpers
{
    internal class HashingUtil(IOptions<CommonConfig> commonConfig) : IHashingUtil
    {
        public byte[] Hash(string value)
        {
            if (commonConfig?.Value == null ||
                string.IsNullOrEmpty(commonConfig.Value.SharedSecret))
            {
                throw new InvalidOperationException("SharedSecret not found in configuration");
            }

            var toBeHashed = $"{value}--{commonConfig.Value.SharedSecret}";
            byte[] toBeHashedBytes = Encoding.UTF8.GetBytes(toBeHashed);

            return SHA256.HashData(toBeHashedBytes);
        }

        public string GetAlbumCoverUri(int albumId, int width = 500)
        {
            string hash = GetAlbumCoverHash(albumId, width);
            return $"/img/albumcover/{albumId}?w={width}&hash={hash}";
        }

        public string GetAlbumCoverHash(int albumId, int width = 500)
        {
            return Convert.ToHexStringLower(Hash($"{albumId}-{width}"))[..10];
        }
    }
}
