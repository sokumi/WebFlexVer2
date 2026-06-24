using System.Diagnostics;
using System.ServiceProcess;
using WebFlex.UI.Controllers.Device;
using WebFlex.UI.Models;

namespace WebFlex.UI.Services;

public sealed class WindowsServiceManager {
    private readonly IConfiguration _configuration;
    private readonly ILogger<WindowsServiceManager> _logger;

    private readonly string _serviceName;
    private readonly string _displayName;
    private readonly string _description;
    private readonly string _exePath;

    public WindowsServiceManager(
        IConfiguration configuration,
        ILogger<WindowsServiceManager> logger) {
        _configuration = configuration;
        _logger = logger;

        _serviceName = _configuration["OpcCollectorService:ServiceName"] ?? "WebFlex OPC Collector";
        _displayName = _configuration["OpcCollectorService:DisplayName"] ?? "WebFlex OPC Collector";
        _description = _configuration["OpcCollectorService:Description"] ?? "WebFlex OPC Collector";
        _exePath = _configuration["OpcCollectorService:ExePath"] ?? @"D:\WF2\OpcCollector\WebFlex.OpcCollector.exe";
    }

    public WindowsServiceStatusDto GetStatus() {
        try {
            using var service = new ServiceController(_serviceName);

            var status = service.Status;

            return new WindowsServiceStatusDto {
                ServiceName = service.ServiceName,
                DisplayName = service.DisplayName,
                Status = status.ToString(),
                Exists = true,
                ExePath = _exePath
            };
        } catch (InvalidOperationException ex) {
            return new WindowsServiceStatusDto {
                ServiceName = _serviceName,
                DisplayName = _displayName,
                Status = "NotInstalled",
                Exists = false,
                ExePath = _exePath,
                Error = ex.Message
            };
        } catch (Exception ex) {
            _logger.LogError(ex, "Windows Service 상태 조회 실패 | ServiceName={ServiceName}", _serviceName);

            return new WindowsServiceStatusDto {
                ServiceName = _serviceName,
                DisplayName = _displayName,
                Status = "Error",
                Exists = false,
                ExePath = _exePath,
                Error = ex.ToString()
            };
        }
    }

    public async Task<WindowsServiceCommandResultDto> InstallAsync() {
        if (GetStatus().Exists) {
            return Success("이미 서비스가 등록되어 있습니다.");
        }

        if (string.IsNullOrWhiteSpace(_exePath)) {
            return Fail("OpcCollectorService:ExePath 설정이 비어 있습니다.");
        }

        if (!File.Exists(_exePath)) {
            return Fail($"서비스 실행 파일이 없습니다. Path={_exePath}");
        }

        var binPath = $"\"{_exePath}\"";

        var createResult = await RunScAsync(
            $"create \"{_serviceName}\" binPath= {binPath} DisplayName= \"{_displayName}\" start= auto"
        );

        if (!createResult.Success) {
            return createResult;
        }

        var descResult = await RunScAsync(
            $"description \"{_serviceName}\" \"{_description}\""
        );

        if (!descResult.Success) {
            return descResult;
        }

        return Success("서비스가 등록되었습니다.", createResult.Output);
    }

    public Task<WindowsServiceCommandResultDto> StartAsync() {
        try {
            if (!GetStatus().Exists) {
                return Task.FromResult(Fail("서비스가 등록되어 있지 않습니다."));
            }

            using var service = new ServiceController(_serviceName);

            if (service.Status == ServiceControllerStatus.Running) {
                return Task.FromResult(Success("이미 실행 중입니다."));
            }

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

            return Task.FromResult(Success("서비스가 시작되었습니다."));
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 시작 실패 | ServiceName={ServiceName}", _serviceName);
            return Task.FromResult(Fail(ex.Message, ex.ToString()));
        }
    }

    public Task<WindowsServiceCommandResultDto> StopAsync() {
        try {
            if (!GetStatus().Exists) {
                return Task.FromResult(Fail("서비스가 등록되어 있지 않습니다."));
            }

            using var service = new ServiceController(_serviceName);

            if (service.Status == ServiceControllerStatus.Stopped) {
                return Task.FromResult(Success("이미 중지 상태입니다."));
            }

            if (!service.CanStop) {
                return Task.FromResult(Fail("현재 서비스는 중지할 수 없는 상태입니다."));
            }

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

            return Task.FromResult(Success("서비스가 중지되었습니다."));
        } catch (Exception ex) {
            _logger.LogError(ex, "서비스 중지 실패 | ServiceName={ServiceName}", _serviceName);
            return Task.FromResult(Fail(ex.Message, ex.ToString()));
        }
    }

    public async Task<WindowsServiceCommandResultDto> RestartAsync() {
        var stopResult = await StopAsync();

        if (!stopResult.Success &&
            !stopResult.Message.Contains("이미 중지", StringComparison.OrdinalIgnoreCase)) {
            return stopResult;
        }

        await Task.Delay(1000);

        return await StartAsync();
    }

    public async Task<WindowsServiceCommandResultDto> UninstallAsync() {
        if (!GetStatus().Exists) {
            return Success("이미 서비스가 등록되어 있지 않습니다.");
        }

        var status = GetStatus();

        if (!string.Equals(status.Status, ServiceControllerStatus.Stopped.ToString(), StringComparison.OrdinalIgnoreCase)) {
            var stopResult = await StopAsync();

            if (!stopResult.Success) {
                return stopResult;
            }
        }

        var deleteResult = await RunScAsync($"delete \"{_serviceName}\"");

        if (!deleteResult.Success) {
            return deleteResult;
        }

        return Success("서비스가 삭제되었습니다.", deleteResult.Output);
    }

    private async Task<WindowsServiceCommandResultDto> RunScAsync(string arguments) {
        try {
            var psi = new ProcessStartInfo {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null) {
                return Fail("sc.exe 실행에 실패했습니다.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0) {
                _logger.LogError(
                    "sc.exe 실패 | Arguments={Arguments} | ExitCode={ExitCode} | Output={Output} | Error={Error}",
                    arguments,
                    process.ExitCode,
                    output,
                    error
                );

                return Fail($"sc.exe 실패. ExitCode={process.ExitCode}", error, output);
            }

            return Success("sc.exe 실행 성공", output);
        } catch (Exception ex) {
            _logger.LogError(ex, "sc.exe 실행 예외 | Arguments={Arguments}", arguments);
            return Fail(ex.Message, ex.ToString());
        }
    }

    private static WindowsServiceCommandResultDto Success(string message, string? output = null) {
        return new WindowsServiceCommandResultDto {
            Success = true,
            Message = message,
            Output = output
        };
    }

    private static WindowsServiceCommandResultDto Fail(string message, string? error = null, string? output = null) {
        return new WindowsServiceCommandResultDto {
            Success = false,
            Message = message,
            Error = error,
            Output = output
        };
    }
}