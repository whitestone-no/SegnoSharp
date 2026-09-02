namespace Whitestone.SegnoSharp.Shared.Interfaces;

/// <summary>
/// Marks an endpoint as part of the machine-facing API: errors are returned as
/// RFC 9457 problem details instead of re-executing into an HTML error page.
/// </summary>
public interface IApiEndpointMetadata
{
}