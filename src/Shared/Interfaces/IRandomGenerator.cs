namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface IRandomGenerator
    {
        /// <summary>
        /// Generates a random integer between 0 and upperBound
        /// </summary>
        /// <param name="upperBound">Exclusive upper bound</param>
        /// <returns>The random number</returns>
        int GetInt(int upperBound);
    }
}
