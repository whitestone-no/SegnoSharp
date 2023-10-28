using System;
using Microsoft.AspNetCore.Components;

namespace Whitestone.SegnoSharp.Components
{
    public partial class ProgressBar
    {
        [Parameter]
        public int CurrentStep { get; set; }

        [Parameter]
        public EventCallback<int> CurrentStepChanged { get; set; }

        [Parameter]
        public int TotalSteps { get; set; }

        [Parameter]
        public EventCallback<int> TotalStepsChanged { get; set; }

        private decimal Percentage => TotalSteps == 0 ? 0 : Math.Round((decimal)CurrentStep / TotalSteps * 100);
    }
}
