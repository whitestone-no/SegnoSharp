using System;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Shared.Helpers
{
    public class SystemClock : ISystemClock
    {
        public DateTime Now => DateTime.Now;
    }
}
