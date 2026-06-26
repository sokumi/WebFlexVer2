import $ from "jquery";

import { api } from "../../framework/common";
import { notify } from "../../framework/notify";
import { WebFlexGrid } from "../../components/grid/webflexGrid";
import {
    WebFlexTagRegisterPopup,
    type WebFlexTagRegisterNode,
    type WebFlexTagRegisterSaveNode
} from "../../components/webflexTagRegisterPopup";

import {
    numberFormatter,
    textFormatter
} from "../../components/grid/webflexGridFormatters";

type DeviceRowDto = {
    id: string;
    deviceName: string;
    deviceType: string;
    endpointUrl: string;
    isCollectEnabled: boolean;
    tagCount: number;
};

type DeviceTagSummaryDto = {
    deviceCount: number;
    nodeCount: number;
    variableNodeCount: number;
    tagCount: number;
    collectTagCount: number;
};

type BrowseNodeDto = WebFlexTagRegisterNode & {
    deviceId: string;
    accessLevel: string;
    engineeringUnit: string;
    children?: BrowseNodeDto[];
};

type SelectedNodeDto = WebFlexTagRegisterSaveNode;


type TagRowDto = {
    id: string;
    deviceId: string;
    groupId: string;
    tagName: string;
    displayName: string;
    nodeId: string;
    dataType: string;
    isCollectEnabled: boolean;
    saveToDatabase: boolean;
    samplingIntervalMs: number;
    queueSize: number;
    sortOrder: number;
    description: string;
};

type SaveRequest = {
    deviceId: string;
    nodes: SelectedNodeDto[];
};

type DeleteRequest = {
    ids: string[];
};


export default class Page {
     devices: DeviceRowDto[] = [];
     nodes: BrowseNodeDto[] = [];
     treeNodes: BrowseNodeDto[] = [];
     selectedDeviceId = "";
     selectedTagIds: string[] = [];
     tagRows: TagRowDto[] = [];

     tagGrid: WebFlexGrid<TagRowDto> | null = null;
     modalPopup: WebFlexTagRegisterPopup | null = null;
     drawerPopup: WebFlexTagRegisterPopup | null = null;

     init(): void {
        this.initTagGrid();
        this.initPopups();
        this.bindEvents();

        void this.loadDevices();
        void this.loadSummary();
    }

     initPopups(): void {
        this.modalPopup = new WebFlexTagRegisterPopup({
            selector: "#tagRegisterModal",
            cascadeCheck: true,
            widthPercent: 55,
            heightPercent: 72,
            saveButtonText: count => count > 0 ? `태그 등록 (${count})` : "태그 등록",
            onSave: nodes => this.saveTags(nodes)
        });

        this.drawerPopup = new WebFlexTagRegisterPopup({
            selector: "#tagRegisterDrawer",
            cascadeCheck: true,
            widthPercent: 60,
            heightPercent: 100,
            saveButtonText: count => count > 0 ? `태그 등록 (${count})` : "태그 등록",
            onSave: nodes => this.saveTags(nodes)
        });
    }

     bindEvents(): void {
        $("#selDevice").on("change", () => {
            this.selectedDeviceId = String($("#selDevice").val() ?? "");
            this.nodes = [];
            this.treeNodes = [];
            this.selectedTagIds = [];

            this.updateSelectedDeviceInfo();
            void this.tagGrid?.setData([]);
            this.updateTagGridFooter();

            if (this.selectedDeviceId.length > 0) {
                void this.loadTags();
                void this.loadSummary();
            } else {
                $("#lblTagCount").text("0개");
                $("#lblGridTagCount").text("0건");
            }
        });

        $("#btnSearch").on("click", () => {
            void this.loadTags();
        });

        $("#txtTagKeyword").on("keydown", event => {
            if (event.key === "Enter") {
                void this.loadTags();
            }
        });

        $("#btnOpenTagModal").on("click", () => {
            void this.openRegisterPopup("modal");
        });

        $("#btnOpenTagDrawer").on("click", () => {
            void this.openRegisterPopup("drawer");
        });

        $("#btnDelete").on("click", () => {
            void this.deleteTags();
        });

        $(document).on("keydown", event => {
            if (event.key === "Escape") {
                $(".wf-tag-popup.is-open [data-popup-close]").first().trigger("click");
            }
        });

        window.addEventListener("webflex:layoutChanged", () => {
            this.tagGrid?.refreshLayout();
        });
    }

     initTagGrid(): void {
        this.tagGrid = new WebFlexGrid<TagRowDto>({
            selector: "#gridTag",
            height: "100%",
            pagination: true,
            paginationSize: 20,
            selectableRows: false,
            placeholder: "등록된 태그가 없습니다.",
            columns: [
                {
                    title: "",
                    field: "id",
                    width: 48,
                    hozAlign: "center",
                    headerSort: false,
                    formatter: (cell: { getValue: () => unknown }) => {
                        const id = String(cell.getValue() ?? "");
                        const checked = this.selectedTagIds.includes(id) ? "checked" : "";
                        return `<input type="checkbox" class="wf-tag-check" data-id="${this.escapeHtml(id)}" ${checked} />`;
                    }
                },
                {
                    title: "태그코드",
                    field: "id",
                    width: 130,
                    formatter: textFormatter
                },
                {
                    title: "태그명",
                    field: "tagName",
                    minWidth: 220,
                    formatter: textFormatter
                },
                {
                    title: "NodeId",
                    field: "nodeId",
                    minWidth: 360,
                    formatter: textFormatter
                },
                {
                    title: "DataType",
                    field: "dataType",
                    width: 120,
                    formatter: (cell: { getValue: () => unknown }) => this.createDataTypeBadge(String(cell.getValue() ?? ""))
                },
                {
                    title: "설명",
                    field: "description",
                    minWidth: 220,
                    formatter: textFormatter
                },
                {
                    title: "수집",
                    field: "isCollectEnabled",
                    width: 80,
                    hozAlign: "center",
                    formatter: (cell: { getValue: () => boolean }) => this.createYn(cell.getValue())
                },
                {
                    title: "사용",
                    field: "saveToDatabase",
                    width: 80,
                    hozAlign: "center",
                    formatter: (cell: { getValue: () => boolean }) => this.createYn(cell.getValue())
                },
                {
                    title: "Sampling",
                    field: "samplingIntervalMs",
                    width: 110,
                    hozAlign: "right",
                    formatter: numberFormatter
                }
            ],
            onRowClick: row => {
                this.toggleTagSelection(row.id);
            }
        });

        $("#gridTag").on("change", ".wf-tag-check", event => {
            event.stopPropagation();

            const id = String($(event.currentTarget).data("id") ?? "");
            this.toggleTagSelection(id);
        });
    }

     async loadDevices(): Promise<void> {
        try {
            const result = await api.get<DeviceRowDto[]>({
                url: "/test/devicetag/devices"
            });

            if (!result.success || result.data == null) {
                notify.error(result.message ?? "디바이스 조회에 실패했습니다.");
                return;
            }

            this.devices = result.data;

            const $sel = $("#selDevice");
            $sel.empty();
            $sel.append(`<option value="">디바이스 선택</option>`);

            for (const device of this.devices) {
                $sel.append(
                    `<option value="${this.escapeHtml(device.id)}">${this.escapeHtml(device.deviceName)} (${this.escapeHtml(device.deviceType)})</option>`
                );
            }

            if (this.devices.length > 0) {
                this.selectedDeviceId = this.devices[0].id;
                $sel.val(this.selectedDeviceId);
                this.updateSelectedDeviceInfo();
                await this.loadTags();
                await this.loadSummary();
            }
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }

     async loadSummary(): Promise<void> {
        try {
            const result = await api.get<DeviceTagSummaryDto>({
                url: `/test/devicetag/summary?deviceId=${encodeURIComponent(this.selectedDeviceId)}`
            });

            if (!result.success || result.data == null) {
                notify.error(result.message ?? "요약 조회에 실패했습니다.");
                return;
            }

            $("#lblTagCount").text(`${result.data.tagCount.toLocaleString()}개`);
            $("#lblGridTagCount").text(`${result.data.tagCount.toLocaleString()}건`);
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "요약 조회 중 오류가 발생했습니다.");
        }
    }

     async loadTags(): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            notify.warning("디바이스를 선택해 주세요.");
            return;
        }

        try {
            this.tagGrid?.showLoading();

            const keyword = encodeURIComponent(String($("#txtTagKeyword").val() ?? "").trim());

            const result = await api.get<TagRowDto[]>({
                url: `/test/devicetag/list?deviceId=${encodeURIComponent(this.selectedDeviceId)}&keyword=${keyword}&onlyCollect=false`
            });

            if (!result.success || result.data == null) {
                notify.error(result.message ?? "태그 조회에 실패했습니다.");
                return;
            }

            this.selectedTagIds = [];
            this.tagRows = result.data;
            await this.tagGrid?.setData(this.tagRows);

            this.updateTagGridFooter();
            await this.loadSummary();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 조회 중 오류가 발생했습니다.");
        } finally {
            this.tagGrid?.hideLoading();
        }
    }

     async openRegisterPopup(type: "modal" | "drawer"): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            notify.warning("디바이스를 선택해 주세요.");
            return;
        }

        if (this.nodes.length === 0) {
            const success = await this.browseNodes();

            if (!success) {
                return;
            }
        }

        const device = this.getSelectedDevice();

        const options = {
            deviceName: device == null ? "-" : `${device.deviceName} (${device.deviceType})`,
            nodes: this.nodes,
            treeNodes: this.treeNodes
        };

        if (type === "modal") {
            this.modalPopup?.open(options);
        } else {
            this.drawerPopup?.open(options);
        }
    }

     async browseNodes(): Promise<boolean> {
        try {
            notify.info("OPC 노드를 조회하고 있습니다.");

            const result = await api.get<BrowseNodeDto[]>({
                url: `/test/devicetag/browse?deviceId=${encodeURIComponent(this.selectedDeviceId)}&onlyCollectable=true`
            });

            if (!result.success || result.data == null) {
                notify.error(result.message ?? "노드 조회에 실패했습니다.");
                return false;
            }

            this.nodes = result.data;
            this.treeNodes = this.buildTree(this.nodes);

            notify.success("OPC 노드를 조회했습니다.");
            return true;
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "노드 조회 중 오류가 발생했습니다.");
            return false;
        }
    }

     async saveTags(nodes: SelectedNodeDto[]): Promise<boolean> {
        const request: SaveRequest = {
            deviceId: this.selectedDeviceId,
            nodes
        };

        try {
            const result = await api.post<unknown, SaveRequest>({
                url: "/test/devicetag/save",
                data: request
            });

            if (!result.success) {
                notify.error(result.message ?? "태그 저장에 실패했습니다.");
                return false;
            }

            notify.success(result.message ?? "태그가 저장되었습니다.");

            this.nodes = [];
            this.treeNodes = [];

            await this.loadTags();
            await this.loadSummary();

            return true;
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 저장 중 오류가 발생했습니다.");
            return false;
        }
    }

     async deleteTags(): Promise<void> {
        if (this.selectedTagIds.length === 0) {
            notify.warning("삭제할 태그를 선택해 주세요.");
            return;
        }

        if (!confirm(`${this.selectedTagIds.length}개의 태그를 삭제하시겠습니까?`)) {
            return;
        }

        const request: DeleteRequest = {
            ids: this.selectedTagIds
        };

        try {
            const result = await api.post<unknown, DeleteRequest>({
                url: "/test/devicetag/delete",
                data: request
            });

            if (!result.success) {
                notify.error(result.message ?? "태그 삭제에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "태그가 삭제되었습니다.");

            this.selectedTagIds = [];
            await this.loadTags();
            await this.loadSummary();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 삭제 중 오류가 발생했습니다.");
        }
    }

     buildTree(nodes: BrowseNodeDto[]): BrowseNodeDto[] {
        const map = new Map<string, BrowseNodeDto>();

        for (const node of nodes) {
            map.set(node.nodeId, {
                ...node,
                children: []
            });
        }

        const roots: BrowseNodeDto[] = [];

        for (const node of map.values()) {
            if (node.parentNodeId && map.has(node.parentNodeId)) {
                map.get(node.parentNodeId)?.children?.push(node);
            } else {
                roots.push(node);
            }
        }

        return roots;
    }

     toggleTagSelection(id: string): void {
        if (id.length === 0) {
            return;
        }

        if (this.selectedTagIds.includes(id)) {
            this.selectedTagIds = this.selectedTagIds.filter(x => x !== id);
        } else {
            this.selectedTagIds.push(id);
        }

        this.tagGrid?.redraw(true);
        this.updateTagGridFooter();
    }

     updateSelectedDeviceInfo(): void {
        const device = this.getSelectedDevice();

        if (device == null) {
            $("#lblDeviceStatus")
                .removeClass("is-connected")
                .addClass("is-empty")
                .find("span:last")
                .text("미선택");

            $("#lblEndpoint").text("Endpoint 없음");
            return;
        }

        $("#lblDeviceStatus")
            .removeClass("is-empty")
            .find("span:last")
            .text(device.isCollectEnabled ? "연결됨" : "수집 중지");

        $("#lblEndpoint").text(device.endpointUrl || "Endpoint 없음");
        $("#lblTagCount").text(`${device.tagCount.toLocaleString()}개`);
    }

     updateTagGridFooter(): void {
        $("#lblGridSummary").text(`총 ${this.tagRows.length.toLocaleString()}건`);
        $("#lblGridTagCount").text(`${this.tagRows.length.toLocaleString()}건`);
        $("#lblSelectedTag").text(
            this.selectedTagIds.length === 0
                ? "선택 없음"
                : `${this.selectedTagIds.length.toLocaleString()}개 선택`
        );
    }

     getSelectedDevice(): DeviceRowDto | null {
        return this.devices.find(x => x.id === this.selectedDeviceId) ?? null;
    }

     createYn(value: boolean): string {
        return value
            ? `<span class="wf-tag-yn">Y</span>`
            : `<span class="wf-tag-yn off">N</span>`;
    }

     createDataTypeBadge(dataType: string): string {
        const value = String(dataType ?? "");
        const lower = value.toLowerCase();

        let className = "default";

        if (lower.includes("string")) {
            className = "string";
        } else if (lower.includes("bool")) {
            className = "boolean";
        } else if (lower.includes("int") || lower.includes("float") || lower.includes("double") || lower.includes("decimal")) {
            className = "number";
        }

        return `<span class="wf-tag-badge ${className}">${this.escapeHtml(value || "-")}</span>`;
    }

     escapeHtml(value: string | null | undefined): string {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
}