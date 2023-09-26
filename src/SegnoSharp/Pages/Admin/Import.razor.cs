using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Whitestone.SegnoSharp.Models.States;

namespace Whitestone.SegnoSharp.Pages.Admin
{
    public partial class Import
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }

        private RenderFragment RenderPath()
        {
            void Renderer(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "ul");

                AddPathLevel(builder, ImporterState.SelectedFolder);

                builder.CloseElement();
            }

            return Renderer;
        }

        private void AddPathLevel(RenderTreeBuilder builder, DirectoryInfo di)
        {
            if (di == null)
            {
                builder.OpenElement(1, "li");
                builder.OpenElement(2, "button");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => OnPathClick(null)));
                builder.AddContent(3, "Computer");
                builder.CloseElement();
                builder.CloseElement();

                return;
            }

            AddPathLevel(builder, di.Parent);

            builder.OpenElement(1, "li");
            builder.OpenElement(2, "button");
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => OnPathClick(di)));
            builder.AddContent(3, di.Name);
            builder.CloseElement();
            builder.CloseElement();
        }

        private void OnPathClick(DirectoryInfo di)
        {
            ImporterState.SelectedFolder = di;
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("/admin/import/step-2");
        }
    }
}