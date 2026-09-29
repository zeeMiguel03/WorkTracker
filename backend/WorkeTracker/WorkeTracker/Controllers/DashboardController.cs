using Application.DTOs.Dashboard;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponseDTO>> Get([FromQuery] DateOnly from, [FromQuery] DateOnly toExclusive, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";

        return Ok(await _service.GetAsync(from, toExclusive, cancellationToken));
    }
}
