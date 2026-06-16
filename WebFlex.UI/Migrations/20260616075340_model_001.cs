using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opc_collect_option",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    option_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    option_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reload_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    save_interval_milliseconds = table.Column<int>(type: "integer", nullable: false),
                    flush_interval_milliseconds = table.Column<int>(type: "integer", nullable: false),
                    max_batch_size = table.Column<int>(type: "integer", nullable: false),
                    save_only_changed_value = table.Column<bool>(type: "boolean", nullable: false),
                    auto_reconnect = table.Column<bool>(type: "boolean", nullable: false),
                    reconnect_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    connection_check_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_collect_option", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "opc_major_group",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    major_group_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    major_group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_major_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "s_menu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_menu_id = table.Column<long>(type: "bigint", nullable: true),
                    menu_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    menu_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    controller_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    show_in_menu = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s_menu", x => x.id);
                    table.ForeignKey(
                        name: "FK_s_menu_s_menu_parent_menu_id",
                        column: x => x.parent_menu_id,
                        principalTable: "s_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "s_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "s_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s_user", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "opc_group",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opc_major_group_id = table.Column<long>(type: "bigint", nullable: true),
                    group_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_group", x => x.id);
                    table.ForeignKey(
                        name: "FK_opc_group_opc_major_group_opc_major_group_id",
                        column: x => x.opc_major_group_id,
                        principalTable: "opc_major_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "s_role_menu",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    s_role_id = table.Column<long>(type: "bigint", nullable: false),
                    s_menu_id = table.Column<long>(type: "bigint", nullable: false),
                    can_read = table.Column<bool>(type: "boolean", nullable: false),
                    can_create = table.Column<bool>(type: "boolean", nullable: false),
                    can_update = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete = table.Column<bool>(type: "boolean", nullable: false),
                    can_export = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s_role_menu", x => x.id);
                    table.ForeignKey(
                        name: "FK_s_role_menu_s_menu_s_menu_id",
                        column: x => x.s_menu_id,
                        principalTable: "s_menu",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_s_role_menu_s_role_s_role_id",
                        column: x => x.s_role_id,
                        principalTable: "s_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "s_user_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    s_user_id = table.Column<long>(type: "bigint", nullable: false),
                    s_role_id = table.Column<long>(type: "bigint", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_s_user_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_s_user_role_s_role_s_role_id",
                        column: x => x.s_role_id,
                        principalTable: "s_role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_s_user_role_s_user_s_user_id",
                        column: x => x.s_user_id,
                        principalTable: "s_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opc_device",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opc_group_id = table.Column<long>(type: "bigint", nullable: true),
                    device_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    device_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    endpoint_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_collect_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    use_security = table.Column<bool>(type: "boolean", nullable: false),
                    security_policy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    security_mode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    use_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    publishing_interval_ms = table.Column<int>(type: "integer", nullable: false),
                    sampling_interval_ms = table.Column<int>(type: "integer", nullable: false),
                    queue_size = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_device", x => x.id);
                    table.ForeignKey(
                        name: "FK_opc_device_opc_group_opc_group_id",
                        column: x => x.opc_group_id,
                        principalTable: "opc_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "opc_collect_runtime_status",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opc_device_id = table.Column<long>(type: "bigint", nullable: true),
                    endpoint_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    is_connected = table.Column<bool>(type: "boolean", nullable: false),
                    subscribed_count = table.Column<int>(type: "integer", nullable: false),
                    last_connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_disconnected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error_message = table.Column<string>(type: "text", nullable: true),
                    status_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_collect_runtime_status", x => x.id);
                    table.ForeignKey(
                        name: "FK_opc_collect_runtime_status_opc_device_opc_device_id",
                        column: x => x.opc_device_id,
                        principalTable: "opc_device",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "opc_tag",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    opc_device_id = table.Column<long>(type: "bigint", nullable: false),
                    opc_group_id = table.Column<long>(type: "bigint", nullable: true),
                    tag_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    node_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    group_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    data_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_collect_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    save_to_database = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_dashboard = table.Column<bool>(type: "boolean", nullable: false),
                    sampling_interval_ms = table.Column<int>(type: "integer", nullable: true),
                    queue_size = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opc_tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_opc_tag_opc_device_opc_device_id",
                        column: x => x.opc_device_id,
                        principalTable: "opc_device",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_opc_tag_opc_group_opc_group_id",
                        column: x => x.opc_group_id,
                        principalTable: "opc_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_opc_collect_option_option_code",
                table: "opc_collect_option",
                column: "option_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_collect_runtime_status_endpoint_url",
                table: "opc_collect_runtime_status",
                column: "endpoint_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_collect_runtime_status_opc_device_id",
                table: "opc_collect_runtime_status",
                column: "opc_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_opc_device_device_code",
                table: "opc_device",
                column: "device_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_device_endpoint_url",
                table: "opc_device",
                column: "endpoint_url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_device_opc_group_id",
                table: "opc_device",
                column: "opc_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_opc_group_group_code",
                table: "opc_group",
                column: "group_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_group_opc_major_group_id",
                table: "opc_group",
                column: "opc_major_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_opc_major_group_major_group_code",
                table: "opc_major_group",
                column: "major_group_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_tag_opc_device_id_node_id",
                table: "opc_tag",
                columns: new[] { "opc_device_id", "node_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_opc_tag_opc_group_id",
                table: "opc_tag",
                column: "opc_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_opc_tag_tag_code",
                table: "opc_tag",
                column: "tag_code");

            migrationBuilder.CreateIndex(
                name: "IX_s_menu_menu_code",
                table: "s_menu",
                column: "menu_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_s_menu_parent_menu_id",
                table: "s_menu",
                column: "parent_menu_id");

            migrationBuilder.CreateIndex(
                name: "IX_s_role_role_code",
                table: "s_role",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_s_role_menu_s_menu_id",
                table: "s_role_menu",
                column: "s_menu_id");

            migrationBuilder.CreateIndex(
                name: "IX_s_role_menu_s_role_id_s_menu_id",
                table: "s_role_menu",
                columns: new[] { "s_role_id", "s_menu_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_s_user_user_id",
                table: "s_user",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_s_user_role_s_role_id",
                table: "s_user_role",
                column: "s_role_id");

            migrationBuilder.CreateIndex(
                name: "IX_s_user_role_s_user_id_s_role_id",
                table: "s_user_role",
                columns: new[] { "s_user_id", "s_role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opc_collect_option");

            migrationBuilder.DropTable(
                name: "opc_collect_runtime_status");

            migrationBuilder.DropTable(
                name: "opc_tag");

            migrationBuilder.DropTable(
                name: "s_role_menu");

            migrationBuilder.DropTable(
                name: "s_user_role");

            migrationBuilder.DropTable(
                name: "opc_device");

            migrationBuilder.DropTable(
                name: "s_menu");

            migrationBuilder.DropTable(
                name: "s_role");

            migrationBuilder.DropTable(
                name: "s_user");

            migrationBuilder.DropTable(
                name: "opc_group");

            migrationBuilder.DropTable(
                name: "opc_major_group");
        }
    }
}
