using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Whitestone.Cambion.Interfaces;
using Whitestone.SegnoSharp.Database;
using Whitestone.SegnoSharp.Database.Models;
using Whitestone.SegnoSharp.Shared.Events;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Shared.Models.Persistent;

namespace Whitestone.SegnoSharp.Modules.StreamControls.Components.Dashboard
{
    public partial class StreamControls : IDashboardBox
    {
        public static string Name => "Stream controls (Admin)";
        public static string Title => "Stream controls";
        public static string AdditionalCss => $"_moduleresource/{typeof(StreamStats).Assembly.GetName().Name}/dashboard.css";

        [Inject] private StreamingSettings StreamingSettings { get; set; }
        [Inject] private ICambion Cambion { get; set; }

        private async Task FireEvent_PlayNextTrack_Click()
        {
            await Cambion.PublishEventAsync(new PlayNextTrack());
        }

        private async Task FireEvent_ReplayCurrentTrack_Click()
        {
            await Cambion.PublishEventAsync(new ReplayCurrentTrack());
        }
    }
}
