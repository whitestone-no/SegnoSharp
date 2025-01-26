using System.Security.Cryptography;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace Whitestone.SegnoSharp.Common.Helpers
{
    public class RandomGenerator : IRandomGenerator
    {
        public int GetInt(int upperBound)
        {
            return RandomNumberGenerator.GetInt32(upperBound);
        }
    }
}
