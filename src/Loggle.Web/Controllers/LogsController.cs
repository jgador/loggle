using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Proto.Collector.Logs.V1;

namespace Loggle.Web.Controllers;

[ApiController]
[Route("")]
public class LogsController : ControllerBase
{
    public const string ProtobufContentType = "application/x-protobuf";

    private readonly LogIngestionService _logIngestionService;

    public LogsController(LogIngestionService logIngestionService)
    {
        ThrowHelper.ThrowIfNull(logIngestionService);

        _logIngestionService = logIngestionService;
    }

    [HttpPost]
    [Route("v1/logs")]
    public async Task<IResult> IngestLogsAsyncv1(CancellationToken cancellationToken)
    {
        try
        {
            if (!IsSupportedMediaType(Request.ContentType))
            {
                Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return Results.Empty;
            }

            var request = await MapOtlpLogsAsync(Request).ConfigureAwait(false);
            var response = await _logIngestionService.IngestAsync(request, cancellationToken).ConfigureAwait(false);

            return Result.Response(response);
        }
        catch (Exception ex)
        {
            return Results.InternalServerError(ex);
        }
    }

    // TODO: Move this if we will be supporting traces and metrics
    private static async Task<ExportLogsServiceRequest> MapOtlpLogsAsync(HttpRequest httpRequest)
    {
        const int MaxRequestSize = 1024 * 1024 * 4; // 4 MB

        ReadResult result = default;
        var message = new ExportLogsServiceRequest();

        try
        {
            do
            {
                result = await httpRequest.BodyReader.ReadAsync().ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    throw new OperationCanceledException("Read call was canceled.");
                }

                if (result.Buffer.Length > MaxRequestSize)
                {
                    throw new BadHttpRequestException(
                        $"The request body was larger than the max allowed of {MaxRequestSize} bytes.",
                        StatusCodes.Status400BadRequest);
                }

                if (result.IsCompleted)
                {
                    break;
                }
                else
                {
                    httpRequest.BodyReader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                }

            } while (true);

            message.MergeFrom(result.Buffer);
            return message;
        }
        finally
        {
            if (!result.Equals(default(ReadResult)))
            {
                httpRequest.BodyReader.AdvanceTo(result.Buffer.End);
            }
        }
    }

    private static bool IsSupportedMediaType(string? contentType)
    {
        if (contentType != null && MediaTypeHeaderValue.TryParse(contentType, out var mt))
        {
            return string.Equals(mt.MediaType, ProtobufContentType, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private sealed class Result
    {
        public static Result<T> Response<T>(T response) where T : IMessage => new Result<T>(response);
    }

    private sealed class Result<T> : IResult where T : IMessage
    {
        private readonly T _message;

        public Result(T message) => _message = message;

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            if (!IsSupportedMediaType(httpContext.Request.ContentType))
            {
                httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            }

            using var ms = new MemoryStream();
            _message.WriteTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            httpContext.Response.ContentType = ProtobufContentType;
            await ms.CopyToAsync(httpContext.Response.Body).ConfigureAwait(false);
        }
    }
}
