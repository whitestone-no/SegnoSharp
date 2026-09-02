using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Whitestone.SegnoSharp.Middleware;

namespace Whitestone.SegnoSharp.Configuration.Controllers;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (!context.Items.ContainsKey(ApiProblemDetailsMiddleware.ApiEndpointItemKey))
        {
            // let the HTML error page handle it
            return false;
        }

        logger.LogError(exception, "Unhandled exception in API endpoint {Path}.", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Title = "An unexpected error occurred." }
        });
    }
}