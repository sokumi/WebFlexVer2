using System.ComponentModel.DataAnnotations;

namespace WebFlex.Shared;

/// <summary>
/// 컬럼 길이 제한
/// </summary>
public class ColumnStringLengthAttribute : StringLengthAttribute {
    /// <summary>
    /// 에러 메세지로 localizer 키가 'entity.validate.stringmax' 설정됨
    /// </summary>
    public ColumnStringLengthAttribute(int length) : base(length) {
        ErrorMessage = "entity.validate.stringmax";
    }
}

/// <summary>
/// 이름 필드는 50자 제한
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ColumnNameAttribute : StringLengthAttribute {
    /// <summary>
    /// 에러 메세지로 localizer 키가 'entity.validate.stringmax' 설정됨
    /// </summary>
    public ColumnNameAttribute() : base(50) {
        ErrorMessage = "entity.validate.stringmax";
    }
}

/// <summary>
/// 전화번호 15자
/// </summary>
public class ColumnNumberAttribute : StringLengthAttribute {
    /// <summary>
    /// 에러 메세지로 localizer 키가 'entity.validate.stringmax' 설정됨
    /// </summary>
    public ColumnNumberAttribute() : base(15) {
        ErrorMessage = "entity.validate.stringmax";
    }
}

/// <summary>
/// JTW 토큰 길이 50자
/// </summary>
public class JTWTokenDefinition : StringLengthAttribute {
    public JTWTokenDefinition() : base(50) { }
}