using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Loggle.Web.Controllers;

[ApiController]
public class LogsController
{
    [AllowAnonymous]
    [HttpPost]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/v1/logs/ingest")]
    public async Task<IResult> IngestLogsAsync([FromBody] object log)
    {
        return Results.Ok(log);
    }
}
