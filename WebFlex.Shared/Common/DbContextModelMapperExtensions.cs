using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace WebFlex.Shared.Common;

public static class DbContextModelMapperExtensions {
    public static T PopulateDTOModel<T>(this DbContext dbContext, object values) where T : new() {
        var model = new T();

        var source = values.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead)
            .ToDictionary(
                x => NormalizeName(x.Name),
                x => x.GetValue(values),
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite)) {
            var key = NormalizeName(property.Name);

            if (!source.TryGetValue(key, out var value)) {
                continue;
            }

            if (value == null) {
                property.SetValue(model, null);
                continue;
            }

            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (targetType == typeof(string)) {
                property.SetValue(model, value.ToString());
            } else if (targetType == typeof(bool)) {
                property.SetValue(model, Convert.ToBoolean(value));
            } else if (targetType == typeof(int)) {
                property.SetValue(model, Convert.ToInt32(value));
            } else if (targetType == typeof(long)) {
                property.SetValue(model, Convert.ToInt64(value));
            } else if (targetType == typeof(decimal)) {
                property.SetValue(model, Convert.ToDecimal(value));
            } else if (targetType == typeof(DateTime)) {
                property.SetValue(model, Convert.ToDateTime(value));
            } else if (targetType.IsEnum) {
                property.SetValue(model, Enum.Parse(targetType, value.ToString() ?? "", true));
            } else {
                property.SetValue(model, Convert.ChangeType(value, targetType));
            }
        }

        return model;
    }

    private static string NormalizeName(string value) {
        return value
            .Replace("_", "")
            .ToLowerInvariant();
    }
}