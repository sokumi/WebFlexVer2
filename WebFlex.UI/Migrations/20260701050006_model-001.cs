using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebFlex.UI.Migrations
{
    /// <inheritdoc />
    public partial class model001 : Migration
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
                    configured_option_names = table.Column<string>(type: "text", nullable: true, comment: "설정된 옵션명 목록"),
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
                    option_json = table.Column<string>(type: "text", nullable: true, comment: "옵션 JSON"),
                    configured_option_names = table.Column<string>(type: "text", nullable: true, comment: "설정된 옵션명 목록"),
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
                    protect_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "권한 타입"),
                    expressions = table.Column<string>(type: "text", nullable: true, comment: "계산식"),
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

            migrationBuilder.InsertData(
                table: "opc_client_option",
                columns: new[] { "client_option_id", "configured_option_names", "created_at", "description", "is_enabled", "option_code", "option_json", "option_name", "updated_at" },
                values: new object[] { "CLIENT_OPTION_DEFAULT", "ApplicationName,ApplicationUri,ProductUri,ApplicationType,DisableHiResClock,ApplicationCertificateStoreType,ApplicationCertificateStorePath,ApplicationCertificateSubjectName,ApplicationCertificateThumbprint,TrustedPeerCertificatesStoreType,TrustedPeerCertificatesStorePath,TrustedIssuerCertificatesStoreType,TrustedIssuerCertificatesStorePath,RejectedCertificateStoreType,RejectedCertificateStorePath,AutoAcceptUntrustedCertificates,RejectSHA1SignedCertificates,RejectUnknownRevocationStatus,MinimumCertificateKeySize,AddAppCertToTrustedStore,SuppressNonceValidationErrors,SendCertificateChain,OperationTimeout,MaxStringLength,MaxByteStringLength,MaxArrayLength,MaxMessageSize,MaxBufferSize,ChannelLifetime,SecurityTokenLifetime,DefaultSessionTimeout,MinSubscriptionLifetime,WellKnownDiscoveryUrls,DiscoveryServers,EndpointCacheFilePath,EndpointUrl,UseSecurity,SecurityPolicyUri,MessageSecurityMode,TransportProfileUri,EndpointSelectionTimeout,IdentityType,UserName,Password,CertificateUserStoreType,CertificateUserStorePath,CertificateUserSubjectName,SessionName,SessionTimeout,UpdateBeforeConnect,CheckDomain,PreferredLocales,PublishingInterval,LifetimeCount,KeepAliveCount,MaxNotificationsPerPublish,PublishingEnabled,Priority,AttributeId,MonitoringMode,SamplingInterval,QueueSize,DiscardOldest,DeadbandType,DeadbandValue,DataChangeTrigger,BrowseNodeClassMask,BrowseResultMask,BrowseMaxReferencesToReturn,ReadMaxAge,ReadTimestampsToReturn,EnableSessionKeepAlive,KeepAliveInterval,ReconnectPeriod,MaxReconnectAttempts,HistoryReadMode,HistoryReturnBounds,HistoryReadModified,HistoryNumValuesPerNode,HistoryTimestampsToReturn,HistoryReleaseContinuationPoints,HistoryMaxContinuationReads,HistoryDefaultRangeMinutes", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OPC UA Client 기본 옵션", true, "DEFAULT", "{\r\n  \"applicationName\": \"WebFlexOpcCollector\",\r\n  \"applicationUri\": \"urn:localhost:WebFlexOpcCollector\",\r\n  \"productUri\": \"WebFlexOpcCollector\",\r\n  \"applicationType\": \"Client\",\r\n  \"disableHiResClock\": true,\r\n\r\n  \"applicationCertificateStoreType\": \"Directory\",\r\n  \"applicationCertificateStorePath\": \"pki/own\",\r\n  \"applicationCertificateSubjectName\": \"WebFlexOpcCollector\",\r\n  \"applicationCertificateThumbprint\": \"\",\r\n\r\n  \"trustedPeerCertificatesStoreType\": \"Directory\",\r\n  \"trustedPeerCertificatesStorePath\": \"pki/trusted\",\r\n  \"trustedIssuerCertificatesStoreType\": \"Directory\",\r\n  \"trustedIssuerCertificatesStorePath\": \"pki/issuer\",\r\n  \"rejectedCertificateStoreType\": \"Directory\",\r\n  \"rejectedCertificateStorePath\": \"pki/rejected\",\r\n\r\n  \"autoAcceptUntrustedCertificates\": true,\r\n  \"rejectSHA1SignedCertificates\": false,\r\n  \"rejectUnknownRevocationStatus\": false,\r\n  \"minimumCertificateKeySize\": 1024,\r\n  \"addAppCertToTrustedStore\": false,\r\n  \"suppressNonceValidationErrors\": true,\r\n  \"sendCertificateChain\": false,\r\n\r\n  \"operationTimeout\": 6000000,\r\n  \"maxStringLength\": 2147483647,\r\n  \"maxByteStringLength\": 2147483647,\r\n  \"maxArrayLength\": 65535,\r\n  \"maxMessageSize\": 419430400,\r\n  \"maxBufferSize\": 65535,\r\n  \"channelLifetime\": -1,\r\n  \"securityTokenLifetime\": -1,\r\n\r\n  \"defaultSessionTimeout\": -1,\r\n  \"minSubscriptionLifetime\": -1,\r\n  \"wellKnownDiscoveryUrls\": \"\",\r\n  \"discoveryServers\": \"\",\r\n  \"endpointCacheFilePath\": \"\",\r\n\r\n  \"endpointUrl\": \"opc.tcp://127.0.0.1:49320\",\r\n  \"useSecurity\": false,\r\n  \"securityPolicyUri\": \"\",\r\n  \"messageSecurityMode\": \"\",\r\n  \"transportProfileUri\": \"\",\r\n  \"endpointSelectionTimeout\": 15000,\r\n\r\n  \"identityType\": \"Anonymous\",\r\n  \"userName\": \"\",\r\n  \"password\": \"\",\r\n  \"certificateUserStoreType\": \"Directory\",\r\n  \"certificateUserStorePath\": \"pki/user\",\r\n  \"certificateUserSubjectName\": \"\",\r\n\r\n  \"sessionName\": \"WebFlexOpcCollector\",\r\n  \"sessionTimeout\": 60000,\r\n  \"updateBeforeConnect\": false,\r\n  \"checkDomain\": false,\r\n  \"preferredLocales\": \"ko-KR,en-US\",\r\n\r\n  \"publishingInterval\": 1000,\r\n  \"lifetimeCount\": 60,\r\n  \"keepAliveCount\": 10,\r\n  \"maxNotificationsPerPublish\": 0,\r\n  \"publishingEnabled\": true,\r\n  \"priority\": 100,\r\n\r\n  \"attributeId\": \"Value\",\r\n  \"monitoringMode\": \"Reporting\",\r\n  \"samplingInterval\": 1000,\r\n  \"queueSize\": 1,\r\n  \"discardOldest\": true,\r\n  \"deadbandType\": \"None\",\r\n  \"deadbandValue\": 0,\r\n  \"dataChangeTrigger\": \"StatusValue\",\r\n\r\n  \"browseNodeClassMask\": \"Object,Variable\",\r\n  \"browseResultMask\": \"All\",\r\n  \"browseMaxReferencesToReturn\": 0,\r\n  \"readMaxAge\": 0,\r\n  \"readTimestampsToReturn\": \"Both\",\r\n\r\n  \"enableSessionKeepAlive\": true,\r\n  \"keepAliveInterval\": 5000,\r\n  \"reconnectPeriod\": 10000,\r\n  \"maxReconnectAttempts\": -1,\r\n\r\n  \"historyReadMode\": \"Raw\",\r\n  \"historyReturnBounds\": true,\r\n  \"historyReadModified\": false,\r\n  \"historyNumValuesPerNode\": 0,\r\n  \"historyTimestampsToReturn\": \"Both\",\r\n  \"historyReleaseContinuationPoints\": false,\r\n  \"historyMaxContinuationReads\": 10,\r\n  \"historyDefaultRangeMinutes\": 60\r\n}", "기본 OPC Client 옵션", null });

            migrationBuilder.InsertData(
                table: "opc_collect_option",
                columns: new[] { "collect_option_id", "auto_reconnect", "configured_option_names", "connection_check_interval_seconds", "created_at", "description", "flush_interval_milliseconds", "is_enabled", "max_batch_size", "option_code", "option_json", "option_name", "reconnect_interval_seconds", "reload_interval_seconds", "save_interval_milliseconds", "save_only_changed_value", "updated_at" },
                values: new object[] { "COLLECT_OPTION_DEFAULT", true, "EnableAutoReload,EnableSnapshotSave,EnableTimescaleHistorySave,EnableCurrentValueSave,ReloadIntervalSeconds,SaveIntervalMilliseconds,MaxBatchSize,WriterLogIntervalSeconds,DefaultPublishingIntervalMs,DefaultSamplingIntervalMs,DefaultQueueSize,SubscriptionKeepAliveCount,SubscriptionLifetimeCount,MaxNotificationsPerPublish,SubscriptionPriority,DiscardOldest,AutoAcceptUntrustedCertificates,RejectSHA1SignedCertificates,MinimumCertificateKeySize,SuppressNonceValidationErrors,CertificateStoreRootPath,OperationTimeoutMilliseconds,DefaultSessionTimeoutMilliseconds,MinSubscriptionLifetimeMilliseconds,MaxStringLength,MaxByteStringLength,MaxArrayLength,MaxMessageSize,MaxBufferSize,ChannelLifetime,SecurityTokenLifetime,DisableHiResClock,DefaultUseSecurity,DefaultUseAnonymous,DefaultSecurityPolicy,DefaultSecurityMode", 30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "OPC Collector 기본 수집 옵션", 200, true, 5000, "DEFAULT", "{\r\n  \"enableAutoReload\": true,\r\n  \"enableSnapshotSave\": true,\r\n  \"enableTimescaleHistorySave\": true,\r\n  \"enableCurrentValueSave\": true,\r\n  \"reloadIntervalSeconds\": 3600,\r\n  \"saveIntervalMilliseconds\": 1000,\r\n  \"maxBatchSize\": 5000,\r\n  \"writerLogIntervalSeconds\": 30,\r\n  \"defaultPublishingIntervalMs\": 1000,\r\n  \"defaultSamplingIntervalMs\": 1000,\r\n  \"defaultQueueSize\": 1,\r\n  \"subscriptionKeepAliveCount\": -1,\r\n  \"subscriptionLifetimeCount\": -1,\r\n  \"maxNotificationsPerPublish\": -1,\r\n  \"subscriptionPriority\": 100,\r\n  \"discardOldest\": true,\r\n  \"autoAcceptUntrustedCertificates\": true,\r\n  \"rejectSHA1SignedCertificates\": false,\r\n  \"minimumCertificateKeySize\": 1024,\r\n  \"suppressNonceValidationErrors\": true,\r\n  \"certificateStoreRootPath\": \"pki\",\r\n  \"operationTimeoutMilliseconds\": 6000000,\r\n  \"defaultSessionTimeoutMilliseconds\": -1,\r\n  \"minSubscriptionLifetimeMilliseconds\": -1,\r\n  \"maxStringLength\": 2147483647,\r\n  \"maxByteStringLength\": 2147483647,\r\n  \"maxArrayLength\": 65535,\r\n  \"maxMessageSize\": 419430400,\r\n  \"maxBufferSize\": 65535,\r\n  \"channelLifetime\": -1,\r\n  \"securityTokenLifetime\": -1,\r\n  \"disableHiResClock\": true,\r\n  \"defaultUseSecurity\": false,\r\n  \"defaultUseAnonymous\": true,\r\n  \"defaultSecurityPolicy\": \"\",\r\n  \"defaultSecurityMode\": \"\"\r\n}", "기본 OPC 수집 옵션", 10, 3600, 1000, false, null });

            migrationBuilder.InsertData(
                table: "s_menu",
                columns: new[] { "menu_id", "action_name", "controller_name", "created_at", "icon", "is_enabled", "menu_code", "menu_name", "parent_menu_id", "show_in_menu", "sort_order", "url", "updated_at" },
                values: new object[,]
                {
                    { "DASHBOARD", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "layout-dashboard", true, "DASHBOARD", "DASHBOARD", null, true, 10, null, null },
                    { "DEVICE", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "cpu", true, "DEVICE", "DEVICE", null, true, 30, null, null },
                    { "OPC", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "network", true, "OPC", "OPC", null, true, 40, null, null },
                    { "OPC_OPTIONS", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sliders-horizontal", true, "OPC_OPTIONS", "OPC OPTIONS", null, true, 60, null, null },
                    { "OPTIONS", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "sliders-horizontal", true, "OPTIONS", "OPTIONS", null, true, 70, null, null },
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
                    { "DASH_CARD", "DBD2000", "Main", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "panel-top", true, "DASHBOARD.CARD", "카드 대시보드", "DASHBOARD", true, 12, "/main/dbd2000", null },
                    { "DASH_MAIN", "Index", "Main", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "home", true, "DASHBOARD.MAIN", "MAIN", "DASHBOARD", true, 11, "/", null },
                    { "DEVICE_GROUP", "DVC1020", "Device", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "folder-tree", true, "DEVICE.GROUP", "그룹 관리", "DEVICE", true, 33, "/device/dvc1020", null },
                    { "DEVICE_MANAGE", "DVC1000", "Device", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "server", true, "DEVICE.MANAGE", "디바이스 관리", "DEVICE", true, 31, "/device/dvc1000", null },
                    { "DEVICE_TAG", "DVC1010", "Device", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "tags", true, "DEVICE.TAG", "태그 관리", "DEVICE", true, 32, "/device/dvc1010", null },
                    { "OPC_COLLECT", "OPC1000", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "radio-tower", true, "OPC.COLLECT", "OPC 수집 관리", "OPC", true, 41, "/opc/opc1000", null },
                    { "OPTION_CARD", "OPT1000", "Option", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "panel-top", true, "OPTIONS.CARD", "카드 대시보드 옵션", "OPTIONS", true, 71, "/option/opt1000", null },
                    { "OPTION_CLIENT", "OPC1030", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "plug", true, "OPC_OPTIONS.CLIENT", "Client 옵션", "OPC_OPTIONS", true, 62, "/opc/opc1030", null },
                    { "OPTION_COLLECT", "OPC1020", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "settings-2", true, "OPC_OPTIONS.COLLECTOR", "Collector 옵션", "OPC_OPTIONS", true, 61, "/opc/opc1020", null },
                    { "OPTION_HISTORY", "OPC3000", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "history", true, "OPC_OPTIONS.HISTORY", "History 조회", "OPC_OPTIONS", true, 63, "/opc/opc3000", null },
                    { "OPTION_TSD", "OPC4000", "Opc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "database-zap", true, "OPC_OPTIONS.TIMESCALE", "Timescale 설정", "OPC_OPTIONS", true, 64, "/opc/opc4000", null },
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
                    { "RM0004", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "BizAuth", null },
                    { "RM0007", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASHBOARD", "TestAuth", null },
                    { "RM0010", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "DevAuth", null },
                    { "RM0013", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "BizAuth", null },
                    { "RM0016", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE", "TestAuth", null },
                    { "RM0019", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE", "DevAuth", null },
                    { "RM0023", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE", "BizAuth", null },
                    { "RM0027", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE", "TestAuth", null },
                    { "RM0031", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC", "DevAuth", null },
                    { "RM0033", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_OPTIONS", "DevAuth", null },
                    { "RM0038", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC", "BizAuth", null },
                    { "RM0040", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_OPTIONS", "BizAuth", null },
                    { "RM0045", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC", "TestAuth", null },
                    { "RM0047", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_OPTIONS", "TestAuth", null },
                    { "RM0052", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM", "DevAuth", null },
                    { "RM0054", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM", "BizAuth", null },
                    { "RM0056", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM", "TestAuth", null },
                    { "RM0058", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTIONS", "DevAuth", null },
                    { "RM0060", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTIONS", "BizAuth", null },
                    { "RM0062", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTIONS", "TestAuth", null }
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
                    { "RM0005", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "BizAuth", null },
                    { "RM0006", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "BizAuth", null },
                    { "RM0008", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_MAIN", "TestAuth", null },
                    { "RM0009", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DASH_CARD", "TestAuth", null },
                    { "RM0011", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "DevAuth", null },
                    { "RM0012", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "DevAuth", null },
                    { "RM0014", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "BizAuth", null },
                    { "RM0015", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "BizAuth", null },
                    { "RM0017", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_DATA", "TestAuth", null },
                    { "RM0018", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SERVICE_TREND", "TestAuth", null },
                    { "RM0020", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_MANAGE", "DevAuth", null },
                    { "RM0021", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_TAG", "DevAuth", null },
                    { "RM0022", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_GROUP", "DevAuth", null },
                    { "RM0024", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_MANAGE", "BizAuth", null },
                    { "RM0025", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_TAG", "BizAuth", null },
                    { "RM0026", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_GROUP", "BizAuth", null },
                    { "RM0028", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_MANAGE", "TestAuth", null },
                    { "RM0029", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_TAG", "TestAuth", null },
                    { "RM0030", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "DEVICE_GROUP", "TestAuth", null },
                    { "RM0032", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_COLLECT", "DevAuth", null },
                    { "RM0034", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_COLLECT", "DevAuth", null },
                    { "RM0035", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CLIENT", "DevAuth", null },
                    { "RM0036", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_HISTORY", "DevAuth", null },
                    { "RM0037", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_TSD", "DevAuth", null },
                    { "RM0039", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_COLLECT", "BizAuth", null },
                    { "RM0041", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_COLLECT", "BizAuth", null },
                    { "RM0042", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CLIENT", "BizAuth", null },
                    { "RM0043", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_HISTORY", "BizAuth", null },
                    { "RM0044", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_TSD", "BizAuth", null },
                    { "RM0046", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPC_COLLECT", "TestAuth", null },
                    { "RM0048", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_COLLECT", "TestAuth", null },
                    { "RM0049", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CLIENT", "TestAuth", null },
                    { "RM0050", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_HISTORY", "TestAuth", null },
                    { "RM0051", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_TSD", "TestAuth", null },
                    { "RM0053", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM_SVC", "DevAuth", null },
                    { "RM0055", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM_SVC", "BizAuth", null },
                    { "RM0057", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SYSTEM_SVC", "TestAuth", null },
                    { "RM0059", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CARD", "DevAuth", null },
                    { "RM0061", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CARD", "BizAuth", null },
                    { "RM0063", true, true, true, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "OPTION_CARD", "TestAuth", null }
                });

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
                name: "ix_opc_option_card_tag_id",
                table: "opc_option_card",
                column: "tag_id");

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
                name: "opc_option_card");

            migrationBuilder.DropTable(
                name: "s_new_no");

            migrationBuilder.DropTable(
                name: "s_role_menu");

            migrationBuilder.DropTable(
                name: "s_user_role");

            migrationBuilder.DropTable(
                name: "opc_tag");

            migrationBuilder.DropTable(
                name: "s_menu");

            migrationBuilder.DropTable(
                name: "s_role");

            migrationBuilder.DropTable(
                name: "s_user");

            migrationBuilder.DropTable(
                name: "opc_device");

            migrationBuilder.DropTable(
                name: "opc_group");

            migrationBuilder.DropTable(
                name: "opc_major_group");
        }
    }
}
