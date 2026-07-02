import $ from "jquery";

import { api, escapeHtml } from "../../framework/common";
import { notify } from "../../framework/notify";
import { WebFlexGrid } from "../../components/grid/webflexGrid";
import {
    boolFormatter,
    numberFormatter,
    textFormatter
} from "../../components/grid/webflexGridFormatters";

const DEFAULT_DEVICE_TYPE = "OPCUA";
const DEFAULT_PORT = 4840;
const DEFAULT_INTERVAL = 1000;

export default class Page {
    grid: any = null;
    rows: any[] = [];
    selectedId = "";

    init() {
        this.initGrid();
        this.bindEvents();
        this.clearForm();

        void this.loadDeviceTypes();
        void this.loadList();

        window.addEventListener("webflex:layoutChanged", () => {
            this.grid?.redraw(true);
        });
    }

    bindEvents() {
        $("#btnNew").on("click", () => this.clearForm());
        $("#btnSave").on("click", () => void this.save());
        $("#btnDelete").on("click", () => void this.delete());
        $("#btnSearch").on("click", () => void this.loadList());

        $("#txtGridKeyword").on("keydown", event => {
            if (event.key === "Enter") {
                void this.loadList();
            }
        });

        $("#txtGridKeyword").on("input", () => {
            void this.applyClientFilter();
        });

        $("#btnPreviewEndpoint").on("click", () => {
            void this.previewEndpoint();
        });

        $("#selDeviceType, #txtDeviceAddress, #txtPort").on("change input", () => {
            this.applyEndpointPlaceholder();
        });

        $("#chkUseSecurity").on("change", () => {
            this.syncSecurityFields();
        });

        $("#chkUseAnonymous").on("change", () => {
            this.syncAccountFields();
        });
    }

    initGrid() {
        this.grid = WebFlexGrid
            .create("#gridDevice")
            .height("100%")
            .pagination(12)
            .selectableRows(1)
            .placeholder("등록된 디바이스가 없습니다.")
            .columns([
                {
                    title: "코드",
                    field: "deviceCode",
                    width: 130,
                    formatter: textFormatter
                },
                {
                    title: "디바이스명",
                    field: "deviceName",
                    minWidth: 200,
                    formatter: textFormatter
                },
                {
                    title: "타입",
                    field: "deviceType",
                    width: 120,
                    formatter: (cell: any) => {
                        const value = String(cell.getValue() ?? "");
                        const className = value === "OPCUA" ? "good" : "warning";
                        return `<span class="wf-status ${className}">${escapeHtml(value || "-")}</span>`;
                    }
                },
                {
                    title: "주소",
                    field: "deviceAddress",
                    minWidth: 160,
                    formatter: textFormatter
                },
                {
                    title: "포트",
                    field: "port",
                    width: 90,
                    hozAlign: "right",
                    formatter: numberFormatter
                },
                {
                    title: "태그",
                    field: "tagCount",
                    width: 90,
                    hozAlign: "right",
                    formatter: numberFormatter
                },
                {
                    title: "수집",
                    field: "isCollectEnabled",
                    width: 80,
                    hozAlign: "center",
                    formatter: boolFormatter
                },
                {
                    title: "사용",
                    field: "isEnabled",
                    width: 80,
                    hozAlign: "center",
                    formatter: boolFormatter
                }
            ])
            .onRowClick(row => {
                this.selectRow(row);
            })
            .onRowDoubleClick(row => {
                this.selectRow(row);
                notify.info(`${row.deviceName} 상세를 열었습니다.`);
            })
            .build();
    }

    async loadDeviceTypes() {
        try {
            const result = await api.get({
                url: "/device/manage/types"
            });

            if (!result.success || result.data == null) {
                notify.warning(result.message ?? "디바이스 타입 조회에 실패했습니다.");
                return;
            }

            const $formType = $("#selDeviceType");
            $formType.empty();

            for (const item of result.data) {
                $formType.append(
                    `<option value="${escapeHtml(item.value)}">${escapeHtml(item.text)}</option>`
                );
            }

            $formType.val(DEFAULT_DEVICE_TYPE);
            this.applyEndpointPlaceholder();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "디바이스 타입 조회 중 오류가 발생했습니다.");
        }
    }

    async loadList() {
        try {
            this.grid?.showLoading("디바이스 목록 조회 중입니다...");

            const result = await api.get({
                url: "/device/manage/list"
            });

            if (!result.success) {
                notify.warning(result.message ?? "디바이스 목록 조회에 실패했습니다.");
                return;
            }

            this.rows = (result.data ?? []).map((x: any) => this.normalizeRow(x));
            await this.applyClientFilter();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "디바이스 목록 조회 중 오류가 발생했습니다.");
        } finally {
            this.grid?.hideLoading();
        }
    }

    async applyClientFilter() {
        const keyword = String($("#txtGridKeyword").val() ?? "").trim().toLowerCase();

        const filteredRows = keyword.length === 0
            ? this.rows
            : this.rows.filter(x =>
                String(x.deviceCode ?? "").toLowerCase().includes(keyword) ||
                String(x.deviceName ?? "").toLowerCase().includes(keyword) ||
                String(x.deviceType ?? "").toLowerCase().includes(keyword) ||
                String(x.deviceAddress ?? "").toLowerCase().includes(keyword) ||
                String(x.endpointUrl ?? "").toLowerCase().includes(keyword)
            );

        await this.grid?.setData(filteredRows);
        this.updateGridSummary(filteredRows.length, this.rows.length);
        this.updateSelectedAfterReload();
    }

    updateGridSummary(visibleCount: any, totalCount: any) {
        $("#lblDeviceGridCount").text(`${totalCount.toLocaleString()}건`);
        $("#lblGridSummary").text(`총 ${totalCount.toLocaleString()}건 · ${visibleCount.toLocaleString()}건 표시`);
    }

    updateSelectedAfterReload() {
        if (this.selectedId.length === 0) {
            return;
        }

        const selected = this.rows.find(x => x.id === this.selectedId);

        if (selected == null) {
            this.clearForm();
            return;
        }

        $("#lblSelectedDevice").text(`${selected.deviceName} 선택됨`);
    }

    selectRow(row: any) {
        const device = this.normalizeRow(row);

        this.selectedId = device.id;

        $("#hidId").val(device.id);
        $("#txtDeviceCode").val(device.deviceCode);
        $("#txtDeviceName").val(device.deviceName);
        $("#selDeviceType").val(device.deviceType || DEFAULT_DEVICE_TYPE);
        $("#txtDeviceAddress").val(device.deviceAddress);
        $("#txtPort").val(device.port ?? DEFAULT_PORT);
        $("#txtEndpointUrl").val(device.endpointUrl);
        $("#chkCollect").prop("checked", device.isCollectEnabled);
        $("#chkEnabled").prop("checked", device.isEnabled);
        $("#chkUseSecurity").prop("checked", device.useSecurity);
        $("#selSecurityMode").val(device.securityMode ?? "");
        $("#selSecurityPolicy").val(device.securityPolicy ?? "");
        $("#chkUseAnonymous").prop("checked", device.useAnonymous);
        $("#txtUserName").val(device.userName ?? "");
        $("#txtPassword").val(device.password ?? "");
        $("#txtPublishingIntervalMs").val(device.publishingIntervalMs ?? DEFAULT_INTERVAL);
        $("#txtSamplingIntervalMs").val(device.samplingIntervalMs ?? DEFAULT_INTERVAL);
        $("#txtDescription").val(device.description ?? "");

        this.setEditMode(device.deviceName);
        this.applyEndpointPlaceholder();
        this.syncSecurityFields();
        this.syncAccountFields();
    }

    clearForm() {
        this.selectedId = "";

        $("#hidId").val("");
        $("#txtDeviceCode").val("");
        $("#txtDeviceName").val("");
        $("#selDeviceType").val(DEFAULT_DEVICE_TYPE);
        $("#txtDeviceAddress").val("");
        $("#txtPort").val(DEFAULT_PORT);
        $("#txtEndpointUrl").val("");
        $("#chkCollect").prop("checked", true);
        $("#chkEnabled").prop("checked", true);
        $("#chkUseSecurity").prop("checked", false);
        $("#selSecurityMode").val("");
        $("#selSecurityPolicy").val("");
        $("#chkUseAnonymous").prop("checked", true);
        $("#txtUserName").val("");
        $("#txtPassword").val("");
        $("#txtPublishingIntervalMs").val(DEFAULT_INTERVAL);
        $("#txtSamplingIntervalMs").val(DEFAULT_INTERVAL);
        $("#txtDescription").val("");

        this.setCreateMode();
        this.applyEndpointPlaceholder();
        this.syncSecurityFields();
        this.syncAccountFields();
    }

    setCreateMode() {
        $("#lblFormMode")
            .removeClass("edit")
            .addClass("create")
            .text("신규");

        $("#lblSelectedDevice").text("선택 없음");
    }

    setEditMode(deviceName: any) {
        $("#lblFormMode")
            .removeClass("create")
            .addClass("edit")
            .text("수정");

        $("#lblSelectedDevice").text(`${deviceName} 선택됨`);
    }

    getFormData() {
        return {
            id: this.selectedId || null,
            deviceName: String($("#txtDeviceName").val() ?? "").trim(),
            deviceType: String($("#selDeviceType").val() ?? DEFAULT_DEVICE_TYPE).trim(),
            deviceAddress: String($("#txtDeviceAddress").val() ?? "").trim(),
            port: Number($("#txtPort").val() ?? 0),
            endpointUrl: String($("#txtEndpointUrl").val() ?? "").trim(),
            isCollectEnabled: $("#chkCollect").prop("checked") === true,
            isEnabled: $("#chkEnabled").prop("checked") === true,
            useSecurity: $("#chkUseSecurity").prop("checked") === true,
            securityMode: String($("#selSecurityMode").val() ?? "").trim(),
            securityPolicy: String($("#selSecurityPolicy").val() ?? "").trim(),
            useAnonymous: $("#chkUseAnonymous").prop("checked") === true,
            userName: String($("#txtUserName").val() ?? "").trim(),
            password: String($("#txtPassword").val() ?? ""),
            publishingIntervalMs: Number($("#txtPublishingIntervalMs").val() ?? DEFAULT_INTERVAL),
            samplingIntervalMs: Number($("#txtSamplingIntervalMs").val() ?? DEFAULT_INTERVAL),
            description: String($("#txtDescription").val() ?? "").trim()
        };
    }

    validate(request: any) {
        if (request.deviceName.length === 0) {
            return "디바이스명을 입력해 주세요.";
        }

        if (request.deviceType.length === 0) {
            return "디바이스 타입을 선택해 주세요.";
        }

        if (request.deviceAddress.length === 0) {
            return "주소를 입력해 주세요.";
        }

        if (request.port <= 0) {
            return "포트를 입력해 주세요.";
        }

        if (request.publishingIntervalMs <= 0) {
            return "Publishing Interval을 입력해 주세요.";
        }

        if (request.samplingIntervalMs <= 0) {
            return "Sampling Interval을 입력해 주세요.";
        }

        if (request.useSecurity && request.securityMode.length === 0) {
            return "Security Mode를 선택해 주세요.";
        }

        if (request.useSecurity && request.securityPolicy.length === 0) {
            return "Security Policy를 선택해 주세요.";
        }

        if (!request.useAnonymous && request.userName.length === 0) {
            return "익명 접속을 사용하지 않으면 사용자명을 입력해야 합니다.";
        }

        return null;
    }

    async save() {
        const request = this.getFormData();
        const errorMessage = this.validate(request);

        if (errorMessage != null) {
            notify.warning(errorMessage);
            return;
        }

        try {
            const result = await api.post({
                url: "/device/manage/save",
                data: request
            });

            if (!result.success) {
                notify.warning(result.message ?? "저장에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "저장되었습니다.");

            await this.loadList();

            const savedId = String(result.data?.id ?? "");
            if (savedId.length > 0) {
                const savedRow = this.rows.find(x => x.id === savedId);
                if (savedRow != null) {
                    this.selectRow(savedRow);
                    return;
                }
            }

            this.clearForm();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "저장 중 오류가 발생했습니다.");
        }
    }

    async delete() {
        if (this.selectedId.length === 0) {
            notify.warning("삭제할 디바이스를 선택해 주세요.");
            return;
        }

        if (!confirm("선택한 디바이스를 삭제하시겠습니까? 등록된 태그도 함께 삭제됩니다.")) {
            return;
        }

        try {
            const result = await api.post({
                url: "/device/manage/delete",
                data: [
                    {
                        id: this.selectedId
                    }
                ]
            });

            if (!result.success) {
                notify.warning(result.message ?? "삭제에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "삭제되었습니다.");

            await this.loadList();
            this.clearForm();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "삭제 중 오류가 발생했습니다.");
        }
    }

    async previewEndpoint() {
        const deviceType = encodeURIComponent(String($("#selDeviceType").val() ?? ""));
        const address = encodeURIComponent(String($("#txtDeviceAddress").val() ?? "").trim());
        const port = Number($("#txtPort").val() ?? 0);

        try {
            const result = await api.get({
                url: `/device/manage/endpoint-preview?deviceType=${deviceType}&address=${address}&port=${port}`
            });

            if (!result.success || result.data == null) {
                notify.warning(result.message ?? "Endpoint 생성에 실패했습니다.");
                return;
            }

            $("#txtEndpointUrl").val(result.data.endpointUrl);
            notify.success("Endpoint URL을 생성했습니다.");
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "Endpoint 생성 중 오류가 발생했습니다.");
        }
    }

    applyEndpointPlaceholder() {
        const deviceType = String($("#selDeviceType").val() ?? "");
        const address = String($("#txtDeviceAddress").val() ?? "").trim();
        const port = Number($("#txtPort").val() ?? 0);

        if (deviceType === "OPCUA" && address.length > 0 && port > 0) {
            $("#txtEndpointUrl").attr("placeholder", `opc.tcp://${address}:${port}`);
            return;
        }

        $("#txtEndpointUrl").attr("placeholder", "비워두면 자동 생성");
    }

    syncSecurityFields() {
        const useSecurity = $("#chkUseSecurity").prop("checked") === true;

        $("#selSecurityMode").prop("disabled", !useSecurity);
        $("#selSecurityPolicy").prop("disabled", !useSecurity);

        if (!useSecurity) {
            $("#selSecurityMode").val("");
            $("#selSecurityPolicy").val("");
        }
    }

    syncAccountFields() {
        const useAnonymous = $("#chkUseAnonymous").prop("checked") === true;

        $("#txtUserName").prop("disabled", useAnonymous);
        $("#txtPassword").prop("disabled", useAnonymous);

        if (useAnonymous) {
            $("#txtUserName").val("");
            $("#txtPassword").val("");
        }
    }

    normalizeRow(row: any) {
        return {
            ...row,
            id: this.readValue(row, "id", "ID", "deviceId", "DEVICE_ID") ?? "",
            deviceCode: this.readValue(row, "deviceCode", "deviceId", "id", "ID") ?? "",
            deviceName: this.readValue(row, "deviceName", "DEVICE_NAME") ?? "",
            deviceType: this.readValue(row, "deviceType", "DEVICE_TYPE") ?? "",
            deviceAddress: this.readValue(row, "deviceAddress", "DEVICE_ADDRESS") ?? "",
            port: this.readNumber(row, "port", "PORT"),
            endpointUrl: this.readValue(row, "endpointUrl", "ENDPOINT_URL") ?? "",
            isCollectEnabled: this.readBool(row, true, "isCollectEnabled", "IS_COLLECTENABLED"),
            isEnabled: this.readBool(row, true, "isEnabled", "IsEnabled"),
            useSecurity: this.readBool(row, false, "useSecurity", "USESECURITY"),
            securityMode: this.readValue(row, "securityMode", "SECURITYMODE") ?? "",
            securityPolicy: this.readValue(row, "securityPolicy", "SECURITYPOLICY") ?? "",
            useAnonymous: this.readBool(row, true, "useAnonymous", "USE_ANONYMOUS"),
            userName: this.readValue(row, "userName", "USER_NAME") ?? "",
            password: this.readValue(row, "password", "PASSWORD") ?? "",
            publishingIntervalMs: this.readNumber(row, "publishingIntervalMs", "PUBLISHINGINTERVALMS"),
            samplingIntervalMs: this.readNumber(row, "samplingIntervalMs", "SAMPLINGINTERVALMS"),
            description: this.readValue(row, "description", "DESCRIPTION") ?? "",
            tagCount: this.readNumber(row, "tagCount", "TAG_COUNT") ?? 0
        };
    }

    readValue(row: any, ...names: any[]) {
        if (row == null) {
            return null;
        }

        for (const name of names) {
            if (Object.prototype.hasOwnProperty.call(row, name)) {
                return row[name];
            }
        }

        const normalizedNames = names.map((x: any) => this.normalizeFieldName(x));

        for (const key of Object.keys(row)) {
            if (normalizedNames.includes(this.normalizeFieldName(key))) {
                return row[key];
            }
        }

        return null;
    }

    readBool(row: any, defaultValue: any, ...names: any[]) {
        const value = this.readValue(row, ...names);

        if (value == null) {
            return defaultValue;
        }

        if (typeof value === "boolean") {
            return value;
        }

        const text = String(value).trim().toLowerCase();

        if (text === "true" || text === "1" || text === "y" || text === "yes") {
            return true;
        }

        if (text === "false" || text === "0" || text === "n" || text === "no") {
            return false;
        }

        return defaultValue;
    }

    readNumber(row: any, ...names: any[]) {
        const value = this.readValue(row, ...names);

        if (value == null || value === "") {
            return null;
        }

        const numberValue = Number(value);

        return Number.isFinite(numberValue) ? numberValue : null;
    }

    normalizeFieldName(value: any) {
        return String(value ?? "")
            .replace(/_/g, "")
            .replace(/-/g, "")
            .toLowerCase();
    }
}
