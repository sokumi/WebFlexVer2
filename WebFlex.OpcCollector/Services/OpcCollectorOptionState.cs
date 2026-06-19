using Microsoft.Extensions.Options;
using WebFlex.OpcCollector.Options;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcCollectorOptionState {
    private readonly object _lock = new();
    private OpcCollectorRuntimeOptionsDto _current;

    public OpcCollectorOptionState(IOptions<OpcCollectorOptions> options) {
        _current = OpcCollectorRuntimeOptionsDto.Clone(options.Value);
        Normalize(_current);
    }

    public OpcCollectorRuntimeOptionsDto Current {
        get {
            lock (_lock) {
                return OpcCollectorRuntimeOptionsDto.Clone(_current);
            }
        }
    }

    public OpcCollectorRuntimeOptionsDto Update(OpcCollectorRuntimeOptionsDto request) {
        var next = OpcCollectorRuntimeOptionsDto.Clone(request);
        Normalize(next);

        lock (_lock) {
            _current = next;
            return OpcCollectorRuntimeOptionsDto.Clone(_current);
        }
    }

    private static void Normalize(OpcCollectorRuntimeOptionsDto options) {
        options.ReloadIntervalSeconds = Math.Max(1, options.ReloadIntervalSeconds);
        options.SaveIntervalMilliseconds = Math.Max(100, options.SaveIntervalMilliseconds);
        options.FlushIntervalMilliseconds = Math.Max(50, options.FlushIntervalMilliseconds);
        options.MaxBatchSize = Math.Max(1, options.MaxBatchSize);
        options.WriterLogIntervalSeconds = Math.Max(1, options.WriterLogIntervalSeconds);

        options.DefaultPublishingIntervalMs = Math.Max(100, options.DefaultPublishingIntervalMs);
        options.DefaultSamplingIntervalMs = Math.Max(100, options.DefaultSamplingIntervalMs);
        options.DefaultQueueSize = Math.Max(1, options.DefaultQueueSize);

        options.SubscriptionPriority = Math.Min((byte)255, options.SubscriptionPriority);

        options.MinimumCertificateKeySize = Math.Max(1024, options.MinimumCertificateKeySize);
        options.CertificateStoreRootPath = string.IsNullOrWhiteSpace(options.CertificateStoreRootPath)
            ? "pki"
            : options.CertificateStoreRootPath.Trim();

        options.OperationTimeoutMilliseconds = options.OperationTimeoutMilliseconds == -1
            ? -1
            : Math.Max(1000, options.OperationTimeoutMilliseconds);

        options.MaxStringLength = options.MaxStringLength <= 0 ? int.MaxValue : options.MaxStringLength;
        options.MaxByteStringLength = options.MaxByteStringLength <= 0 ? int.MaxValue : options.MaxByteStringLength;
        options.MaxArrayLength = options.MaxArrayLength <= 0 ? 65535 : options.MaxArrayLength;
        options.MaxMessageSize = options.MaxMessageSize <= 0 ? 419430400 : options.MaxMessageSize;
        options.MaxBufferSize = options.MaxBufferSize <= 0 ? 65535 : options.MaxBufferSize;
    }
}