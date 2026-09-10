using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Whitestone.SegnoSharp.Services;

internal sealed class UniqueMcpNameValidator(
    IEnumerable<McpServerTool> tools,
    IEnumerable<McpServerPrompt> prompts,
    IEnumerable<McpServerResource> resources) : IValidateOptions<McpServerOptions>
{
    public ValidateOptionsResult Validate(string name, McpServerOptions options)
    {
        var failures = new List<string>();

        Collect(failures, "tool", tools.Select(t => t.ProtocolTool.Name));
        Collect(failures, "prompt", prompts.Select(p => p.ProtocolPrompt.Name));
        Collect(failures, "resource URI", resources.Select(r => r.ProtocolResourceTemplate.UriTemplate));

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
    private static void Collect(List<string> failures, string kind, IEnumerable<string> ids)
    {
        IEnumerable<IGrouping<string, string>> duplicates = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1);

        foreach (IGrouping<string, string> group in duplicates)
        {
            failures.Add($"Duplicate {kind} '{group.Key}' ({group.Count()} registrations).");
        }
    }
}