using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Modules.Dashboard.Models;
using Whitestone.SegnoSharp.Modules.Dashboard.ViewModels;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Modules.Dashboard.Components.Pages
{
    public partial class Dashboard
    {
        [Inject] private IEnumerable<IModule> Modules { get; set; }
        [Inject] private DashboardSettings DashboardSettings { get; set; }

        private List<DashboardBoxViewModel> DashboardBoxes { get; set; } = [];

        protected override void OnInitialized()
        {
            foreach (IModule module in Modules)
            {
                foreach (Type dashboardBoxType in module.GetType().Assembly.GetTypes())
                {
                    if (!dashboardBoxType.IsAssignableTo(typeof(IComponent)) ||
                        !dashboardBoxType.IsAssignableTo(typeof(IDashboardBox)))
                    {
                        continue;
                    }

                    DashboardBoxes.Add(new DashboardBoxViewModel
                    {
                        Id = dashboardBoxType.FullName?.Replace(".", "-"),
                        Type = dashboardBoxType,
                        Title = dashboardBoxType.GetProperty(nameof(IDashboardBox.Title))?.GetValue(null)?.ToString(),
                        AdditionalCss = dashboardBoxType.GetProperty(nameof(IDashboardBox.AdditionalCss))?.GetValue(null)?.ToString()
                    });
                }
            }

            // Join the available boxes with settings to get the correct order
            DashboardBoxes = DashboardSettings.DashboardBoxes
                .Join(
                    DashboardBoxes,
                    setting => setting,
                    box => box.Id,
                    (_, box) => box)
                .ToList();
        }
    }
}
