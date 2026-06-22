using System.Text.Json;
using System.Text.RegularExpressions;
using Npgsql;
using WebFlex.Shared.Dtos.Opc;

namespace WebFlex.UI.Services.Timescale;

public class TimescaleOptionService {
    private readonly IConfiguration _configuration;
    private readonly ILogger<TimescaleOptionService> _logger;

    public TimescaleOptionService(
        IConfiguration configuration,
        ILogger<TimescaleOptionService> logger) {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<TimescaleOptionTableDto>> GetTablesAsync(
        CancellationToken cancellationToken) {
        var connectionString = GetConnectionString();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = await GetBaseTablesAsync(connection, cancellationToken);
        var hypertables = await GetHypertablesAsync(connection, cancellationToken);
        var dimensions = await GetDimensionsAsync(connection, cancellationToken);
        var jobs = await GetJobsAsync(connection, cancellationToken);
        var compressionSettings = await GetCompressionSettingsAsync(connection, cancellationToken);

        foreach (var table in tables) {
            var key = MakeKey(table.SchemaName, table.TableName);

            if (hypertables.TryGetValue(key, out var hypertable)) {
                table.IsHypertable = true;
                table.ChunkCount = hypertable.ChunkCount;
                table.CompressionEnabled = hypertable.CompressionEnabled;
            }

            if (dimensions.TryGetValue(key, out var dimension)) {
                table.TimeColumnName = dimension.TimeColumnName;
                table.ChunkTimeInterval = dimension.ChunkTimeInterval;
            }

            if (jobs.TryGetValue($"{key}:retention", out var retentionJob)) {
                table.RetentionEnabled = true;
                table.RetentionDropAfter = retentionJob.DropAfter;
                table.RetentionScheduleInterval = retentionJob.ScheduleInterval;
            }

            if (jobs.TryGetValue($"{key}:compression", out var compressionJob)) {
                table.CompressionPolicyEnabled = true;
                table.CompressionAfter = compressionJob.CompressAfter;
                table.CompressionScheduleInterval = compressionJob.ScheduleInterval;
            }

            if (compressionSettings.TryGetValue(key, out var compressionSetting)) {
                table.CompressionSegmentBy = compressionSetting.SegmentBy;
                table.CompressionOrderBy = compressionSetting.OrderBy;
            }

            await FillSizeAsync(connection, table, cancellationToken);
        }

        return tables
            .OrderByDescending(x => x.IsHypertable)
            .ThenBy(x => x.SchemaName)
            .ThenBy(x => x.TableName)
            .ToList();
    }

    public async Task<TimescaleOptionApplyResultDto> ApplyAsync(
        TimescaleOptionApplyRequestDto request,
        CancellationToken cancellationToken) {
        var result = new TimescaleOptionApplyResultDto();

        try {
            ValidateIdentifier(request.SchemaName, nameof(request.SchemaName));
            ValidateIdentifier(request.TableName, nameof(request.TableName));

            if (string.IsNullOrWhiteSpace(request.TableName)) {
                result.Success = false;
                result.Message = "테이블명이 비어 있습니다.";
                return result;
            }

            var connectionString = GetConnectionString();

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var fullName = QuoteFullName(request.SchemaName, request.TableName);
            var regclassName = $"{request.SchemaName}.{request.TableName}";

            if (!await IsHypertableAsync(connection, request.SchemaName, request.TableName, cancellationToken)) {
                result.Success = false;
                result.Message = $"{regclassName} 테이블은 hypertable이 아닙니다.";
                return result;
            }

            if (!string.IsNullOrWhiteSpace(request.ChunkTimeInterval)) {
                await ExecuteAsync(
                    connection,
                    "SELECT set_chunk_time_interval(@tableName::regclass, @chunkInterval::interval);",
                    new Dictionary<string, object?> {
                        ["tableName"] = regclassName,
                        ["chunkInterval"] = request.ChunkTimeInterval
                    },
                    cancellationToken);

                result.Logs.Add($"chunk_time_interval 적용: {request.ChunkTimeInterval}");
            }

            await ApplyRetentionAsync(
                connection,
                regclassName,
                request,
                result.Logs,
                cancellationToken);

            await ApplyCompressionAsync(
                connection,
                fullName,
                regclassName,
                request,
                result.Logs,
                cancellationToken);

            result.Success = true;
            result.Message = "TimescaleDB 옵션 적용이 완료되었습니다.";
            return result;
        } catch (Exception ex) {
            _logger.LogError(ex, "TimescaleDB 옵션 적용 실패");

            result.Success = false;
            result.Message = ex.Message;
            result.Logs.Add(ex.Message);

            return result;
        }
    }

    private async Task ApplyRetentionAsync(
        NpgsqlConnection connection,
        string regclassName,
        TimescaleOptionApplyRequestDto request,
        List<string> logs,
        CancellationToken cancellationToken) {
        await ExecuteAsync(
            connection,
            "SELECT remove_retention_policy(@tableName::regclass, if_exists => true);",
            new Dictionary<string, object?> {
                ["tableName"] = regclassName
            },
            cancellationToken);

        logs.Add("기존 retention policy 제거");

        if (!request.RetentionEnabled) {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.RetentionDropAfter)) {
            throw new InvalidOperationException("Retention 사용 시 drop_after 값이 필요합니다.");
        }

        if (string.IsNullOrWhiteSpace(request.RetentionScheduleInterval)) {
            await ExecuteAsync(
                connection,
                "SELECT add_retention_policy(@tableName::regclass, drop_after => @dropAfter::interval, if_not_exists => true);",
                new Dictionary<string, object?> {
                    ["tableName"] = regclassName,
                    ["dropAfter"] = request.RetentionDropAfter
                },
                cancellationToken);
        } else {
            await ExecuteAsync(
                connection,
                "SELECT add_retention_policy(@tableName::regclass, drop_after => @dropAfter::interval, schedule_interval => @scheduleInterval::interval, if_not_exists => true);",
                new Dictionary<string, object?> {
                    ["tableName"] = regclassName,
                    ["dropAfter"] = request.RetentionDropAfter,
                    ["scheduleInterval"] = request.RetentionScheduleInterval
                },
                cancellationToken);
        }

        logs.Add($"retention policy 적용: drop_after={request.RetentionDropAfter}, schedule_interval={request.RetentionScheduleInterval}");
    }

    private async Task ApplyCompressionAsync(
        NpgsqlConnection connection,
        string fullName,
        string regclassName,
        TimescaleOptionApplyRequestDto request,
        List<string> logs,
        CancellationToken cancellationToken) {
        try {
            await ExecuteAsync(
                connection,
                "SELECT remove_compression_policy(@tableName::regclass, if_exists => true);",
                new Dictionary<string, object?> {
                    ["tableName"] = regclassName
                },
                cancellationToken);

            logs.Add("기존 compression policy 제거");
        } catch (Exception ex) {
            logs.Add($"기존 compression policy 제거 실패 또는 미지원: {ex.Message}");
        }

        if (!request.CompressionEnabled) {
            await ExecuteRawAsync(
                connection,
                $"ALTER TABLE {fullName} SET (timescaledb.compress = false);",
                cancellationToken);

            logs.Add("compression 비활성화");
            return;
        }

        var segmentBy = string.IsNullOrWhiteSpace(request.CompressionSegmentBy)
            ? ""
            : request.CompressionSegmentBy.Trim();

        var orderBy = string.IsNullOrWhiteSpace(request.CompressionOrderBy)
            ? "time DESC"
            : request.CompressionOrderBy.Trim();

        var setOptions = new List<string> {
            "timescaledb.compress = true",
            $"timescaledb.compress_orderby = '{EscapeSqlLiteral(orderBy)}'"
        };

        if (!string.IsNullOrWhiteSpace(segmentBy)) {
            setOptions.Add($"timescaledb.compress_segmentby = '{EscapeSqlLiteral(segmentBy)}'");
        }

        await ExecuteRawAsync(
            connection,
            $"ALTER TABLE {fullName} SET ({string.Join(", ", setOptions)});",
            cancellationToken);

        logs.Add($"compression 활성화: segmentby={segmentBy}, orderby={orderBy}");

        if (!string.IsNullOrWhiteSpace(request.CompressionAfter)) {
            if (string.IsNullOrWhiteSpace(request.CompressionScheduleInterval)) {
                await ExecuteAsync(
                    connection,
                    "SELECT add_compression_policy(@tableName::regclass, @compressAfter::interval, if_not_exists => true);",
                    new Dictionary<string, object?> {
                        ["tableName"] = regclassName,
                        ["compressAfter"] = request.CompressionAfter
                    },
                    cancellationToken);
            } else {
                await ExecuteAsync(
                    connection,
                    "SELECT add_compression_policy(@tableName::regclass, @compressAfter::interval, if_not_exists => true, schedule_interval => @scheduleInterval::interval);",
                    new Dictionary<string, object?> {
                        ["tableName"] = regclassName,
                        ["compressAfter"] = request.CompressionAfter,
                        ["scheduleInterval"] = request.CompressionScheduleInterval
                    },
                    cancellationToken);
            }

            logs.Add($"compression policy 적용: compress_after={request.CompressionAfter}, schedule_interval={request.CompressionScheduleInterval}");
        }
    }

    private async Task<List<TimescaleOptionTableDto>> GetBaseTablesAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT
                schemaname,
                tablename
            FROM pg_tables
            WHERE schemaname = 'public'
            ORDER BY tablename;
            """;

        var result = new List<TimescaleOptionTableDto>();

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken)) {
            result.Add(new TimescaleOptionTableDto {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1)
            });
        }

        return result;
    }

    private async Task<Dictionary<string, HypertableInfo>> GetHypertablesAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT
                hypertable_schema,
                hypertable_name,
                COALESCE(num_chunks, 0) AS num_chunks,
                COALESCE(compression_enabled, false) AS compression_enabled
            FROM timescaledb_information.hypertables;
            """;

        var result = new Dictionary<string, HypertableInfo>();

        try {
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken)) {
                var schema = reader.GetString(0);
                var table = reader.GetString(1);

                result[MakeKey(schema, table)] = new HypertableInfo {
                    ChunkCount = reader.GetInt32(2),
                    CompressionEnabled = reader.GetBoolean(3)
                };
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "timescaledb_information.hypertables 조회 실패");
        }

        return result;
    }

    private async Task<Dictionary<string, DimensionInfo>> GetDimensionsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT
                hypertable_schema,
                hypertable_name,
                column_name,
                time_interval
            FROM timescaledb_information.dimensions
            WHERE dimension_type = 'Time';
            """;

        var result = new Dictionary<string, DimensionInfo>();

        try {
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken)) {
                var schema = reader.GetString(0);
                var table = reader.GetString(1);

                result[MakeKey(schema, table)] = new DimensionInfo {
                    TimeColumnName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ChunkTimeInterval = reader.IsDBNull(3) ? null : reader.GetString(3)
                };
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "timescaledb_information.dimensions 조회 실패");
        }

        return result;
    }

    private async Task<Dictionary<string, JobInfo>> GetJobsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT
                hypertable_schema,
                hypertable_name,
                proc_name,
                schedule_interval::text,
                config::text
            FROM timescaledb_information.jobs
            WHERE hypertable_schema IS NOT NULL
              AND hypertable_name IS NOT NULL;
            """;

        var result = new Dictionary<string, JobInfo>();

        try {
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken)) {
                var schema = reader.GetString(0);
                var table = reader.GetString(1);
                var procName = reader.GetString(2);
                var scheduleInterval = reader.IsDBNull(3) ? null : reader.GetString(3);
                var configText = reader.IsDBNull(4) ? "" : reader.GetString(4);

                var key = MakeKey(schema, table);
                var jobInfo = new JobInfo {
                    ScheduleInterval = scheduleInterval,
                    DropAfter = GetJsonValue(configText, "drop_after"),
                    CompressAfter = GetJsonValue(configText, "compress_after")
                };

                if (procName.Contains("retention", StringComparison.OrdinalIgnoreCase)) {
                    result[$"{key}:retention"] = jobInfo;
                }

                if (procName.Contains("compression", StringComparison.OrdinalIgnoreCase) ||
                    procName.Contains("columnstore", StringComparison.OrdinalIgnoreCase)) {
                    result[$"{key}:compression"] = jobInfo;
                }
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "timescaledb_information.jobs 조회 실패");
        }

        return result;
    }

    private async Task<Dictionary<string, CompressionInfo>> GetCompressionSettingsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT
                hypertable_schema,
                hypertable_name,
                attname,
                segmentby_column_index,
                orderby_column_index,
                orderby_asc,
                orderby_nullsfirst
            FROM timescaledb_information.compression_settings;
            """;

        var rows = new List<CompressionSettingRow>();

        try {
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken)) {
                rows.Add(new CompressionSettingRow {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1),
                    ColumnName = reader.GetString(2),
                    SegmentIndex = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    OrderIndex = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    OrderAsc = reader.IsDBNull(5) ? null : reader.GetBoolean(5),
                    OrderNullsFirst = reader.IsDBNull(6) ? null : reader.GetBoolean(6)
                });
            }
        } catch (Exception ex) {
            _logger.LogWarning(ex, "timescaledb_information.compression_settings 조회 실패");
        }

        return rows
            .GroupBy(x => MakeKey(x.SchemaName, x.TableName))
            .ToDictionary(
                x => x.Key,
                x => new CompressionInfo {
                    SegmentBy = string.Join(",",
                        x.Where(y => y.SegmentIndex != null)
                            .OrderBy(y => y.SegmentIndex)
                            .Select(y => y.ColumnName)),
                    OrderBy = string.Join(",",
                        x.Where(y => y.OrderIndex != null)
                            .OrderBy(y => y.OrderIndex)
                            .Select(y => $"{y.ColumnName} {(y.OrderAsc == true ? "ASC" : "DESC")}"))
                });
    }

    private async Task FillSizeAsync(
        NpgsqlConnection connection,
        TimescaleOptionTableDto table,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT
                pg_size_pretty(pg_total_relation_size(@tableName::regclass)) AS total_size,
                pg_size_pretty(pg_relation_size(@tableName::regclass)) AS table_size,
                pg_size_pretty(pg_indexes_size(@tableName::regclass)) AS index_size;
            """;

        try {
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tableName", $"{table.SchemaName}.{table.TableName}");

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken)) {
                table.TotalSize = reader.IsDBNull(0) ? null : reader.GetString(0);
                table.TableSize = reader.IsDBNull(1) ? null : reader.GetString(1);
                table.IndexSize = reader.IsDBNull(2) ? null : reader.GetString(2);
            }
        } catch (Exception ex) {
            table.LastError = ex.Message;
        }
    }

    private async Task<bool> IsHypertableAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken) {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM timescaledb_information.hypertables
                WHERE hypertable_schema = @schemaName
                  AND hypertable_name = @tableName
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("schemaName", schemaName);
        command.Parameters.AddWithValue("tableName", tableName);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is bool value && value;
    }

    private async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken) {
        await using var command = new NpgsqlCommand(sql, connection);

        foreach (var parameter in parameters) {
            command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ExecuteRawAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken) {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string GetConnectionString() {
        var connectionString =
            _configuration.GetConnectionString("WebFlexTsd")
            ?? _configuration.GetConnectionString("Tsd")
            ?? _configuration["ConnectionStrings:WebFlexTsd"];

        if (string.IsNullOrWhiteSpace(connectionString)) {
            throw new InvalidOperationException("WebFlexTsd 연결 문자열이 없습니다.");
        }

        return connectionString;
    }

    private static string MakeKey(string schemaName, string tableName) {
        return $"{schemaName}.{tableName}".ToLowerInvariant();
    }

    private static string QuoteFullName(string schemaName, string tableName) {
        return $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";
    }

    private static string QuoteIdentifier(string value) {
        ValidateIdentifier(value, nameof(value));

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void ValidateIdentifier(string value, string name) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"{name} 값이 비어 있습니다.");
        }

        if (!Regex.IsMatch(value, "^[a-zA-Z_][a-zA-Z0-9_]*$")) {
            throw new ArgumentException($"{name} 값이 올바르지 않습니다. value={value}");
        }
    }

    private static string EscapeSqlLiteral(string value) {
        return value.Replace("'", "''");
    }

    private static string? GetJsonValue(string json, string key) {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.TryGetProperty(key, out var value)) {
                return value.ToString();
            }
        } catch {
            return null;
        }

        return null;
    }

    private class HypertableInfo {
        public int ChunkCount { get; set; }
        public bool CompressionEnabled { get; set; }
    }

    private class DimensionInfo {
        public string? TimeColumnName { get; set; }
        public string? ChunkTimeInterval { get; set; }
    }

    private class JobInfo {
        public string? ScheduleInterval { get; set; }
        public string? DropAfter { get; set; }
        public string? CompressAfter { get; set; }
    }

    private class CompressionInfo {
        public string? SegmentBy { get; set; }
        public string? OrderBy { get; set; }
    }

    private class CompressionSettingRow {
        public string SchemaName { get; set; } = "";
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public int? SegmentIndex { get; set; }
        public int? OrderIndex { get; set; }
        public bool? OrderAsc { get; set; }
        public bool? OrderNullsFirst { get; set; }
    }
}