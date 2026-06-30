using System.Collections.Concurrent;
using System.Globalization;
using DynamicExpresso;

namespace WebFlex.OpcCollector.Services;

public class OpcExpressionEvaluator {
    private readonly ILogger<OpcExpressionEvaluator> _logger;
    private readonly ConcurrentDictionary<string, Lambda> _cache = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastErrorLogAt = new();

    public OpcExpressionEvaluator(ILogger<OpcExpressionEvaluator> logger) {
        _logger = logger;
    }

    public string? Evaluate(
        string tagId,
        string? dataType,
        string? expression,
        object? rawValue) {
        if (string.IsNullOrWhiteSpace(expression)) {
            return null;
        }

        try {
            var convertedRaw = ConvertRawValue(dataType, rawValue);
            var rawType = convertedRaw?.GetType() ?? GetRawType(dataType);
            var cacheKey = $"{NormalizeDataType(dataType)}::{rawType.FullName}::{expression}";

            var lambda = _cache.GetOrAdd(cacheKey, _ => {
                var interpreter = new Interpreter()
                    .Reference(typeof(string))
                    .Reference(typeof(Math))
                    .Reference(typeof(Convert))
                    .Reference(typeof(DateTime));

                return interpreter.Parse(
                    expression,
                    new Parameter("raw", rawType)
                );
            });

            var result = lambda.Invoke(convertedRaw);

            return result?.ToString();
        } catch (Exception ex) {
            LogExpressionError(tagId, expression, ex);
            return null;
        }
    }

    private void LogExpressionError(
        string tagId,
        string expression,
        Exception ex) {
        var now = DateTime.UtcNow;
        var key = $"{tagId}::{expression}";

        if (_lastErrorLogAt.TryGetValue(key, out var lastLogAt) &&
            (now - lastLogAt).TotalSeconds < 30) {
            return;
        }

        _lastErrorLogAt[key] = now;

        _logger.LogWarning(
            ex,
            "OPC 계산식 실행 실패 | TagId={TagId} | Expression={Expression} | Error={Error}",
            tagId,
            expression,
            ex.InnerException?.Message ?? ex.Message);
    }

    private static object ConvertRawValue(string? dataType, object? rawValue) {
        var type = NormalizeDataType(dataType);

        if (rawValue == null) {
            return type switch {
                "bit" or "bool" => false,
                "ascii" or "utf8" or "string" => "",
                "datetime" => DateTime.MinValue,
                _ => 0d
            };
        }

        if (rawValue is string rawText) {
            return ConvertRawText(type, rawText);
        }

        try {
            return type switch {
                "bit" => Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture),
                "bool" => Convert.ToBoolean(rawValue, CultureInfo.InvariantCulture),

                "uint8" => Convert.ToByte(rawValue, CultureInfo.InvariantCulture),
                "int8" => Convert.ToSByte(rawValue, CultureInfo.InvariantCulture),

                "uint16" => Convert.ToUInt16(rawValue, CultureInfo.InvariantCulture),
                "int16" => Convert.ToInt16(rawValue, CultureInfo.InvariantCulture),
                "bcd16" => Convert.ToInt32(rawValue, CultureInfo.InvariantCulture),

                "uint32" => Convert.ToUInt32(rawValue, CultureInfo.InvariantCulture),
                "int32" => Convert.ToInt32(rawValue, CultureInfo.InvariantCulture),
                "bcd32" => Convert.ToInt64(rawValue, CultureInfo.InvariantCulture),

                "uint64" => Convert.ToUInt64(rawValue, CultureInfo.InvariantCulture),
                "int64" => Convert.ToInt64(rawValue, CultureInfo.InvariantCulture),

                "float" => Convert.ToSingle(rawValue, CultureInfo.InvariantCulture),
                "double" => Convert.ToDouble(rawValue, CultureInfo.InvariantCulture),

                "datetime" => rawValue is DateTime dateTime
                    ? dateTime
                    : Convert.ToDateTime(rawValue, CultureInfo.InvariantCulture),

                "timestamp(ms)" => Convert.ToInt64(rawValue, CultureInfo.InvariantCulture),
                "timestamp(s)" => Convert.ToInt64(rawValue, CultureInfo.InvariantCulture),

                "ascii" => rawValue.ToString() ?? "",
                "utf8" => rawValue.ToString() ?? "",
                "string" => rawValue.ToString() ?? "",

                _ => rawValue
            };
        } catch {
            return rawValue;
        }
    }

    private static object ConvertRawText(string type, string rawText) {
        var value = rawText.Trim();

        if (value.Length == 0) {
            return type switch {
                "bit" or "bool" => false,
                "ascii" or "utf8" or "string" => "",
                "datetime" => DateTime.MinValue,
                _ => 0d
            };
        }

        return type switch {
            "bit" => ConvertToBoolean(value),
            "bool" => ConvertToBoolean(value),

            "uint8" => byte.Parse(value, CultureInfo.InvariantCulture),
            "int8" => sbyte.Parse(value, CultureInfo.InvariantCulture),

            "uint16" => ushort.Parse(value, CultureInfo.InvariantCulture),
            "int16" => short.Parse(value, CultureInfo.InvariantCulture),
            "bcd16" => int.Parse(value, CultureInfo.InvariantCulture),

            "uint32" => uint.Parse(value, CultureInfo.InvariantCulture),
            "int32" => int.Parse(value, CultureInfo.InvariantCulture),
            "bcd32" => long.Parse(value, CultureInfo.InvariantCulture),

            "uint64" => ulong.Parse(value, CultureInfo.InvariantCulture),
            "int64" => long.Parse(value, CultureInfo.InvariantCulture),

            "float" => float.Parse(value, CultureInfo.InvariantCulture),
            "double" => double.Parse(value, CultureInfo.InvariantCulture),

            "datetime" => DateTime.Parse(value, CultureInfo.InvariantCulture),
            "timestamp(ms)" => long.Parse(value, CultureInfo.InvariantCulture),
            "timestamp(s)" => long.Parse(value, CultureInfo.InvariantCulture),

            "ascii" => value,
            "utf8" => value,
            "string" => value,

            _ => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
                ? number
                : value
        };
    }

    private static Type GetRawType(string? dataType) {
        var type = NormalizeDataType(dataType);

        return type switch {
            "bit" => typeof(bool),
            "bool" => typeof(bool),

            "uint8" => typeof(byte),
            "int8" => typeof(sbyte),

            "uint16" => typeof(ushort),
            "int16" => typeof(short),
            "bcd16" => typeof(int),

            "uint32" => typeof(uint),
            "int32" => typeof(int),
            "bcd32" => typeof(long),

            "uint64" => typeof(ulong),
            "int64" => typeof(long),

            "float" => typeof(float),
            "double" => typeof(double),

            "datetime" => typeof(DateTime),
            "timestamp(ms)" => typeof(long),
            "timestamp(s)" => typeof(long),

            "ascii" => typeof(string),
            "utf8" => typeof(string),
            "string" => typeof(string),

            _ => typeof(object)
        };
    }

    private static string NormalizeDataType(string? dataType) {
        return (dataType ?? "")
            .Trim()
            .Replace(" ", "")
            .ToLowerInvariant();
    }

    private static bool ConvertToBoolean(string value) {
        if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "y", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "n", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        throw new InvalidOperationException("bool 타입 값은 true/false 또는 1/0이어야 합니다.");
    }
}