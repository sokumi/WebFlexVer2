using System.Diagnostics;
using System.Text.Json.Serialization;

namespace WebFlex.UI.DTO;

/// <summary>
/// 엑션 Data Transfer Object
/// </summary>
[DebuggerDisplay("{CrudId} {CrudName} {CrudValue}")]
public class CrudDTO {
    [JsonPropertyName("crudid")]
    public string? CrudId { get; set; }

    [JsonPropertyName("crudtitle")]
    public string? CrudTitle { get; set; }

    [JsonPropertyName("crudname")]
    public string? CrudName { get; set; }

    [JsonPropertyName("crudvalue")]
    public string? CrudValue { get; set; }

    [JsonPropertyName("crudvalues")]
    public string?[] CrudValues { get; set; }

    [JsonPropertyName("crudremark")]
    public string? CrudRemark { get; set; }

    [JsonPropertyName("crudbool")]
    public bool? CrudBool { get; set; }

    public class CrudItemsDTO : CrudDTO {
        [JsonPropertyName("items")]
        public List<CrudDTO>? Items { get; set; }
    }
}