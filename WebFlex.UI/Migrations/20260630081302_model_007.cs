using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_007 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DEVICE_GROUP",
                columns: new[] { "action_name", "url" },
                values: new object[] { "DVC1020", "/device/dvc1020" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DEVICE_GROUP",
                columns: new[] { "action_name", "url" },
                values: new object[] { "DVC2000", "/device/dvc2000" });
        }
    }
}
