using System.ComponentModel.DataAnnotations.Schema;

namespace WebFlex.Shared;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class KeyFieldColumnAttribute : Attribute {
    /// <summary>
    /// 컬럼명
    /// </summary>
    public string? CustomColumnName { get; }

    /// <summary>
    /// 컬럼 설명
    /// </summary>
    public string? ColumnComment { get; }

    /// <summary>
    /// 데이터 베이스 생성 옵션
    /// </summary>
    public DatabaseGeneratedOption? DbGeneratedOption { get; }

    /// <summary>
    /// 키필드가 문자열이면 문자열 길이(기본 10)
    /// </summary>
    public int StringLength { get; } = 10;

    /// <summary>
    /// 데이트타임 필드경우 옵션
    /// </summary>
    public bool IsUtc { get; }

    public KeyFieldColumnAttribute(string? customColumnName = null, string? columnComment = null, int stringLength = 10, DatabaseGeneratedOption databaseGeneratedOption = DatabaseGeneratedOption.None) {
        CustomColumnName = customColumnName;
        ColumnComment = columnComment;
        StringLength = stringLength;
        DbGeneratedOption = databaseGeneratedOption;
    }
}