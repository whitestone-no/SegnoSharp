namespace Whitestone.SegnoSharp.Shared.Events
{
    public class GetListenersRequest
    {
    }

    public class GetListenersResponse
    {
        public int Listeners { get; set; }
        public int PeakListeners { get; set; }
    }
}