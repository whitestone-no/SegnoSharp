using System;
using System.Security.Cryptography;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Shared.Helpers
{
    public class RandomGenerator : IRandomGenerator
    {
        public int GetInt(int upperBound)
        {
            return RandomNumberGenerator.GetInt32(upperBound);
        }

        public void GetBytes(Span<byte> buffer)
        {
            RandomNumberGenerator.Fill(buffer);
        }
    }
}
