using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Whitestone.SegnoSharp.Common.Components
{
    public class ValidationCustomMessage<TValue> : ValidationMessage<TValue>
    {
        [CascadingParameter] private EditContext EditContext { get; set; } = null;
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (EditContext.IsValid(FieldIdentifier.Create(For!)))
            {
                return;
            }

            builder.AddContent(0, ChildContent);
        }
    }
}
