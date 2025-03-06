using System;

namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface ISystemClock
    {
        DateTime Now { get; }
    }
}
