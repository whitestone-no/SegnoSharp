using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Whitestone.SegnoSharp.Services;

internal sealed class UniqueMcpToolNameValidator(IEnumerable<McpServerTool> tools) : IValidateOptions<McpServerOptions>
{
    public ValidateOptionsResult Validate(string name, McpServerOptions options)
    {
        string[] duplicates = tools
            .GroupBy(t => t.ProtocolTool.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        return duplicates.Length == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"Duplicate MCP tool names: {string.Join(", ", duplicates)}");
    }
}