import { api } from "../../framework/common";
import { notify } from "../../framework/notify";
import { WebFlexGrid } from "../../framework/grid/webflexGrid";
import {
    numberFormatter,
    textFormatter
} from "../../framework/grid/webflexGridFormatters";

type DeviceTypeDto = {
    value: string;
    text: string;
};

type DeviceRowDto = {
    id: string;
    deviceCode: string;
    deviceName: string;
    deviceType: string;
    deviceAddress: string;
    port: number;
    endpointUrl: string;
    isCollectEnabled: boolean;
    isEnabled: boolean;
    useSecurity: boolean;
    useAnonymous: boolean;
    userName: string;
    password: string;
    publishingIntervalMs: number;
    samplingIntervalMs: number;
    sortOrder: number;
    description: string;
    tagCount?: number | null;
};

type DeviceSaveRequest = {
    id?: string | null;
    deviceName: string;
    deviceType: string;
    deviceAddress: string;
    port: number;
    endpointUrl: string;
    isCollectEnabled: boolean;
    isEnabled: boolean;
    useSecurity: boolean;
    useAnonymous: boolean;
    userName: string;
    password: string;
    publishingIntervalMs: number;
    samplingIntervalMs: number;
    sortOrder: number;
    description: string;
};

type DeviceDeleteRequest = {
    id: string;
};

type EndpointPreviewDto = {
    endpointUrl: string;
};


export default class Page {
    private grid: WebFlexGrid<DeviceRowDto> | null = null;
    private rows: DeviceRowDto[] = [];
    private selectedId = "";

    public init(): void {
        this.initGrid();
        this.bindEvents();
        this.clearForm();

        void this.loadDeviceTypes();
        void this.loadList();

        window.addEventListener("webflex:layoutChanged", () => {
            this.grid?.redraw(true);
        });
    }

    private bindEvents(): void {
        $("#btnNew").on("click", () => {
            this.clearForm();
        });

        $("#btnSave").on("click", () => {
            void this.save();
        });

        $("#btnDelete").on("click", () => {
            void this.delete();
        });

        $("#btnSearch").on("click", () => {
            void this.loadList();
        });

        $("#txtGridKeyword").on("keydown", event => {
            if (event.key === "Enter") {
                void this.loadList();
            }
        });

        $("#txtGridKeyword").on("input", () => {
            this.applyClientFilter();
        });

        $("#btnPreviewEndpoint").on("click", () => {
            void this.previewEndpoint();
        });

        $("#selDeviceType, #txtDeviceAddress, #txtPort").on("change", () => {
            this.applyEndpointPlaceholder();
        });
    }

    private initGrid(): void {
        this.grid = new WebFlexGrid<DeviceRowDto>({
            selector: "#gridDevice",
            height: "100%",
            pagination: true,
            paginationSize: 12,
            selectableRows: 1,
            columns: [
                {
                    title: "코드",
                    field: "deviceCode",
                    width: 120,
                    formatter: textFormatter
                },
                {
                    title: "디바이스명",
                    field: "deviceName",
                    minWidth: 190,
                    formatter: textFormatter
                },
                {
                    title: "타입",
                    field: "deviceType",
                    width: 120,
                    formatter: (cell: { getValue: () => any; }) => {
                        const value = String(cell.getValue() ?? "");
                        const className = value === "OPCUA" ? "good" : "warning";
                        return `<span class="wf-status ${className}">${value}</span>`;
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
                    width: 100,
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
                    width: 90,
                    formatter: (cell: { getValue: () => boolean; }) => {
                        return cell.getValue() === true
                            ? `<span class="wf-bool-dot good">Y</span>`
                            : `<span class="wf-bool-dot muted">N</span>`;
                    }
                },
                {
                    title: "사용",
                    field: "isEnabled",
                    width: 90,
                    formatter: (cell: { getValue: () => boolean; }) => {
                        return cell.getValue() === true
                            ? `<span class="wf-bool-dot good">Y</span>`
                            : `<span class="wf-bool-dot muted">N</span>`;
                    }
                }
            ],
            onRowClick: row => {
                this.selectRow(row);
            }
        });
    }

    private async loadDeviceTypes(): Promise<void> {
        try {
            const result = await api.get<DeviceTypeDto[]>({
                url: "/test/device/types"
            });

            if (!result.success || result.data == null) {
                notify.warning(result.message ?? "디바이스 타입 조회에 실패했습니다.");
                return;
            }

            const $formType = $("#selDeviceType");
            $formType.empty();

            for (const item of result.data) {
                $formType.append(`<option value="${item.value}">${item.text}</option>`);
            }

            $formType.val("OPCUA");
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "디바이스 타입 조회 중 오류가 발생했습니다.");
        }
    }

    private async loadList(): Promise<void> {
        try {
            const result = await api.get<DeviceRowDto[]>({
                url: "/test/device/list"
            });

            if (!result.success) {
                notify.warning(result.message ?? "디바이스 목록 조회에 실패했습니다.");
                return;
            }

            this.rows = result.data ?? [];
            await this.applyClientFilter();

            notify.success("디바이스 목록을 조회했습니다.");
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "디바이스 목록 조회 중 오류가 발생했습니다.");
        }
    }

    private async applyClientFilter(): Promise<void> {
        const keyword = String($("#txtGridKeyword").val() ?? "").trim().toLowerCase();

        const filteredRows = keyword.length === 0
            ? this.rows
            : this.rows.filter(x =>
                String(x.deviceCode ?? "").toLowerCase().includes(keyword) ||
                String(x.deviceName ?? "").toLowerCase().includes(keyword) ||
                String(x.deviceAddress ?? "").toLowerCase().includes(keyword) ||
                String(x.endpointUrl ?? "").toLowerCase().includes(keyword)
            );

        await this.grid?.setData(filteredRows);
        this.updateGridSummary(filteredRows.length, this.rows.length);
    }

    private updateGridSummary(visibleCount: number, totalCount: number): void {
        $("#lblDeviceGridCount").text(`${totalCount.toLocaleString()}건`);
        $("#lblGridSummary").text(`총 ${totalCount.toLocaleString()}건 · ${visibleCount.toLocaleString()}건 표시`);
    }

    private selectRow(row: DeviceRowDto): void {
        console.log("selected device row", row);

        this.selectedId = row.id;

        $("#hidId").val(row.id);
        $("#txtDeviceCode").val(row.deviceCode);
        $("#txtDeviceName").val(row.deviceName);
        $("#selDeviceType").val(row.deviceType);
        $("#txtDeviceAddress").val(row.deviceAddress);
        $("#txtPort").val(row.port);
        $("#txtEndpointUrl").val(row.endpointUrl);
        $("#chkCollect").prop("checked", row.isCollectEnabled);
        $("#chkEnabled").prop("checked", row.isEnabled);
        $("#chkUseSecurity").prop("checked", row.useSecurity);
        $("#chkUseAnonymous").prop("checked", row.useAnonymous);
        $("#txtUserName").val(row.userName ?? "");
        $("#txtPassword").val(row.password ?? "");
        $("#txtPublishingIntervalMs").val(row.publishingIntervalMs ?? 1000);
        $("#txtSamplingIntervalMs").val(row.samplingIntervalMs ?? 1000);
        $("#txtSortOrder").val(row.sortOrder ?? 0);
        $("#txtDescription").val(row.description ?? "");

        this.setEditMode(row.deviceName);
        this.applyEndpointPlaceholder();
    }

    private clearForm(): void {
        this.selectedId = "";

        $("#hidId").val("");
        $("#txtDeviceCode").val("");
        $("#txtDeviceName").val("");
        $("#selDeviceType").val("OPCUA");
        $("#txtDeviceAddress").val("");
        $("#txtPort").val(4840);
        $("#txtEndpointUrl").val("");
        $("#chkCollect").prop("checked", true);
        $("#chkEnabled").prop("checked", true);
        $("#chkUseSecurity").prop("checked", false);
        $("#chkUseAnonymous").prop("checked", true);
        $("#txtUserName").val("");
        $("#txtPassword").val("");
        $("#txtPublishingIntervalMs").val(1000);
        $("#txtSamplingIntervalMs").val(1000);
        $("#txtSortOrder").val(0);
        $("#txtDescription").val("");

        this.setCreateMode();
        this.applyEndpointPlaceholder();
    }

    private setCreateMode(): void {
        $("#lblFormMode")
            .removeClass("edit")
            .addClass("create")
            .text("신규");

        $("#lblSelectedDevice").text("선택 없음");
    }

    private setEditMode(deviceName: string): void {
        $("#lblFormMode")
            .removeClass("create")
            .addClass("edit")
            .text("수정");

        $("#lblSelectedDevice").text(`${deviceName} 선택됨`);
    }

    private getFormData(): DeviceSaveRequest {
        return {
            id: this.selectedId || null,
            deviceName: String($("#txtDeviceName").val() ?? "").trim(),
            deviceType: String($("#selDeviceType").val() ?? "OPCUA"),
            deviceAddress: String($("#txtDeviceAddress").val() ?? "").trim(),
            port: Number($("#txtPort").val() ?? 0),
            endpointUrl: String($("#txtEndpointUrl").val() ?? "").trim(),
            isCollectEnabled: $("#chkCollect").prop("checked") === true,
            isEnabled: $("#chkEnabled").prop("checked") === true,
            useSecurity: $("#chkUseSecurity").prop("checked") === true,
            useAnonymous: $("#chkUseAnonymous").prop("checked") === true,
            userName: String($("#txtUserName").val() ?? "").trim(),
            password: String($("#txtPassword").val() ?? ""),
            publishingIntervalMs: Number($("#txtPublishingIntervalMs").val() ?? 1000),
            samplingIntervalMs: Number($("#txtSamplingIntervalMs").val() ?? 1000),
            sortOrder: Number($("#txtSortOrder").val() ?? 0),
            description: String($("#txtDescription").val() ?? "").trim()
        };
    }

    private validate(request: DeviceSaveRequest): string | null {
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

        return null;
    }

    private async save(): Promise<void> {
        const request = this.getFormData();
        const errorMessage = this.validate(request);

        if (errorMessage != null) {
            notify.warning(errorMessage);
            return;
        }

        try {
            const result = await api.post<{ id: string }, DeviceSaveRequest>({
                url: "/test/device/save",
                data: request
            });

            if (!result.success) {
                notify.warning(result.message ?? "저장에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "저장되었습니다.");

            await this.loadList();
            this.clearForm();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "저장 중 오류가 발생했습니다.");
        }
    }

    private async delete(): Promise<void> {
        if (this.selectedId.length === 0) {
            notify.warning("삭제할 디바이스를 선택해 주세요.");
            return;
        }

        if (!confirm("선택한 디바이스를 삭제하시겠습니까?")) {
            return;
        }

        const request: DeviceDeleteRequest = {
            id: this.selectedId
        };

        try {
            const result = await api.post<unknown, DeviceDeleteRequest>({
                url: "/test/device/delete",
                data: request
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

    private async previewEndpoint(): Promise<void> {
        const deviceType = encodeURIComponent(String($("#selDeviceType").val() ?? ""));
        const address = encodeURIComponent(String($("#txtDeviceAddress").val() ?? "").trim());
        const port = Number($("#txtPort").val() ?? 0);

        try {
            const result = await api.get<EndpointPreviewDto>({
                url: `/test/device/endpoint-preview?deviceType=${deviceType}&address=${address}&port=${port}`
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

    private applyEndpointPlaceholder(): void {
        const deviceType = String($("#selDeviceType").val() ?? "");
        const address = String($("#txtDeviceAddress").val() ?? "").trim();
        const port = Number($("#txtPort").val() ?? 0);

        if (deviceType === "OPCUA" && address.length > 0 && port > 0) {
            $("#txtEndpointUrl").attr("placeholder", `opc.tcp://${address}:${port}`);
            return;
        }

        $("#txtEndpointUrl").attr("placeholder", "비워두면 자동 생성");
    }
}