using System.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Whitestone.SegnoSharp.Modules.MediaImporter.Models;

namespace Whitestone.SegnoSharp.Modules.MediaImporter.Components.Pages
{
    public partial class Step1
    {
        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private ImportState ImporterState { get; set; }

        private RenderFragment RenderPath()
        {
            return Renderer;

            void Renderer(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "ul");

                AddPathLevel(builder, ImporterState.SelectedFolder);

                builder.CloseElement();
            }
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
            builder.AddContent(3, di.Name.TrimEnd('\\', '/'));
            builder.CloseElement();
            builder.CloseElement();
        }

        private void OnPathClick(DirectoryInfo di)
        {
            ImporterState.SelectedFolder = di;
            ImporterState.SelectedFiles = null;
        }

        private void OnNextClick()
        {
            NavigationManager.NavigateTo("admin/mediaimporter/step2");
        }
    }
}