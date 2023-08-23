using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace Whitestone.SegnoSharp.HealthChecks
{
    public class HealthCheckResponseWriter
    {
        public static Task WriteResponse(HttpContext context, HealthReport healthReport)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(memoryStream, options))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", healthReport.Status.ToString());
                jsonWriter.WriteStartObject("results");

                foreach (KeyValuePair<string, HealthReportEntry> healthReportEntry in healthReport.Entries)
                {
                    jsonWriter.WriteStartObject(healthReportEntry.Key);
                    jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
                    jsonWriter.WriteString("description", healthReportEntry.Value.Description);
                    jsonWriter.WriteStartObject("data");

                    foreach (KeyValuePair<string, object> item in healthReportEntry.Value.Data)
                    {
                        jsonWriter.WritePropertyName(item.Key);

                        JsonSerializer.Serialize(jsonWriter, item.Value, item.Value?.GetType() ?? typeof(object));
                    }

                    jsonWriter.WriteEndObject();
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndObject();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
        }

    }
}
