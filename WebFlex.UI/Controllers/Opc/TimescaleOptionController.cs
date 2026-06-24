using Microsoft.AspNetCore.Mvc;
using WebFlex.Shared.Dtos.Opc;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.Opc;

[ApiController]
[Route("api/timescale-options")]
public class TimescaleOptionController : ControllerBase {
    private readonly TimescaleOptionService _service;

    public TimescaleOptionController(TimescaleOptionService service) {
        _service = service;
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(CancellationToken cancellationToken) {
        var result = await _service.GetTablesAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromBody] TimescaleOptionApplyRequestDto request,
        CancellationToken cancellationToken) {
        var result = await _service.ApplyAsync(request, cancellationToken);

        if (!result.Success) {
            return BadRequest(result);
        }

        return Ok(result);
    }
}