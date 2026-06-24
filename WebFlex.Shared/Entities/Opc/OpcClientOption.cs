using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared;

/// <summary>
/// OPC 클라이언트 옵션
/// </summary>
[DebuggerDisplay("{Id} {OPTION_CODE} {OPTION_NAME}")]
[Table("opc_client_option"), KeyFieldColumn("CLIENT_OPTION_ID"), Comment("OPC 클라이언트 옵션")]
public class OpcClientOption : BaseEntity {

    [ColumnRequired]
    [ColumnStringLength(30)]
    [Column(Order = 11), Comment("옵션 코드")]
    [DisplayName("entity.OpcClientOption.OPTION_CODE")]
    public string OPTION_CODE { get; set; }

    [ColumnRequired]
    [ColumnStringLength(100)]
    [Column(Order = 12), Comment("옵션명")]
    [DisplayName("entity.OpcClientOption.OPTION_NAME")]
    public string OPTION_NAME { get; set; } 

    [Column(Order = 13), Comment("옵션 JSON")]
    [DisplayName("entity.OpcClientOption.OPTION_JSON")]
    public string? OPTION_JSON { get; set; }

    [Column(Order = 14), Comment("설정된 옵션명 목록")]
    [DisplayName("entity.OpcClientOption.CONFIGURED_OPTION_NAMES")]
    public string? CONFIGURED_OPTION_NAMES { get; set; }

    [ColumnStringLength(500)]
    [Column(Order = 15), Comment("설명")]
    [DisplayName("entity.OpcClientOption.DESCRIPTION")]
    public string? DESCRIPTION { get; set; }
}