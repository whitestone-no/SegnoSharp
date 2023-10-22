using System.Threading.Tasks;

namespace Whitestone.SegnoSharp.Pages.Admin.Importer
{
    public partial class Step4
    {
        private int TotalNumberOfSteps { get; set; } = 10;
        private int CurrentStep { get; set; }

        protected override void OnInitialized()
        {
            Task.Run(UpdatePercent);
        }

        private async Task UpdatePercent()
        {
            for (var i = 0; i <= 10; i++)
            {
                CurrentStep = i;
                
                await InvokeAsync(StateHasChanged);

                await Task.Delay(500);
            }
        }
    }
}
