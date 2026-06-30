using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model_006 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "s_menu",
                columns: new[] { "menu_id", "action_name", "controller_name", "created_at", "icon", "is_enabled", "menu_code", "menu_name", "parent_menu_id", "show_in_menu", "sort_order", "url", "updated_at" },
                values: new object[,]
                {
                    { "DASHBOARD", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "layout-dashboard", true, "DASHBOARD", "DASHBOARD", null, true, 10, null, null },
                    { "DEVICE", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpu", true, "DEVICE", "DEVICE", null, true, 30, null, null },
                    { "OPC", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "network", true, "OPC", "OPC", null, true, 40, null, null },
                    { "OPTIONS", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sliders-horizontal", true, "OPTIONS", "OPTIONS", null, true, 60, null, null },
                    { "SERVICE", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "activity", true, "SERVICE", "SERVICE", null, true, 20, null, null },
                    { "SYSTEM", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "settings", true, "SYSTEM", "SYSTEM", null, true, 50, null, null }
                });

            migrationBuilder.InsertData(
                table: "s_role",
                columns: new[] { "role_id", "created_at", "description", "is_enabled", "role_code", "role_name", "updated_at" },
                values: new object[,]
                {
                    { "BizAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "BizAuth", "고객사 운영 관리자 권한", null },
                    { "DevAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "최고 개발자 권한", true, "DevAuth", "개발자 권한", null },
                    { "FileAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "FileAuth", "파일 모니터링 관리자 권한", null },
                    { "FileUserAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "FileUserAuth", "파일 모니터링 유저 권한", null },
                    { "OPCAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "OPCAuth", "컨설턴트 및 운영 사용자 권한", null },
                    { "TestAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "TestAuth", "테스트 권한", null },
                    { "UserAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "UserAuth", "고객사 유저 권한", null },
                    { "WebflexUser", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "WebflexUser", "사내 개발자 권한", null },
                    { "WOPCUserAuth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "회원가입 사용자 권한", true, "WOPCUserAuth", "일반 사용자 권한", null }
                });

            migrationBuilder.InsertData(
                table: "s_user",
                columns: new[] { "user_uid", "created_at", "email", "is_admin", "is_enabled", "last_login_at", "password_hash", "phone_number", "user_id", "user_name", "updated_at" },
                values: new object[,]
                {
                    { "U_ADMIN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, null, "EA544D16CB34FA2D9A187C0784F0A58EDA0CB147B34628611668EFD869BAF326", null, "admin", "운영 관리자", null },
                    { "U_DEV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, null, "193F2CFB059D2261749B2A8B4820AE6A13D0ABF75552FE364162316569C1645E", null, "dev", "개발자", null },
                    { "U_DEVOPC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, true, null, "5E24FD4A1276FD0CA7823546EBA654A3C45C7DB07B8A692813A6C67446B49997", null, "devopc", "사내 개발자", null },
                    { "U_OPC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, true, null, "C0CE85102A32FB10E3FB7379D187C4D5E50E74B42F5B0E74E6B4278CA5E1B2FB", null, "opc", "OPC 운영자", null },
                    { "U_TEST", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, true, null, "AB6CA302F46469908A299E8F3BFA392BDD84350172C0C8F191249470D227852E", null, "test", "테스트 사용자", null }
                });

            migrationBuilder.InsertData(
                table: "s_menu",
                columns: new[] { "menu_id", "action_name", "controller_name", "created_at", "icon", "is_enabled", "menu_code", "menu_name", "parent_menu_id", "show_in_menu", "sort_order", "url", "updated_at" },
                values: new object[,]
                {
                    { "DASH_CARD", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "panel-top", true, "DASHBOARD.CARD", "CARD", "DASHBOARD", true, 12, "/main/card", null },
                    { "DASH_MAIN", "Index", "Main", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "home", true, "DASHBOARD.MAIN", "MAIN", "DASHBOARD", true, 11, "/", null },
                    { "DEVICE_GROUP", "DVC2000", "Device", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "folder-tree", true, "DEVICE.GROUP", "그룹 관리", "DEVICE", true, 33, "/device/dvc2000", null },
                    { "DEVICE_MANAGE", "DVC1000", "Device", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "server", true, "DEVICE.MANAGE", "디바이스 관리", "DEVICE", true, 31, "/device/dvc1000", null },
                    { "DEVICE_TAG", "DVC1010", "Device", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "tags", true, "DEVICE.TAG", "태그 관리", "DEVICE", true, 32, "/device/dvc1010", null },
                    { "OPC_COLLECT", "OPC1000", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "radio-tower", true, "OPC.COLLECT", "OPC 수집 관리", "OPC", true, 41, "/opc/opc1000", null },
                    { "OPTION_CLIENT", "OPC1030", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "plug", true, "OPTIONS.CLIENT", "Client 옵션", "OPTIONS", true, 62, "/opc/opc1030", null },
                    { "OPTION_COLLECT", "OPC1020", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "settings-2", true, "OPTIONS.COLLECTOR", "Collector 옵션", "OPTIONS", true, 61, "/opc/opc1020", null },
                    { "OPTION_HISTORY", "OPC3000", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "history", true, "OPTIONS.HISTORY", "History 조회", "OPTIONS", true, 63, "/opc/opc3000", null },
                    { "OPTION_TSD", "OPC4000", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "database-zap", true, "OPTIONS.TIMESCALE", "Timescale 설정", "OPTIONS", true, 64, "/opc/opc4000", null },
                    { "SERVICE_DATA", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "database", true, "SERVICE.DATA", "데이터 조회", "SERVICE", true, 21, "/service/data", null },
                    { "SERVICE_TREND", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "chart-line", true, "SERVICE.TREND", "트렌드 조회", "SERVICE", true, 22, "/service/trend", null },
                    { "SYSTEM_SVC", "SVC1000", "System", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "hard-drive", true, "SYSTEM.SERVICE", "Windows Service", "SYSTEM", true, 51, "/system/svc1000", null }
                });

            migrationBuilder.InsertData(
                table: "s_role_menu",
                columns: new[] { "role_menu_id", "can_create", "can_delete", "can_export", "can_read", "can_update", "created_at", "is_enabled", "menu_id", "role_id", "updated_at" },
                values: new object[,]
                {
                    { "RM0001", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "DevAuth", null },
                    { "RM0004", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "DevAuth", null },
                    { "RM0007", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "OPCAuth", null },
                    { "RM0010", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "OPCAuth", null },
                    { "RM0013", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "TestAuth", null },
                    { "RM0016", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "TestAuth", null },
                    { "RM0019", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "FileAuth", null },
                    { "RM0022", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "FileAuth", null },
                    { "RM0025", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "FileUserAuth", null },
                    { "RM0028", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "FileUserAuth", null },
                    { "RM0031", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "WebflexUser", null },
                    { "RM0034", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "WebflexUser", null },
                    { "RM0037", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "UserAuth", null },
                    { "RM0040", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "UserAuth", null },
                    { "RM0043", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "BizAuth", null },
                    { "RM0046", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "BizAuth", null },
                    { "RM0049", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "WOPCUserAuth", null },
                    { "RM0052", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "WOPCUserAuth", null },
                    { "RM0055", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE", "DevAuth", null },
                    { "RM0059", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE", "WebflexUser", null },
                    { "RM0063", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE", "TestAuth", null },
                    { "RM0067", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC", "DevAuth", null },
                    { "RM0069", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM", "DevAuth", null },
                    { "RM0071", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTIONS", "DevAuth", null },
                    { "RM0076", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC", "TestAuth", null },
                    { "RM0078", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM", "TestAuth", null },
                    { "RM0080", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTIONS", "TestAuth", null }
                });

            migrationBuilder.InsertData(
                table: "s_user_role",
                columns: new[] { "user_role_id", "created_at", "is_enabled", "role_id", "user_uid", "updated_at" },
                values: new object[,]
                {
                    { "UR001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DevAuth", "U_DEV", null },
                    { "UR002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPCAuth", "U_OPC", null },
                    { "UR003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "TestAuth", "U_TEST", null },
                    { "UR004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "WebflexUser", "U_DEVOPC", null },
                    { "UR005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "BizAuth", "U_ADMIN", null }
                });

            migrationBuilder.InsertData(
                table: "s_role_menu",
                columns: new[] { "role_menu_id", "can_create", "can_delete", "can_export", "can_read", "can_update", "created_at", "is_enabled", "menu_id", "role_id", "updated_at" },
                values: new object[,]
                {
                    { "RM0002", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "DevAuth", null },
                    { "RM0003", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "DevAuth", null },
                    { "RM0005", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "DevAuth", null },
                    { "RM0006", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "DevAuth", null },
                    { "RM0008", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "OPCAuth", null },
                    { "RM0009", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "OPCAuth", null },
                    { "RM0011", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "OPCAuth", null },
                    { "RM0012", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "OPCAuth", null },
                    { "RM0014", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "TestAuth", null },
                    { "RM0015", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "TestAuth", null },
                    { "RM0017", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "TestAuth", null },
                    { "RM0018", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "TestAuth", null },
                    { "RM0020", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "FileAuth", null },
                    { "RM0021", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "FileAuth", null },
                    { "RM0023", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "FileAuth", null },
                    { "RM0024", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "FileAuth", null },
                    { "RM0026", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "FileUserAuth", null },
                    { "RM0027", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "FileUserAuth", null },
                    { "RM0029", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "FileUserAuth", null },
                    { "RM0030", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "FileUserAuth", null },
                    { "RM0032", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "WebflexUser", null },
                    { "RM0033", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "WebflexUser", null },
                    { "RM0035", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "WebflexUser", null },
                    { "RM0036", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "WebflexUser", null },
                    { "RM0038", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "UserAuth", null },
                    { "RM0039", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "UserAuth", null },
                    { "RM0041", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "UserAuth", null },
                    { "RM0042", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "UserAuth", null },
                    { "RM0044", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "BizAuth", null },
                    { "RM0045", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "BizAuth", null },
                    { "RM0047", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "BizAuth", null },
                    { "RM0048", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "BizAuth", null },
                    { "RM0050", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "WOPCUserAuth", null },
                    { "RM0051", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "WOPCUserAuth", null },
                    { "RM0053", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "WOPCUserAuth", null },
                    { "RM0054", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "WOPCUserAuth", null },
                    { "RM0056", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_MANAGE", "DevAuth", null },
                    { "RM0057", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_TAG", "DevAuth", null },
                    { "RM0058", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_GROUP", "DevAuth", null },
                    { "RM0060", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_MANAGE", "WebflexUser", null },
                    { "RM0061", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_TAG", "WebflexUser", null },
                    { "RM0062", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_GROUP", "WebflexUser", null },
                    { "RM0064", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_MANAGE", "TestAuth", null },
                    { "RM0065", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_TAG", "TestAuth", null },
                    { "RM0066", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_GROUP", "TestAuth", null },
                    { "RM0068", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_COLLECT", "DevAuth", null },
                    { "RM0070", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM_SVC", "DevAuth", null },
                    { "RM0072", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_COLLECT", "DevAuth", null },
                    { "RM0073", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CLIENT", "DevAuth", null },
                    { "RM0074", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_HISTORY", "DevAuth", null },
                    { "RM0075", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_TSD", "DevAuth", null },
                    { "RM0077", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_COLLECT", "TestAuth", null },
                    { "RM0079", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM_SVC", "TestAuth", null },
                    { "RM0081", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_COLLECT", "TestAuth", null },
                    { "RM0082", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CLIENT", "TestAuth", null },
                    { "RM0083", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_HISTORY", "TestAuth", null },
                    { "RM0084", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_TSD", "TestAuth", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0001");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0002");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0003");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0004");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0005");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0006");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0007");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0008");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0009");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0010");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0011");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0012");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0013");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0014");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0015");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0016");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0017");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0018");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0019");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0020");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0021");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0022");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0023");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0024");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0025");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0026");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0027");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0028");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0029");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0030");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0031");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0032");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0033");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0034");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0035");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0036");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0037");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0038");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0039");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0040");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0041");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0042");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0043");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0044");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0045");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0046");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0047");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0048");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0049");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0050");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0051");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0052");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0053");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0054");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0055");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0056");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0057");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0058");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0059");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0060");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0061");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0062");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0063");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0064");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0065");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0066");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0067");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0068");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0069");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0070");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0071");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0072");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0073");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0074");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0075");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0076");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0077");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0078");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0079");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0080");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0081");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0082");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0083");

            migrationBuilder.DeleteData(
                table: "s_role_menu",
                keyColumn: "role_menu_id",
                keyValue: "RM0084");

            migrationBuilder.DeleteData(
                table: "s_user_role",
                keyColumn: "user_role_id",
                keyValue: "UR001");

            migrationBuilder.DeleteData(
                table: "s_user_role",
                keyColumn: "user_role_id",
                keyValue: "UR002");

            migrationBuilder.DeleteData(
                table: "s_user_role",
                keyColumn: "user_role_id",
                keyValue: "UR003");

            migrationBuilder.DeleteData(
                table: "s_user_role",
                keyColumn: "user_role_id",
                keyValue: "UR004");

            migrationBuilder.DeleteData(
                table: "s_user_role",
                keyColumn: "user_role_id",
                keyValue: "UR005");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DASH_CARD");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DASH_MAIN");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DEVICE_GROUP");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DEVICE_MANAGE");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DEVICE_TAG");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPC_COLLECT");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPTION_CLIENT");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPTION_COLLECT");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPTION_HISTORY");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPTION_TSD");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "SERVICE_DATA");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "SERVICE_TREND");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "SYSTEM_SVC");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "BizAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "DevAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "FileAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "FileUserAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "OPCAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "TestAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "UserAuth");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "WebflexUser");

            migrationBuilder.DeleteData(
                table: "s_role",
                keyColumn: "role_id",
                keyValue: "WOPCUserAuth");

            migrationBuilder.DeleteData(
                table: "s_user",
                keyColumn: "user_uid",
                keyValue: "U_ADMIN");

            migrationBuilder.DeleteData(
                table: "s_user",
                keyColumn: "user_uid",
                keyValue: "U_DEV");

            migrationBuilder.DeleteData(
                table: "s_user",
                keyColumn: "user_uid",
                keyValue: "U_DEVOPC");

            migrationBuilder.DeleteData(
                table: "s_user",
                keyColumn: "user_uid",
                keyValue: "U_OPC");

            migrationBuilder.DeleteData(
                table: "s_user",
                keyColumn: "user_uid",
                keyValue: "U_TEST");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DASHBOARD");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "DEVICE");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPC");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "OPTIONS");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "SERVICE");

            migrationBuilder.DeleteData(
                table: "s_menu",
                keyColumn: "menu_id",
                keyValue: "SYSTEM");
        }
    }
}
