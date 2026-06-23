using System.ComponentModel.DataAnnotations;

namespace WebFlex.Shared;

/// <summary>
/// 필수 입력
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnRequiredAttribute : RequiredAttribute {
    /// <summary>
    /// 에러 메세지로 localizer 키가 'entity.required' 설정됨
    /// </summary>
    public ColumnRequiredAttribute() : base() {
        this.ErrorMessage = "entity.required";
    }
}

