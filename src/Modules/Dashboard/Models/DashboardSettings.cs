using System;
using System.Collections.Generic;
using Whitestone.SegnoSharp.Shared.Attributes.PersistenceManager;

namespace Whitestone.SegnoSharp.Modules.Dashboard.Models
{
    public class DashboardSettings
    {
        [Persist]
        [DefaultValue(["Whitestone-SegnoSharp-Modules-Dashboard-Components-Dashboard-Welcome"])]
        [Description("Dashboard boxes")]
        public List<string> DashboardBoxes { get; set; }

    }
}
