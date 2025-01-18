using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;

namespace Loggle.Web.Controllers;

[ApiController]
[Route("")]
public class LogsController : ControllerBase
{
    // [AllowAnonymous]
    [HttpPost]
    [Route("v1/logs")]
    public async Task<IResult> IngestLogsAsync()
    {
        try
        {
            using var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream);

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            // Will have errors during dotnet publish
            var logsData = OtlpLogs.LogsData.Parser.ParseFrom(stream);

            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.InternalServerError(ex);
        }
    }
}
