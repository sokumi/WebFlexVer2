using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFlex.Shared.Dtos.Opc;
using WebFlex.Shared.Entities.Opc;
using WebFlex.UI.Data;

namespace WebFlex.UI.Controllers.Opc;

[ApiController]
[Route("api/opc-client-options")]
public class OpcClientOptionController : ControllerBase {
    private const string OptionCode = "DEFAULT";

    private readonly WebFlexDbContext _db;

    public OpcClientOptionController(WebFlexDbContext db) {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) {
        var row = await _db.OpcClientOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OptionCode == OptionCode && x.IsEnabled, cancellationToken);

        var result = new OpcClientOptionViewDto {
            Options = new OpcClientOptionDto(),
            UsedOptionNames = OpcClientOptionUsedNames.GetAll(),
            HasSavedOptions = row != null
        };

        if (row != null) {
            result.Options = JsonSerializer.Deserialize<OpcClientOptionDto>(
                row.OptionJson,
                JsonOptions()) ?? new OpcClientOptionDto();

            result.ConfiguredOptionNames = SplitNames(row.ConfiguredOptionNames);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Save(
        [FromBody] OpcClientOptionViewDto request,
        CancellationToken cancellationToken) {
        var now = DateTime.UtcNow;

        var row = await _db.OpcClientOptions
            .FirstOrDefaultAsync(x => x.OptionCode == OptionCode, cancellationToken);

        var optionJson = JsonSerializer.Serialize(request.Options, JsonOptions());

        var configuredNames = request.ConfiguredOptionNames.Count > 0
            ? string.Join(",", request.ConfiguredOptionNames.Distinct())
            : string.Join(",", OpcClientOptionUsedNames.GetAll());

        if (row == null) {
            row = new OpcClientOption {
                OptionCode = OptionCode,
                OptionName = "OPC Client Default Options",
                CreatedAt = now
            };

            _db.OpcClientOptions.Add(row);
        }

        row.OptionJson = optionJson;
        row.ConfiguredOptionNames = configuredNames;
        row.Description = "OPC1030 화면에서 저장한 OPC Client 옵션";
        row.IsEnabled = true;
        row.UpdatedAt = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new {
            success = true,
            message = "OPC Client 옵션이 저장되었습니다. OPC Collector 서비스 재시작 후 적용됩니다."
        });
    }

    private static List<string> SplitNames(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return new List<string>();

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();
    }

    private static JsonSerializerOptions JsonOptions() {
        return new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }
}