using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace WebFlex.UI.Common;

public static class WebFlexModelMapper {
    public static T PopulateModel<T>(
        string values,
        Action<T>? beforeMap = null,
        Action<T>? afterMap = null) where T : class, new() {
        if (string.IsNullOrWhiteSpace(values)) {
            var emptyModel = new T();
            beforeMap?.Invoke(emptyModel);
            afterMap?.Invoke(emptyModel);
            return emptyModel;
        }

        using var document = JsonDocument.Parse(values);

        return PopulateModel(
            document.RootElement,
            beforeMap,
            afterMap
        );
    }

    public static T PopulateModel<T>(
        JsonElement element,
        Action<T>? beforeMap = null,
        Action<T>? afterMap = null) where T : class, new() {
        var model = new T();

        beforeMap?.Invoke(model);
        ApplyModel(model, element);
        afterMap?.Invoke(model);

        return model;
    }

    public static void PopulateModel<T>(
        T model,
        string values,
        Action<T>? afterMap = null) where T : class {
        if (string.IsNullOrWhiteSpace(values)) {
            afterMap?.Invoke(model);
            return;
        }

        using var document = JsonDocument.Parse(values);

        ApplyModel(
            model,
            document.RootElement,
            afterMap
        );
    }

    public static void ApplyModel<T>(
        T model,
        JsonElement element,
        Action<T>? afterMap = null) where T : class {
        if (element.ValueKind != JsonValueKind.Object) {
            afterMap?.Invoke(model);
            return;
        }

        var properties = GetMappableProperties(typeof(T));

        foreach (var jsonProperty in element.EnumerateObject()) {
            var jsonName = NormalizeJsonName(jsonProperty.Name);

            if (!properties.TryGetValue(jsonName, out var property)) {
                continue;
            }

            var value = ConvertJsonValue(jsonProperty.Value, property.PropertyType);
            property.SetValue(model, value);
        }

        afterMap?.Invoke(model);
    }

    public static T PopulateDTOModel<T>(string values) {
        if (string.IsNullOrWhiteSpace(values)) {
            return CreateEmptyValue<T>();
        }

        using var document = JsonDocument.Parse(values);

        return PopulateDTOModel<T>(document.RootElement);
    }

    public static T PopulateDTOModel<T>(JsonElement element) {
        var targetType = typeof(T);

        if (IsListType(targetType, out var itemType)) {
            var list = CreateList(itemType);

            if (element.ValueKind == JsonValueKind.Array) {
                foreach (var item in element.EnumerateArray()) {
                    var model = CreateModelFromElement(itemType, item);
                    list.Add(model);
                }
            } else if (element.ValueKind == JsonValueKind.Object) {
                var model = CreateModelFromElement(itemType, element);
                list.Add(model);
            }

            return (T)list;
        }

        if (element.ValueKind == JsonValueKind.Object) {
            var model = Activator.CreateInstance<T>();

            if (model != null) {
                ApplyModelByType(model, targetType, element);
            }

            return model!;
        }

        return (T?)ConvertJsonValue(element, targetType) ?? CreateEmptyValue<T>();
    }

    public static List<T> PopulateList<T>(string values) where T : class, new() {
        if (string.IsNullOrWhiteSpace(values)) {
            return new List<T>();
        }

        using var document = JsonDocument.Parse(values);

        return PopulateList<T>(document.RootElement);
    }

    public static List<T> PopulateList<T>(JsonElement element) where T : class, new() {
        var list = new List<T>();

        if (element.ValueKind == JsonValueKind.Array) {
            foreach (var item in element.EnumerateArray()) {
                list.Add(PopulateModel<T>(item));
            }

            return list;
        }

        if (element.ValueKind == JsonValueKind.Object) {
            list.Add(PopulateModel<T>(element));
        }

        return list;
    }

    public static string? GetString(JsonElement element, params string[] names) {
        return TryGetProperty(element, out var property, names)
            ? GetStringValue(property)
            : null;
    }

    public static List<string> GetStringList(JsonElement element, params string[] names) {
        foreach (var name in names) {
            if (!TryGetProperty(element, out var property, name)) {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Array) {
                return property
                    .EnumerateArray()
                    .Select(GetStringValue)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct()
                    .ToList();
            }

            var value = GetStringValue(property);

            if (!string.IsNullOrWhiteSpace(value)) {
                return new List<string> { value };
            }
        }

        if (element.ValueKind == JsonValueKind.Array) {
            return element
                .EnumerateArray()
                .Select(GetStringValue)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .Distinct()
                .ToList();
        }

        return new List<string>();
    }

    public static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names) {
        property = default;

        if (element.ValueKind != JsonValueKind.Object) {
            return false;
        }

        var normalizedNames = names
            .Select(NormalizeJsonName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in element.EnumerateObject()) {
            if (normalizedNames.Contains(NormalizeJsonName(item.Name))) {
                property = item.Value;
                return true;
            }
        }

        return false;
    }

    private static IDictionary<string, PropertyInfo> GetMappableProperties(Type type) {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanWrite)
            .Where(IsMappableProperty)
            .ToDictionary(
                x => NormalizeModelName(x.Name),
                x => x,
                StringComparer.OrdinalIgnoreCase
            );
    }

    private static bool IsMappableProperty(PropertyInfo property) {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type != typeof(string) && IsListType(type, out _)) {
            return true;
        }

        return type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(decimal) ||
               type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(DateTime) ||
               type == typeof(Guid) ||
               type.IsEnum;
    }

    private static object CreateModelFromElement(Type modelType, JsonElement element) {
        if (element.ValueKind != JsonValueKind.Object) {
            return ConvertJsonValue(element, modelType)
                ?? (modelType.IsValueType ? Activator.CreateInstance(modelType)! : null!);
        }

        var model = Activator.CreateInstance(modelType)
            ?? throw new InvalidOperationException($"{modelType.Name} 인스턴스를 생성할 수 없습니다.");

        ApplyModelByType(model, modelType, element);
        return model;
    }

    private static void ApplyModelByType(object model, Type modelType, JsonElement element) {
        if (element.ValueKind != JsonValueKind.Object) {
            return;
        }

        var properties = GetMappableProperties(modelType);

        foreach (var jsonProperty in element.EnumerateObject()) {
            var jsonName = NormalizeJsonName(jsonProperty.Name);

            if (!properties.TryGetValue(jsonName, out var property)) {
                continue;
            }

            var value = ConvertJsonValue(jsonProperty.Value, property.PropertyType);
            property.SetValue(model, value);
        }
    }

    private static bool IsListType(Type type, out Type itemType) {
        itemType = typeof(object);

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>)) {
            itemType = type.GetGenericArguments()[0];
            return true;
        }

        var enumerableType = type
            .GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType == null || type == typeof(string)) {
            return false;
        }

        itemType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    private static IList CreateList(Type itemType) {
        var listType = typeof(List<>).MakeGenericType(itemType);

        return (IList)(Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException($"{listType.Name} 인스턴스를 생성할 수 없습니다."));
    }

    private static T CreateEmptyValue<T>() {
        var type = typeof(T);

        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null) {
            return default!;
        }

        return Activator.CreateInstance<T>();
    }

    private static object? ConvertJsonValue(JsonElement value, Type propertyType) {
        var nullableType = Nullable.GetUnderlyingType(propertyType);
        var targetType = nullableType ?? propertyType;

        if (value.ValueKind == JsonValueKind.Null ||
            value.ValueKind == JsonValueKind.Undefined) {
            return nullableType != null || !propertyType.IsValueType
                ? null
                : Activator.CreateInstance(targetType);
        }

        if (targetType != typeof(string) && IsListType(targetType, out var itemType)) {
            var list = CreateList(itemType);

            if (value.ValueKind != JsonValueKind.Array) {
                return list;
            }

            foreach (var item in value.EnumerateArray()) {
                list.Add(CreateModelFromElement(itemType, item));
            }

            return list;
        }

        if (targetType == typeof(string)) {
            return GetStringValue(value);
        }

        if (targetType == typeof(bool)) {
            return ConvertBool(value);
        }

        if (targetType == typeof(byte)) {
            return ConvertNumber(value, propertyType, x => byte.Parse(x, CultureInfo.InvariantCulture), (byte)0);
        }

        if (targetType == typeof(sbyte)) {
            return ConvertNumber(value, propertyType, x => sbyte.Parse(x, CultureInfo.InvariantCulture), (sbyte)0);
        }

        if (targetType == typeof(short)) {
            return ConvertNumber(value, propertyType, x => short.Parse(x, CultureInfo.InvariantCulture), (short)0);
        }

        if (targetType == typeof(ushort)) {
            return ConvertNumber(value, propertyType, x => ushort.Parse(x, CultureInfo.InvariantCulture), (ushort)0);
        }

        if (targetType == typeof(int)) {
            return ConvertNumber(value, propertyType, x => int.Parse(x, CultureInfo.InvariantCulture), 0);
        }

        if (targetType == typeof(uint)) {
            return ConvertNumber(value, propertyType, x => uint.Parse(x, CultureInfo.InvariantCulture), 0u);
        }

        if (targetType == typeof(long)) {
            return ConvertNumber(value, propertyType, x => long.Parse(x, CultureInfo.InvariantCulture), 0L);
        }

        if (targetType == typeof(ulong)) {
            return ConvertNumber(value, propertyType, x => ulong.Parse(x, CultureInfo.InvariantCulture), 0UL);
        }

        if (targetType == typeof(decimal)) {
            return ConvertNumber(value, propertyType, x => decimal.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture), 0m);
        }

        if (targetType == typeof(double)) {
            return ConvertNumber(value, propertyType, x => double.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture), 0d);
        }

        if (targetType == typeof(float)) {
            return ConvertNumber(value, propertyType, x => float.Parse(x, NumberStyles.Any, CultureInfo.InvariantCulture), 0f);
        }

        if (targetType == typeof(DateTime)) {
            var text = GetStringValue(value);

            if (string.IsNullOrWhiteSpace(text)) {
                return nullableType != null ? null : DateTime.MinValue;
            }

            return DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var dateValue)
                ? dateValue
                : nullableType != null ? null : DateTime.MinValue;
        }

        if (targetType == typeof(Guid)) {
            var text = GetStringValue(value);

            if (string.IsNullOrWhiteSpace(text)) {
                return nullableType != null ? null : Guid.Empty;
            }

            return Guid.TryParse(text, out var guidValue)
                ? guidValue
                : nullableType != null ? null : Guid.Empty;
        }

        if (targetType.IsEnum) {
            var text = GetStringValue(value);

            if (string.IsNullOrWhiteSpace(text)) {
                return nullableType != null ? null : Activator.CreateInstance(targetType);
            }

            if (int.TryParse(text, out var enumNumber)) {
                return Enum.ToObject(targetType, enumNumber);
            }

            return Enum.Parse(targetType, text, true);
        }

        return null;
    }

    private static object? ConvertNumber<T>(
        JsonElement value,
        Type propertyType,
        Func<string, T> converter,
        T defaultValue) {
        var nullableType = Nullable.GetUnderlyingType(propertyType);
        var text = GetStringValue(value);

        if (string.IsNullOrWhiteSpace(text)) {
            return nullableType != null ? null : defaultValue;
        }

        try {
            return converter(text);
        } catch {
            return nullableType != null ? null : defaultValue;
        }
    }

    private static bool ConvertBool(JsonElement value) {
        if (value.ValueKind == JsonValueKind.True) {
            return true;
        }

        if (value.ValueKind == JsonValueKind.False) {
            return false;
        }

        var text = GetStringValue(value);

        if (string.IsNullOrWhiteSpace(text)) {
            return false;
        }

        if (bool.TryParse(text, out var boolValue)) {
            return boolValue;
        }

        return text == "1" ||
               text.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
               text.Equals("YES", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetStringValue(JsonElement property) {
        return property.ValueKind switch {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => property.GetRawText()
        };
    }

    private static string NormalizeJsonName(string value) {
        var name = NormalizeModelName(value);

        return name switch {
            "tagid" => "id",
            "displayname" => "tagname",
            "expression" => "expressions",
            _ => name
        };
    }

    private static string NormalizeModelName(string value) {
        return value
            .Replace("_", "")
            .Replace("-", "")
            .ToLowerInvariant();
    }
}