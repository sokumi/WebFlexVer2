using System.ComponentModel.DataAnnotations.Schema;
using WebFlex.Shared.Entities;

namespace WebFlex.Shared.Entities.Opc;

public class OpcMajorGroup : BaseEntity {
    [Column("magpid")]
    public new string Id { get; set; }
    public string MajorGroupCode { get; set; } = "";

    public string MajorGroupName { get; set; } = "";

    public int SortOrder { get; set; }

    public string? Description { get; set; }

    public ICollection<OpcGroup> Groups { get; set; } = new List<OpcGroup>();
}