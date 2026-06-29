using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_005 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "expressions",
                table: "opc_tag",
                type: "text",
                nullable: true,
                comment: "계산식")
                .Annotation("Relational:ColumnOrder", 19);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expressions",
                table: "opc_tag");
        }
    }
}
