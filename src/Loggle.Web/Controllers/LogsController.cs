using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var dataStreamName = "logs-loggle-default";
            using var stream = new MemoryStream();
            await Request.Body.CopyToAsync(stream);

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            var logsData = OtlpLogs.LogsData.Parser.ParseFrom(stream);
            var logRecord = logsData
                ?.ResourceLogs.FirstOrDefault()
                ?.ScopeLogs.FirstOrDefault()
                ?.LogRecords.FirstOrDefault();

            var otlpContext = new OtlpContext
            {
                Options = new TelemetryLimitOptions { }
            };

            var logEntry = new OtlpLogEntry(logRecord, otlpContext);
            var logs = (IEnumerable<OtlpLogEntry>)[logEntry];

            var response = await _logIngestionService
                .IngestAsync(
                    logs,
                    dataStreamName,
                    cancellationToken
                ).ConfigureAwait(false);

            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.InternalServerError(ex);
        }
    }
}
