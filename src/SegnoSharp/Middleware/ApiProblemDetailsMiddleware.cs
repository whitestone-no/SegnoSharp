using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Shared.Interfaces;

namespace Whitestone.SegnoSharp.Middleware;

public sealed class ApiProblemDetailsMiddleware(RequestDelegate next)
{
    /// <summary>Set for API endpoints so the exception handler can identify them after the endpoint is cleared.</summary>
    public const string ApiEndpointItemKey = "__IsSegnoSharpApiEndpoint";

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        if (context.GetEndpoint()?.Metadata.GetMetadata<IApiEndpointMetadata>() is null)
        {
            await next(context);

            return;
        }

        context.Items[ApiEndpointItemKey] = true;

        // Suppress the HTML re-execution registered further up the pipeline.
        if (context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
        {
            statusCodePages.Enabled = false;
        }

        await next(context);

        if (context.Response.HasStarted ||
            context.Response.StatusCode < 400 ||
            context.Response.ContentType is not null)
        {
            // already written, or MVC produced its own problem details
            return;
        }

        await problemDetails.WriteAsync(new ProblemDetailsContext { HttpContext = context });
    }
}