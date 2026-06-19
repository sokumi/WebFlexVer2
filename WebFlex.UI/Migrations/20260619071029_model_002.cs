using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opc_client_option",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    option_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    option_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    option_json = table.Column<string>(type: "text", nullable: false),
                    configured_option_names = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_client_option", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_opc_client_option_option_code",
                table: "opc_client_option",
                column: "option_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opc_client_option");
        }
    }
}
