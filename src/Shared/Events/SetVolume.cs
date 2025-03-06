namespace Whitestone.SegnoSharp.Common.Events
{
    public class SetVolume
    {
        private byte _volume;

        public SetVolume(byte volume)
        {
            Volume = volume;
        }

        public byte Volume
        {
            get => _volume;
            set => _volume = value > 100 ? (byte)100 : value;
        }
    }
}
