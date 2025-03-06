using System;

namespace Whitestone.SegnoSharp.Common.Interfaces
{
    public interface ISystemClock
    {
        DateTime Now { get; }
    }
}
