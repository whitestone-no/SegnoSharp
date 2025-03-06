using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Whitestone.SegnoSharp.Common.Components
{
    public class ValidationSummaryAlert : ValidationSummary
    {
        [CascadingParameter] private EditContext EditContext { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // As an optimization, only evaluate the messages enumerable once, and
            // only produce the enclosing <ul> if there's at least one message
            IEnumerable<string> validationMessages = Model is null ?
                EditContext.GetValidationMessages() :
                EditContext.GetValidationMessages(new FieldIdentifier(Model, string.Empty));

            var first = true;
            foreach (string error in validationMessages)
            {
                if (first)
                {
                    first = false;

                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "alert alert-error");
                    builder.AddMultipleAttributes(2, AdditionalAttributes);

                    builder.OpenElement(3, "ul");
                }

                builder.OpenElement(4, "li");
                builder.AddContent(5, error);
                builder.CloseElement();
            }

            if (first)
            {
                return;
            }
            
            // We have at least one validation message.
            builder.CloseElement();
            builder.CloseElement();
        }
    }
}
