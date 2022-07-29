using System;

namespace BassService.Models.Bass
{
    public delegate void SYNCPROC(int handle, int channel, int data, IntPtr user);
}
