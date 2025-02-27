using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Loggle.Web.Configuration;
using Loggle.Web.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;

namespace Loggle.Web.Controllers;

[ApiController]
[Route("")]
public class LogsController : ControllerBase
{
    private readonly LogIngestionService _logIngestionService;

    public LogsController(LogIngestionService logIngestionService)
    {
        ThrowHelper.ThrowIfNull(logIngestionService);

        _logIngestionService = logIngestionService;
    }

    [HttpPost]
    [Route("v1/logs")]
    public async Task<IResult> IngestLogsAsync(CancellationToken cancellationToken)
    {
        // Receives the protobuf
        try
        {
            using var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var logsData = OtlpLogs.LogsData.Parser.ParseFrom(stream);

            var context = new OtlpContext
            {
                Options = new TelemetryLimitOptions { }
            };

            foreach (var rl in logsData.ResourceLogs)
            {
                var application = new OtlpApplication(context, rl.Resource);

                foreach (var sl in rl.ScopeLogs)
                {
                    var logs = new List<OtlpLogEntry>(sl.LogRecords.Count);
                    foreach (var record in sl.LogRecords)
                    {
                        var logEntry = new OtlpLogEntry(context, application, record);
                        logs.Add(logEntry);
                    }

                    var response = await _logIngestionService
                        .IngestAsync(
                            logs,
                            cancellationToken
                        ).ConfigureAwait(false);
                }
            }

            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.InternalServerError(ex);
        }
    }
}
