using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_009 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opc_option_card",
                columns: table => new
                {
                    card_id = table.Column<string>(type: "text", nullable: false),
                    tag_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "태그 아이디"),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "표시 상태"),
                    match_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "조건 타입"),
                    text_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "문자 조건값"),
                    min_value = table.Column<decimal>(type: "numeric", nullable: true, comment: "최소값"),
                    max_value = table.Column<decimal>(type: "numeric", nullable: true, comment: "최대값"),
                    sort_order = table.Column<int>(type: "integer", nullable: true, comment: "정렬 순서"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_option_card", x => x.card_id);
                    table.ForeignKey(
                        name: "fk_opc_option_card_opc_tag_tag_id",
                        column: x => x.tag_id,
                        principalTable: "opc_tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "OPC 카드 대시보드 태그 표시 옵션");

            migrationBuilder.UpdateData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DASH_CARD",
                column: "url",
                value: "/main/dbd1000");

            migrationBuilder.CreateIndex(
                name: "ix_opc_option_card_tag_id",
                table: "opc_option_card",
                column: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opc_option_card");

            migrationBuilder.UpdateData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DASH_CARD",
                column: "url",
                value: "/main/card");
        }
    }
}
