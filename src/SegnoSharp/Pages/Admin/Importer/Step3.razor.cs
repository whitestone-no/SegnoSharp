using Microsoft.AspNetCore.Components;
using Whitestone.SegnoSharp.Models.States;

namespace Whitestone.SegnoSharp.Pages.Admin.Importer
{
    public partial class Step3
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("/admin/import/step-4");
        }

        private void OnBackClick()
        {
            NavigationManager.NavigateTo("/admin/import/step-2");
        }
    }
}
