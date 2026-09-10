namespace Whitestone.SegnoSharp.Shared.Interfaces;

public interface IMcpProvider : IModule
{
    /// <summary>Prepended to every tool and prompt name this module exposes.</summary>
    string McpPrefix { get; }
    
    /// <summary>URI scheme this module's resources must use, e.g. "alpha" for alpha://…</summary>
    string ResourceUriScheme => null;
}