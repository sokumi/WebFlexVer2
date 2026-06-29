using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "s_new_no",
                columns: table => new
                {
                    no_id = table.Column<string>(type: "text", nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "번호 접두어"),
                    date_part = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false, comment: "날짜"),
                    current_no = table.Column<int>(type: "integer", nullable: false, comment: "현재 번호"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_s_new_no", x => x.no_id);
                },
                comment: "공통 번호 채번 정보");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "s_new_no");
        }
    }
}
