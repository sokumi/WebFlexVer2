using Opc.Ua;
using Opc.Ua.Client;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.OpcCollector.Services;

public class OpcHistoryReadService {
    private readonly OpcUaSessionFactory _sessionFactory;
    private readonly OpcClientOptionState _optionState;
    private readonly ILogger<OpcHistoryReadService> _logger;

    public OpcHistoryReadService(
        OpcUaSessionFactory sessionFactory,
        OpcClientOptionState optionState,
        ILogger<OpcHistoryReadService> logger) {
        _sessionFactory = sessionFactory;
        _optionState = optionState;
        _logger = logger;
    }

    public async Task<OpcHistoryReadResponseDto> ReadAsync(
        OpcHistoryReadRequestDto request,
        CancellationToken cancellationToken) {
        var response = new OpcHistoryReadResponseDto {
            EndpointUrl = request.EndpointUrl,
            NodeId = request.NodeId
        };

        if (string.IsNullOrWhiteSpace(request.EndpointUrl)) {
            response.Success = false;
            response.Message = "EndpointUrl이 비어 있습니다.";
            return response;
        }

        if (string.IsNullOrWhiteSpace(request.NodeId)) {
            response.Success = false;
            response.Message = "NodeId가 비어 있습니다.";
            return response;
        }

        if (request.StartTime >= request.EndTime) {
            response.Success = false;
            response.Message = "시작 시간이 종료 시간보다 크거나 같습니다.";
            return response;
        }

        Session? session = null;

        try {
            var target = new OpcCollectTargetDto {
                DeviceId = "HISTORY_READ",
                DeviceCode = "HISTORY_READ",
                DeviceName = "HistoryRead",
                EndpointUrl = request.EndpointUrl,
                UseSecurity = request.UseSecurity,
                UseAnonymous = request.UseAnonymous,
                UserName = request.UserName,
                Password = request.Password,
                PublishingIntervalMs = 1000,
                SamplingIntervalMs = 1000,
                QueueSize = 1,
                Tags = new List<OpcCollectTargetTagDto>()
            };

            session = await _sessionFactory.CreateSessionAsync(target, cancellationToken);

            var values = await ReadRawModifiedAsync(session, request, cancellationToken);

            response.Success = true;
            response.Message = $"History 데이터 {values.Count}건 조회 완료";
            response.Values = values;
            return response;
        } catch (Exception ex) {
            _logger.LogError(
                ex,
                "OPC HistoryRead 실패 | Endpoint={EndpointUrl} | NodeId={NodeId}",
                request.EndpointUrl,
                request.NodeId);

            response.Success = false;
            response.Message = ex.Message;
            return response;
        } finally {
            if (session != null) {
                try {
                    await session.CloseAsync();
                    session.Dispose();
                } catch {
                    // ignore
                }
            }
        }
    }

    private async Task<List<OpcHistoryReadValueDto>> ReadRawModifiedAsync(
        Session session,
        OpcHistoryReadRequestDto request,
        CancellationToken cancellationToken) {
        var result = new List<OpcHistoryReadValueDto>();

        var nodeId = NodeId.Parse(request.NodeId);

        var details = new ReadRawModifiedDetails {
            IsReadModified = request.ReadModified,
            StartTime = ToUtc(request.StartTime),
            EndTime = ToUtc(request.EndTime),
            NumValuesPerNode = request.NumValuesPerNode,
            ReturnBounds = request.ReturnBounds
        };

        var nodesToRead = new HistoryReadValueIdCollection {
            new HistoryReadValueId {
                NodeId = nodeId
            }
        };

        var timestampsToReturn = ResolveTimestampsToReturn(request.TimestampsToReturn);

        HistoryReadResultCollection historyResults;
        DiagnosticInfoCollection diagnosticInfos;

        session.HistoryRead(
            null,
            new ExtensionObject(details),
            timestampsToReturn,
            request.ReleaseContinuationPoints,
            nodesToRead,
            out historyResults,
            out diagnosticInfos);

        ClientBase.ValidateResponse(historyResults, nodesToRead);
        ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

        if (historyResults.Count == 0) {
            return result;
        }

        var historyResult = historyResults[0];

        AddHistoryValues(result, historyResult);

        var continuationPoint = historyResult.ContinuationPoint;
        var continuationReadCount = 0;

        while (continuationPoint != null &&
               continuationPoint.Length > 0 &&
               continuationReadCount < request.MaxContinuationReads) {
            continuationReadCount++;

            var continuationNodes = new HistoryReadValueIdCollection {
                new HistoryReadValueId {
                    NodeId = nodeId,
                    ContinuationPoint = continuationPoint
                }
            };

            session.HistoryRead(
                null,
                new ExtensionObject(details),
                timestampsToReturn,
                false,
                continuationNodes,
                out historyResults,
                out diagnosticInfos);

            ClientBase.ValidateResponse(historyResults, continuationNodes);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationNodes);

            if (historyResults.Count == 0) {
                break;
            }

            historyResult = historyResults[0];

            AddHistoryValues(result, historyResult);

            continuationPoint = historyResult.ContinuationPoint;
        }

        return await Task.FromResult(result);
    }

    private static void AddHistoryValues(
        List<OpcHistoryReadValueDto> result,
        HistoryReadResult historyResult) {
        if (StatusCode.IsBad(historyResult.StatusCode)) {
            throw new ServiceResultException(historyResult.StatusCode);
        }

        if (historyResult.HistoryData?.Body is not HistoryData historyData) {
            return;
        }

        foreach (var value in historyData.DataValues) {
            result.Add(new OpcHistoryReadValueDto {
                SourceTimestamp = value.SourceTimestamp == DateTime.MinValue
                    ? null
                    : DateTime.SpecifyKind(value.SourceTimestamp, DateTimeKind.Utc),
                ServerTimestamp = value.ServerTimestamp == DateTime.MinValue
                    ? null
                    : DateTime.SpecifyKind(value.ServerTimestamp, DateTimeKind.Utc),
                Value = value.Value?.ToString(),
                StatusCode = value.StatusCode.ToString()
            });
        }
    }

    private static TimestampsToReturn ResolveTimestampsToReturn(string value) {
        return value switch {
            "Source" => TimestampsToReturn.Source,
            "Server" => TimestampsToReturn.Server,
            "Neither" => TimestampsToReturn.Neither,
            _ => TimestampsToReturn.Both
        };
    }

    private static DateTime ToUtc(DateTime value) {
        if (value.Kind == DateTimeKind.Utc) {
            return value;
        }

        if (value.Kind == DateTimeKind.Local) {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
    }
}