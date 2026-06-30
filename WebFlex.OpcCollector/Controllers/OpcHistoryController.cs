using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebFlex.OpcCollector.Services;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Controllers;

[Authorize]
[ApiController]
[Route("api/opc-history")]
public class OpcHistoryController : ControllerBase {
    private readonly OpcHistoryReadService _historyReadService;

    public OpcHistoryController(OpcHistoryReadService historyReadService) {
        _historyReadService = historyReadService;
    }

    [HttpPost("read")]
    public async Task<IActionResult> Read(
        [FromBody] OpcHistoryReadRequestDto request,
        CancellationToken cancellationToken) {
        var result = await _historyReadService.ReadAsync(request, cancellationToken);

        if (!result.Success) {
            return BadRequest(result);
        }

        return Ok(result);
    }
}