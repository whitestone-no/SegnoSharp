using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Whitestone.SegnoSharp.Shared.Attributes.Controllers;

namespace Whitestone.SegnoSharp.Configuration.Controllers;

public sealed class GlobalRoutePrefixConvention : IApplicationModelConvention
{
    private readonly string _prefix;

    public GlobalRoutePrefixConvention(string prefix)
    {
        _prefix = prefix.Trim().Trim('~').Trim('/');

        if (_prefix.Length == 0)
        {
            throw new ArgumentException("Prefix must not be empty.", nameof(prefix));
        }
    }

    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            if (HasOptOut(controller.Attributes))
            {
                continue;
            }

            // Does the controller itself carry an attribute route that actions can inherit?
            bool controllerIsAttributeRouted = controller.Selectors.Any(s => s.AttributeRouteModel is not null);

            foreach (SelectorModel selector in controller.Selectors)
            {
                Prefix(selector); // no-ops when there is no attribute route
            }

            foreach (ActionModel action in controller.Actions)
            {
                if (HasOptOut(action.Attributes))
                {
                    continue;
                }

                foreach (SelectorModel selector in action.Selectors)
                {
                    if (selector.AttributeRouteModel?.Template is not { } template)
                    {
                        continue; // [HttpGet] with no template: inherits the controller route
                    }

                    // Prefix the action only when it won't inherit an already-prefixed
                    // controller route: either it overrides it, or there isn't one.
                    if (controllerIsAttributeRouted && !IsOverride(template))
                    {
                        continue;
                    }

                    Prefix(selector);
                }
            }
        }
    }

    private void Prefix(SelectorModel selector)
    {
        if (selector.AttributeRouteModel is not { } model)
        {
            return;
        }

        string template = Normalize(model.Template);

        if (IsAlreadyPrefixed(template))
        {
            return;
        }

        model.Template = template.Length == 0 ? _prefix : $"{_prefix}/{template}";
    }

    private static bool HasOptOut(IReadOnlyList<object> attributes) => attributes.OfType<SkipGlobalRoutePrefixAttribute>().Any();

    private static bool IsOverride(string template) => template.StartsWith('/') || template.StartsWith("~/");

    private static string Normalize(string template) => (template ?? string.Empty).TrimStart('~').TrimStart('/');

    private bool IsAlreadyPrefixed(string normalizedTemplate) =>
        normalizedTemplate.Equals(_prefix, StringComparison.OrdinalIgnoreCase) ||
        normalizedTemplate.StartsWith(_prefix + "/", StringComparison.OrdinalIgnoreCase);

}