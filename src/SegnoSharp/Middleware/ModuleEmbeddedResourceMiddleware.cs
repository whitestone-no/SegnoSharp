using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Whitestone.SegnoSharp.Shared.Interfaces;
using Whitestone.SegnoSharp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.StaticFiles;

namespace Whitestone.SegnoSharp.Middleware
{
    public class ModuleEmbeddedResourceMiddleware(
        RequestDelegate next,
        IOptions<ModuleEmbeddedResourceOptions> options,
        IEnumerable<IModule> modules,
        ILoggerFactory loggerFactory)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<ModuleEmbeddedResourceMiddleware>();

        public async Task Invoke(HttpContext context)
        {
            // ReSharper disable once PossibleNullReferenceException
            // Path.Value is checked for null in the previous statement
            if (!(context.Request.Path.HasValue && context.Request.Path.Value.StartsWith($"/{options.Value.Prefix}/")))
            {
                // Not a request for a module embedded resource
                // Continue to next middleware
                await next(context);
                return;
            }

            // Fetch the appropriate module from second path parameter
            string[] pathSegments = context.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (pathSegments.Length < 2)
            {
                _logger.LogWarning("No module specified in path.");
                return;
            }

            string moduleName = pathSegments[1];
            IModule module = modules.FirstOrDefault(m => m.GetType().Assembly.GetName().Name == moduleName);
            if (module == null)
            {
                _logger.LogWarning("Module '{moduleName}' not found.", moduleName);
                return;
            }

            // Fetch the path and file from the third path parameter and onwards
            if (pathSegments.Length < 3)
            {
                _logger.LogWarning("No file specified in request.");
                return;
            }

            string filePath = string.Join('/', pathSegments.Skip(2));
            string resourceName = "wwwroot." + filePath.Replace('/', '.');
            Assembly moduleAssembly = module.GetType().Assembly;
            string resourceFullName = moduleAssembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));
            
            if (resourceFullName == null)
            {
                _logger.LogWarning("Resource '{filePath}' not found in module '{moduleName}'.", filePath, moduleName);
                return;
            }

            await using Stream stream = moduleAssembly.GetManifestResourceStream(resourceFullName);

            if (stream == null)
            {
                _logger.LogWarning("Resource stream for '{filePath}' not found in module '{moduleName}'.", filePath, moduleName);
                return;
            }

            string contentType = GetContentType(resourceName);
            context.Response.ContentType = contentType;
            await stream.CopyToAsync(context.Response.Body);
        }

        private static string GetContentType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            
            if (!provider.TryGetContentType(fileName, out string contentType))
            {
                contentType = "application/octet-stream";
            }

            return contentType;
        }
    }

    public static class ModuleEmbeddedResourceMiddlewareExtensions
    {
        public static IApplicationBuilder UseModuleEmbeddedResource(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            return app.UseModuleEmbeddedResource(new ModuleEmbeddedResourceOptions());
        }

        public static IApplicationBuilder UseModuleEmbeddedResource(this IApplicationBuilder app, ModuleEmbeddedResourceOptions options)
        {
            ArgumentNullException.ThrowIfNull(app);
            ArgumentNullException.ThrowIfNull(options);

            return app.UseMiddleware<ModuleEmbeddedResourceMiddleware>(Options.Create(options));
        }
    }
}
