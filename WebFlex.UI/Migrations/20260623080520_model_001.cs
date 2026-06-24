using System;
using Microsoft.EntityFrameworkCore.Migrations;

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
                name: "opc_client_option",
                columns: table => new
                {
                    client_option_id = table.Column<string>(type: "text", nullable: false),
                    option_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "옵션 코드"),
                    option_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "옵션명"),
                    option_json = table.Column<string>(type: "text", nullable: true, comment: "옵션 JSON"),
                    configured_option_names = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "설정된 옵션명 목록"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_client_option", x => x.client_option_id);
                },
                comment: "OPC 클라이언트 옵션");

            migrationBuilder.CreateTable(
                name: "opc_collect_option",
                columns: table => new
                {
                    collect_option_id = table.Column<string>(type: "text", nullable: false),
                    option_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, comment: "옵션 코드"),
                    option_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "옵션명"),
                    reload_interval_seconds = table.Column<int>(type: "integer", nullable: true, comment: "수집 대상 재조회 주기(초)"),
                    save_interval_milliseconds = table.Column<int>(type: "integer", nullable: true, comment: "저장 주기(ms)"),
                    flush_interval_milliseconds = table.Column<int>(type: "integer", nullable: true, comment: "Flush 주기(ms)"),
                    max_batch_size = table.Column<int>(type: "integer", nullable: true, comment: "최대 배치 크기"),
                    save_only_changed_value = table.Column<bool>(type: "boolean", nullable: false, comment: "변경값만 저장 여부"),
                    auto_reconnect = table.Column<bool>(type: "boolean", nullable: false, comment: "자동 재연결 여부"),
                    reconnect_interval_seconds = table.Column<int>(type: "integer", nullable: true, comment: "재연결 주기(초)"),
                    connection_check_interval_seconds = table.Column<int>(type: "integer", nullable: true, comment: "연결 확인 주기(초)"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_collect_option", x => x.collect_option_id);
                },
                comment: "OPC 수집 옵션");

            migrationBuilder.CreateTable(
                name: "opc_major_group",
                columns: table => new
                {
                    major_group_id = table.Column<string>(type: "text", nullable: false),
                    major_group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "대그룹명"),
                    sort_order = table.Column<int>(type: "integer", nullable: true, comment: "정렬 순서"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_major_group", x => x.major_group_id);
                },
                comment: "OPC 대그룹 정보");

            migrationBuilder.CreateTable(
                name: "s_menu",
                columns: table => new
                {
                    menu_id = table.Column<string>(type: "text", nullable: false),
                    parent_menu_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "상위 메뉴 아이디"),
                    menu_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "메뉴 코드"),
                    menu_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "메뉴명"),
                    controller_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "컨트롤러명"),
                    action_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "액션명"),
                    url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true, comment: "URL"),
                    icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "아이콘"),
                    sort_order = table.Column<int>(type: "integer", nullable: true, comment: "정렬 순서"),
                    show_in_menu = table.Column<bool>(type: "boolean", nullable: false, comment: "메뉴 표시 여부"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_s_menu", x => x.menu_id);
                    table.ForeignKey(
                        name: "fk_s_menu_s_menu_parent_menu_id",
                        column: x => x.parent_menu_id,
                        principalTable: "s_menu",
                        principalColumn: "menu_id");
                },
                comment: "시스템 메뉴 정보");

            migrationBuilder.CreateTable(
                name: "s_role",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "text", nullable: false),
                    role_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "권한 코드"),
                    role_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "권한명"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_s_role", x => x.role_id);
                },
                comment: "시스템 권한 정보");

            migrationBuilder.CreateTable(
                name: "s_user",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, comment: "사용자 아이디"),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "사용자명"),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "비밀번호 해시"),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "이메일"),
                    phone_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "전화번호"),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false, comment: "관리자 여부"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "마지막 로그인 일시"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_s_user", x => x.user_uid);
                },
                comment: "시스템 사용자 정보");

            migrationBuilder.CreateTable(
                name: "opc_group",
                columns: table => new
                {
                    group_id = table.Column<string>(type: "text", nullable: false),
                    major_group_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "대그룹 아이디"),
                    group_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "그룹명"),
                    sort_order = table.Column<int>(type: "integer", nullable: true, comment: "정렬 순서"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_group", x => x.group_id);
                    table.ForeignKey(
                        name: "fk_opc_group_opc_major_group_major_group_id",
                        column: x => x.major_group_id,
                        principalTable: "opc_major_group",
                        principalColumn: "major_group_id");
                },
                comment: "OPC 중그룹 정보");

            migrationBuilder.CreateTable(
                name: "s_role_menu",
                columns: table => new
                {
                    role_menu_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "권한 아이디"),
                    menu_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "메뉴 아이디"),
                    can_read = table.Column<bool>(type: "boolean", nullable: false, comment: "조회 권한"),
                    can_create = table.Column<bool>(type: "boolean", nullable: false, comment: "생성 권한"),
                    can_update = table.Column<bool>(type: "boolean", nullable: false, comment: "수정 권한"),
                    can_delete = table.Column<bool>(type: "boolean", nullable: false, comment: "삭제 권한"),
                    can_export = table.Column<bool>(type: "boolean", nullable: false, comment: "내보내기 권한"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_s_role_menu", x => x.role_menu_id);
                    table.ForeignKey(
                        name: "fk_s_role_menu_s_menu_menu_id",
                        column: x => x.menu_id,
                        principalTable: "s_menu",
                        principalColumn: "menu_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_s_role_menu_s_role_role_id",
                        column: x => x.role_id,
                        principalTable: "s_role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "시스템 권한별 메뉴 정보");

            migrationBuilder.CreateTable(
                name: "s_user_role",
                columns: table => new
                {
                    user_role_id = table.Column<string>(type: "text", nullable: false),
                    user_uid = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "사용자 고유 아이디"),
                    role_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "권한 아이디"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_s_user_role", x => x.user_role_id);
                    table.ForeignKey(
                        name: "fk_s_user_role_s_role_role_id",
                        column: x => x.role_id,
                        principalTable: "s_role",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_s_user_role_s_user_user_uid",
                        column: x => x.user_uid,
                        principalTable: "s_user",
                        principalColumn: "user_uid",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "시스템 사용자별 권한 정보");

            migrationBuilder.CreateTable(
                name: "opc_device",
                columns: table => new
                {
                    device_id = table.Column<string>(type: "text", nullable: false),
                    device_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "디바이스명"),
                    device_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "디바이스 주소"),
                    port = table.Column<int>(type: "integer", nullable: true, comment: "포트"),
                    endpoint_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Endpoint URL"),
                    device_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "디바이스 타입"),
                    is_collectenabled = table.Column<bool>(type: "boolean", nullable: false, comment: "수집 사용 여부"),
                    use_anonymous = table.Column<bool>(type: "boolean", nullable: false, comment: "익명 접속 사용 여부"),
                    publishingintervalms = table.Column<int>(type: "integer", nullable: true, comment: "Publishing Interval(ms)"),
                    samplingintervalms = table.Column<int>(type: "integer", nullable: true, comment: "Sampling Interval(ms)"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    usesecurity = table.Column<bool>(type: "boolean", nullable: false, comment: "보안 사용 여부"),
                    securitymode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "보안 모드"),
                    securitypolicy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "보안 정책"),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "OPC 서버 접속 사용자명"),
                    password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "OPC 서버 접속 비밀번호"),
                    opc_group_id = table.Column<string>(type: "text", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_device", x => x.device_id);
                    table.ForeignKey(
                        name: "fk_opc_device_opc_group_opc_group_id",
                        column: x => x.opc_group_id,
                        principalTable: "opc_group",
                        principalColumn: "group_id");
                },
                comment: "디바이스 정보");

            migrationBuilder.CreateTable(
                name: "opc_collect_runtime_status",
                columns: table => new
                {
                    runtime_status_id = table.Column<string>(type: "text", nullable: false),
                    device_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "디바이스 아이디"),
                    endpoint_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Endpoint URL"),
                    is_connected = table.Column<bool>(type: "boolean", nullable: false, comment: "연결 여부"),
                    subscribed_count = table.Column<int>(type: "integer", nullable: true, comment: "구독 태그 수"),
                    last_connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "마지막 연결 일시"),
                    last_disconnected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "마지막 연결 해제 일시"),
                    last_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "마지막 수신 일시"),
                    last_error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true, comment: "마지막 오류 메시지"),
                    status_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "상태 갱신 일시"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_collect_runtime_status", x => x.runtime_status_id);
                    table.ForeignKey(
                        name: "fk_opc_collect_runtime_status_opc_device_device_id",
                        column: x => x.device_id,
                        principalTable: "opc_device",
                        principalColumn: "device_id");
                },
                comment: "OPC 수집 런타임 상태");

            migrationBuilder.CreateTable(
                name: "opc_tag",
                columns: table => new
                {
                    tag_id = table.Column<string>(type: "text", nullable: false),
                    device_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false, comment: "디바이스 아이디"),
                    node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "노드 아이디"),
                    group_id = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true, comment: "중그룹 아이디"),
                    tag_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "표시명"),
                    data_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "데이터 타입"),
                    is_collectenabled = table.Column<bool>(type: "boolean", nullable: false, comment: "수집 사용 여부"),
                    save_to_database = table.Column<bool>(type: "boolean", nullable: false, comment: "DB 저장 여부"),
                    show_on_dashboard = table.Column<bool>(type: "boolean", nullable: false, comment: "대시보드 표시 여부"),
                    samplingintervalms = table.Column<int>(type: "integer", nullable: true, comment: "Sampling Interval(ms)"),
                    sort_order = table.Column<int>(type: "integer", nullable: true, comment: "정렬 순서"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "설명"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opc_tag", x => x.tag_id);
                    table.ForeignKey(
                        name: "fk_opc_tag_opc_device_device_id",
                        column: x => x.device_id,
                        principalTable: "opc_device",
                        principalColumn: "device_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_opc_tag_opc_group_group_id",
                        column: x => x.group_id,
                        principalTable: "opc_group",
                        principalColumn: "group_id");
                },
                comment: "OPC 태그 정보");

            migrationBuilder.CreateIndex(
                name: "ix_opc_collect_runtime_status_device_id",
                table: "opc_collect_runtime_status",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_opc_device_opc_group_id",
                table: "opc_device",
                column: "opc_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_opc_group_major_group_id",
                table: "opc_group",
                column: "major_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_opc_tag_device_id",
                table: "opc_tag",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_opc_tag_group_id",
                table: "opc_tag",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_s_menu_parent_menu_id",
                table: "s_menu",
                column: "parent_menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_s_role_menu_menu_id",
                table: "s_role_menu",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_s_role_menu_role_id",
                table: "s_role_menu",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_s_user_role_role_id",
                table: "s_user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_s_user_role_user_uid",
                table: "s_user_role",
                column: "user_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opc_client_option");

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
