using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/api/healthz")]
    public IActionResult GetHealthz()
    {
        return Ok("Healthy");
    }
}