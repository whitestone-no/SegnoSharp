using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using System;
using Whitestone.SegnoSharp.Shared.Models.Configuration;

namespace Whitestone.SegnoSharp.Components.Pages
{
    public partial class Index
    {
        [Inject] private IOptions<SiteConfig> SiteConfig { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }

        protected override void OnInitialized()
        {
            if (!string.IsNullOrEmpty(SiteConfig.Value.StartPage))
            {
                NavigationManager.NavigateTo(SiteConfig.Value.StartPage, true);
            }
        }
    }
}
