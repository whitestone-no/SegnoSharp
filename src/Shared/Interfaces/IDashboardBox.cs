using System;

namespace Whitestone.SegnoSharp.Shared.Interfaces
{
    public interface IDashboardBox
    {
        static abstract string Name { get; }
        static string Title => null;
        static string AdditionalCss => null;
    }
}
