using System;
using Whitestone.SegnoSharp.Common.Interfaces;

namespace Whitestone.SegnoSharp.Common.Helpers
{
    public class SystemClock : ISystemClock
    {
        public DateTime Now => DateTime.Now;
    }
}
