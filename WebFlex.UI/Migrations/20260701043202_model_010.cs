using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_010 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "s_menu",
                columns: new[] { "menu_id", "action_name", "controller_name", "created_at", "icon", "is_enabled", "menu_code", "menu_name", "parent_menu_id", "show_in_menu", "sort_order", "url", "updated_at" },
                values: new object[] { "OPTION_CARD", "opt1000", "Options", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "panel-top", true, "OPTIONS.CARD", "카드 대시보드 옵션", "OPTIONS", true, 65, "/option/opt1000", null });

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0076",
                columns: new[] { "menu_id", "role_id" },
                values: new object[] { "OPTION_CARD", "DevAuth" });

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0077",
                column: "menu_id",
                value: "OPC");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0078",
                column: "menu_id",
                value: "OPC_COLLECT");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0079",
                column: "menu_id",
                value: "SYSTEM");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0080",
                column: "menu_id",
                value: "SYSTEM_SVC");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0081",
                column: "menu_id",
                value: "OPTIONS");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0082",
                column: "menu_id",
                value: "OPTION_COLLECT");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0083",
                column: "menu_id",
                value: "OPTION_CLIENT");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0084",
                column: "menu_id",
                value: "OPTION_HISTORY");

            migrationBuilder.InsertData(
                table: "s_role_menu",
                columns: new[] { "role_menu_id", "can_create", "can_delete", "can_export", "can_read", "can_update", "created_at", "is_enabled", "menu_id", "role_id", "updated_at" },
                values: new object[,]
                {
                    { "RM0085", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_TSD", "TestAuth", null },
                    { "RM0086", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CARD", "TestAuth", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0085");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0086");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPTION_CARD");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0076",
                columns: new[] { "menu_id", "role_id" },
                values: new object[] { "OPC", "TestAuth" });

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0077",
                column: "menu_id",
                value: "OPC_COLLECT");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0078",
                column: "menu_id",
                value: "SYSTEM");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0079",
                column: "menu_id",
                value: "SYSTEM_SVC");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0080",
                column: "menu_id",
                value: "OPTIONS");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0081",
                column: "menu_id",
                value: "OPTION_COLLECT");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0082",
                column: "menu_id",
                value: "OPTION_CLIENT");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0083",
                column: "menu_id",
                value: "OPTION_HISTORY");

            migrationBuilder.UpdateData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0084",
                column: "menu_id",
                value: "OPTION_TSD");
        }
    }
}
