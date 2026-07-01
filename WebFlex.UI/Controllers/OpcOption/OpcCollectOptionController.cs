using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared;
using WebFlex.Shared.Dtos.Opc;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Opc;

[Route("api/opc-collect-options")]
public sealed class OpcCollectOptionController : Controller {
    private const string OptionCode = "DEFAULT";

    private readonly WebFlexDbContext _db;

    public OpcCollectOptionController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) {
        var row = await _db.Set<OpcCollectOption>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OPTION_CODE == OptionCode && x.IsEnabled, cancellationToken);

        if (row == null || string.IsNullOrWhiteSpace(row.OPTION_JSON)) {
            return NotFound(new {
                success = false,
                message = "OPC 수집 옵션 기본 데이터가 없습니다. 마이그레이션 seed 데이터를 확인하세요."
            });
        }

        var data = JsonSerializer.Deserialize<OpcCollectorRuntimeOptionsDto>(
            row.OPTION_JSON,
            JsonOptions()
        );

        return Json(data);
    }

    [HttpPost("")]
    public async Task<IActionResult> Save(
        [FromBody] OpcCollectorRuntimeOptionsDto request,
        CancellationToken cancellationToken) {
        var row = await _db.Set<OpcCollectOption>()
            .FirstOrDefaultAsync(x => x.OPTION_CODE == OptionCode, cancellationToken);

        var json = JsonSerializer.Serialize(request, JsonOptions());

        if (row == null) {
            row = new OpcCollectOption {
                ID = "COLLECT_OPTION_DEFAULT",
                OPTION_CODE = OptionCode,
                OPTION_NAME = "기본 OPC 수집 옵션",
                OPTION_JSON = json,
                CONFIGURED_OPTION_NAMES = string.Join(",", GetConfiguredOptionNames()),
                DESCRIPTION = "OPC Collector 기본 수집 옵션",
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<OpcCollectOption>().Add(row);
        } else {
            row.OPTION_NAME = "기본 OPC 수집 옵션";
            row.OPTION_JSON = json;
            row.CONFIGURED_OPTION_NAMES = string.Join(",", GetConfiguredOptionNames());
            row.DESCRIPTION = "OPC Collector 기본 수집 옵션";
            row.IsEnabled = true;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Json(new {
            success = true,
            message = "OPC Collector 옵션이 저장되었습니다. Windows Service 제어 화면에서 서비스 재시작 후 적용됩니다.",
            data = request
        });
    }

    private static string[] GetConfiguredOptionNames() {
        return typeof(OpcCollectorRuntimeOptionsDto)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();
    }

    private static JsonSerializerOptions JsonOptions() {
        return new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }
}