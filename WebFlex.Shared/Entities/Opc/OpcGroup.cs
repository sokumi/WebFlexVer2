using System.ComponentModel.DataAnnotations.Schema;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.Opc;

public class OpcGroup : BaseEntity {
    [Column("group_id")]
    public new string Id { get; set; }
    public string? OpcMajorGroupId { get; set; }

    public OpcMajorGroup? MajorGroup { get; set; }

    public string GroupCode { get; set; } = "";

    public string GroupName { get; set; } = "";

    public int SortOrder { get; set; }

    public string? Description { get; set; }

    public ICollection<OpcDevice> Devices { get; set; } = new List<OpcDevice>();

    public ICollection<OpcTag> Tags { get; set; } = new List<OpcTag>();
}