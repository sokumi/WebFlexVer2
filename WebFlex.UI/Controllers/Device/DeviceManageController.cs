using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebFlex.Shared;
using WebFlex.Shared.Exceptions;
using WebFlex.UI.Common;
using WebFlex.UI.Data;
using WebFlex.UI.Services;

namespace WebFlex.UI.Controllers.Device;

[Route("device/manage/[action]")]
public class DeviceManageController : WebFlexController {
    private readonly WebFlexDbContext _db;
    private readonly TsdReadDbContext _tsdDb;
    private readonly INewNoService _newNo;

    public DeviceManageController(
        WebFlexDbContext db,
        TsdReadDbContext tsdDb,
        INewNoService newNo) {
        _db = db;
        _tsdDb = tsdDb;
        _newNo = newNo;
    }

    [HttpGet, ActionName("types")]
    public IActionResult Types() {
        return Success("조회되었습니다.", new[] {
            new { value = "OPCUA", text = "OPC UA" },
            new { value = "MODBUS", text = "Modbus TCP" },
            new { value = "MQTT", text = "MQTT" }
        });
    }

    [HttpGet, ActionName("list")]
    public async Task<IActionResult> List() {
        try {
            var rows = await _db.Set<OpcDevice>()
                .AsNoTracking()
                .OrderBy(x => x.DEVICE_NAME)
                .Select(x => new {
                    id = x.ID,
                    deviceCode = x.ID,
                    deviceName = x.DEVICE_NAME,
                    deviceType = x.DEVICE_TYPE,
                    deviceAddress = x.DEVICE_ADDRESS,
                    port = x.PORT,
                    endpointUrl = x.ENDPOINT_URL,
                    isCollectEnabled = x.IS_COLLECTENABLED,
                    isEnabled = x.IsEnabled,
                    useSecurity = x.USESECURITY,
                    securityMode = x.SECURITYMODE,
                    securityPolicy = x.SECURITYPOLICY,
                    useAnonymous = x.USE_ANONYMOUS,
                    userName = x.USER_NAME,
                    password = x.PASSWORD,
                    publishingIntervalMs = x.PUBLISHINGINTERVALMS,
                    samplingIntervalMs = x.SAMPLINGINTERVALMS,
                    description = x.DESCRIPTION,
                    tagCount = _db.Set<OpcTag>().Count(t => t.DEVICE_ID == x.ID)
                })
                .ToListAsync();

            return Success("조회되었습니다.", rows);
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("save")]
    public async Task<IActionResult> Save([FromBody] JsonElement request) {
        var model = WebFlexModelMapper.PopulateDTOModel<OpcDevice>(request);

        if (string.IsNullOrWhiteSpace(model.DEVICE_NAME)) {
            return ErrorData("디바이스명을 입력해 주세요.");
        }

        if (string.IsNullOrWhiteSpace(model.DEVICE_TYPE)) {
            return ErrorData("디바이스 타입을 선택해 주세요.");
        }

        if (string.IsNullOrWhiteSpace(model.DEVICE_ADDRESS)) {
            return ErrorData("주소를 입력해 주세요.");
        }

        if (model.PORT == null || model.PORT <= 0) {
            return ErrorData("포트를 입력해 주세요.");
        }

        if (model.PUBLISHINGINTERVALMS == null || model.PUBLISHINGINTERVALMS <= 0) {
            return ErrorData("Publishing Interval을 입력해 주세요.");
        }

        if (model.SAMPLINGINTERVALMS == null || model.SAMPLINGINTERVALMS <= 0) {
            return ErrorData("Sampling Interval을 입력해 주세요.");
        }

        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var isCreate = string.IsNullOrWhiteSpace(model.ID);
            var now = DateTime.UtcNow;

            OpcDevice device;

            if (isCreate) {
                var ids = await _newNo.NewNosAsync("DV", 1);

                device = new OpcDevice {
                    ID = ids[0],
                    CreatedAt = now
                };

                _db.Set<OpcDevice>().Add(device);
            } else {
                device = await _db.Set<OpcDevice>()
                    .FirstOrDefaultAsync(x => x.ID == model.ID);

                if (device == null) {
                    throw new WebFlexMessageException("디바이스를 찾을 수 없습니다.");
                }
            }

            var originalId = device.ID;
            var originalCreatedAt = device.CreatedAt;

            WebFlexModelMapper.ApplyModel(device, request, NormalizeOpcDevice);

            device.ID = originalId;
            device.CreatedAt = originalCreatedAt;
            device.DEVICE_NAME = model.DEVICE_NAME.Trim();
            device.DEVICE_TYPE = NormalizeDeviceType(model.DEVICE_TYPE);
            device.DEVICE_ADDRESS = model.DEVICE_ADDRESS?.Trim();
            device.PORT = model.PORT;
            device.ENDPOINT_URL = CreateEndpointUrl(device);
            device.PUBLISHINGINTERVALMS = model.PUBLISHINGINTERVALMS;
            device.SAMPLINGINTERVALMS = model.SAMPLINGINTERVALMS;
            device.DESCRIPTION = string.IsNullOrWhiteSpace(model.DESCRIPTION) ? null : model.DESCRIPTION.Trim();
            device.USESECURITY = model.USESECURITY;
            device.SECURITYMODE = device.USESECURITY ? NormalizeNullable(model.SECURITYMODE) : null;
            device.SECURITYPOLICY = device.USESECURITY ? NormalizeNullable(model.SECURITYPOLICY) : null;
            device.USE_ANONYMOUS = model.USE_ANONYMOUS;
            device.USER_NAME = device.USE_ANONYMOUS ? null : NormalizeNullable(model.USER_NAME);
            device.PASSWORD = device.USE_ANONYMOUS ? null : model.PASSWORD;
            device.IS_COLLECTENABLED = model.IS_COLLECTENABLED;
            device.IsEnabled = model.IsEnabled;
            device.UpdatedAt = now;

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            return Success(isCreate ? "디바이스가 등록되었습니다." : "디바이스가 수정되었습니다.", new {
                id = device.ID
            });
        } catch (WebFlexMessageException ex) {
            await tran.RollbackAsync();
            return ErrorData(ex.Message);
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpPost, ActionName("delete")]
    public async Task<IActionResult> Delete([FromBody] JsonElement request) {
        await using var tran = await _db.Database.BeginTransactionAsync();

        try {
            var models = WebFlexModelMapper.PopulateDTOModel<List<OpcDevice>>(request);

            if (models.Count == 0) {
                return ErrorData("삭제할 디바이스를 선택해 주세요.");
            }

            var ids = models
                .Select(x => x.ID)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (ids.Count == 0) {
                return ErrorData("삭제할 디바이스를 선택해 주세요.");
            }

            var devices = await _db.Set<OpcDevice>()
                .Where(x => ids.Contains(x.ID))
                .ToListAsync();

            if (devices.Count == 0) {
                return ErrorData("삭제할 디바이스를 찾을 수 없습니다.");
            }

            var deviceIds = devices.Select(x => x.ID).ToList();

            var tags = await _db.Set<OpcTag>()
                .Where(x => deviceIds.Contains(x.DEVICE_ID))
                .ToListAsync();

            var tagIds = tags.Select(x => x.ID).ToList();

            if (tagIds.Count > 0) {
                await _tsdDb.Set<CurrentValue>()
                    .Where(x => tagIds.Contains(x.TAG_ID))
                    .ExecuteDeleteAsync();

                _db.Set<OpcTag>().RemoveRange(tags);
            }

            _db.Set<OpcDevice>().RemoveRange(devices);

            await _db.SaveChangesAsync();
            await tran.CommitAsync();

            return Success($"{devices.Count}개의 디바이스가 삭제되었습니다.");
        } catch (Exception ex) {
            await tran.RollbackAsync();
            return ErrorData(GetErrorMessage(ex));
        }
    }

    [HttpGet, ActionName("endpoint-preview")]
    public IActionResult EndpointPreview(string? deviceType, string? address, int port = 0) {
        try {
            var endpointUrl = CreateEndpointUrl(
                NormalizeDeviceType(deviceType),
                address,
                port,
                null
            );

            if (string.IsNullOrWhiteSpace(endpointUrl)) {
                return ErrorData("Endpoint URL을 생성할 수 없습니다.");
            }

            return Success("Endpoint URL을 생성했습니다.", new {
                endpointUrl
            });
        } catch (Exception ex) {
            return ErrorData(GetErrorMessage(ex));
        }
    }

    private static void NormalizeOpcDevice(OpcDevice device) {
        device.DEVICE_TYPE = NormalizeDeviceType(device.DEVICE_TYPE);
        device.DEVICE_ADDRESS = NormalizeNullable(device.DEVICE_ADDRESS);
        device.ENDPOINT_URL = NormalizeNullable(device.ENDPOINT_URL) ?? "";
        device.DESCRIPTION = NormalizeNullable(device.DESCRIPTION);
        device.SECURITYMODE = NormalizeNullable(device.SECURITYMODE);
        device.SECURITYPOLICY = NormalizeNullable(device.SECURITYPOLICY);
        device.USER_NAME = NormalizeNullable(device.USER_NAME);
        device.PASSWORD = NormalizeNullable(device.PASSWORD);

        if (!device.USESECURITY) {
            device.SECURITYMODE = null;
            device.SECURITYPOLICY = null;
        }

        if (device.USE_ANONYMOUS) {
            device.USER_NAME = null;
            device.PASSWORD = null;
        }
    }

    private static string CreateEndpointUrl(OpcDevice device) {
        return CreateEndpointUrl(
            device.DEVICE_TYPE,
            device.DEVICE_ADDRESS,
            device.PORT ?? 0,
            device.ENDPOINT_URL
        );
    }

    private static string CreateEndpointUrl(
        string? deviceType,
        string? address,
        int port,
        string? endpointUrl) {
        if (!string.IsNullOrWhiteSpace(endpointUrl)) {
            return endpointUrl.Trim();
        }

        var type = NormalizeDeviceType(deviceType);
        var host = address?.Trim() ?? "";

        if (type == "OPCUA" && host.Length > 0 && port > 0) {
            return $"opc.tcp://{host}:{port}";
        }

        return endpointUrl?.Trim() ?? "";
    }

    private static string NormalizeDeviceType(string? value) {
        var text = value?.Trim() ?? "";

        return text.ToUpperInvariant() switch {
            "OPC UA" => "OPCUA",
            "OPC_UA" => "OPCUA",
            "OPCUA" => "OPCUA",
            "MODBUS TCP" => "MODBUS",
            "MODBUS_TCP" => "MODBUS",
            "MODBUS" => "MODBUS",
            "MQTT" => "MQTT",
            _ => string.IsNullOrWhiteSpace(text) ? "OPCUA" : text.ToUpperInvariant()
        };
    }

    private static string? NormalizeNullable(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}