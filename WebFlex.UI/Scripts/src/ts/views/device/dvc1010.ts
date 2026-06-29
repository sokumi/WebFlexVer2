import $ from "jquery";

import { api, escapeHtml } from "../../framework/common";
import { notify } from "../../framework/notify";
import { WebFlexGrid } from "../../components/grid/webflexGrid";
import { WebFlexPopup } from "../../components/webflexPopup";
import { WebFlexCheckTree, type WebFlexTreeItem } from "../../components/webflexCheckTree";
import {
    boolFormatter,
    numberFormatter,
    textFormatter
} from "../../components/grid/webflexGridFormatters";

type PopupType = "modal" | "drawer";
type DeviceConnectionStatus = "empty" | "checking" | "connected" | "failed";

export default class Page {
    devices: any[] = [];
    rows: any[] = [];
    selectedDeviceId = "";
    selectedTagIds: string[] = [];

    browseRows: any[] = [];
    treeItems: WebFlexTreeItem[] = [];
    selectedTreeItems: WebFlexTreeItem[] = [];

    connectionCheckSeq = 0;

    tagGrid: WebFlexGrid<any> | null = null;

    modalPopup: WebFlexPopup | null = null;
    drawerPopup: WebFlexPopup | null = null;

    modalTree: WebFlexCheckTree | null = null;
    drawerTree: WebFlexCheckTree | null = null;

    activePopupType: PopupType | null = null;

    init(): void {
        this.initTagGrid();
        this.initPopups();
        this.bindEvents();

        void this.loadDevices();
        void this.loadSummary();
    }

    initTagGrid(): void {
        this.tagGrid = WebFlexGrid
            .create<any>("#gridTag")
            .height("100%")
            .pagination(20)
            .selectableRows(false)
            .placeholder("등록된 태그가 없습니다.")
            .columns([
                {
                    title: `<input type="checkbox" class="wf-tag-check-all" />`,
                    field: "id",
                    width: 48,
                    hozAlign: "center",
                    headerSort: false,
                    formatter: (cell: any) => {
                        const id = String(cell.getValue() ?? "");
                        const checked = this.selectedTagIds.includes(id) ? "checked" : "";
                        return `<input type="checkbox" class="wf-tag-check" data-id="${escapeHtml(id)}" ${checked} />`;
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
                    formatter: (cell: any) => this.createDataTypeBadge(String(cell.getValue() ?? ""))
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
                    formatter: boolFormatter
                },
                {
                    title: "DB저장",
                    field: "saveToDatabase",
                    width: 90,
                    hozAlign: "center",
                    formatter: boolFormatter
                },
                {
                    title: "Sampling",
                    field: "samplingIntervalMs",
                    width: 110,
                    hozAlign: "right",
                    formatter: numberFormatter
                }
            ])
            .onRowClick(row => {
                this.toggleTagSelection(row.id);
            })
            .build();

        $("#gridTag").on("change", ".wf-tag-check", event => {
            event.stopPropagation();

            const id = String($(event.currentTarget).data("id") ?? "");
            this.toggleTagSelection(id);
        });

        $("#gridTag").on("change", ".wf-tag-check-all", event => {
            event.stopPropagation();

            const checked = $(event.currentTarget).prop("checked") === true;
            this.toggleAllTagSelection(checked);
        });
    }

    initPopups(): void {
        this.modalPopup = new WebFlexPopup({
            selector: "#tagRegisterModal",
            widthPercent: 55,
            heightPercent: 72
        });

        this.drawerPopup = new WebFlexPopup({
            selector: "#tagRegisterDrawer",
            widthPercent: 60,
            heightPercent: 100
        });

        this.modalTree = new WebFlexCheckTree({
            selector: "#tagRegisterModal [data-tree-host]",
            cascadeCheck: true,
            classPrefix: "wf-tag-tree",
            onSelectionChanged: items => {
                if (this.activePopupType === "modal") {
                    this.syncSelectedTreeItems(items);
                }
            }
        });

        this.drawerTree = new WebFlexCheckTree({
            selector: "#tagRegisterDrawer [data-tree-host]",
            cascadeCheck: true,
            classPrefix: "wf-tag-tree",
            onSelectionChanged: items => {
                if (this.activePopupType === "drawer") {
                    this.syncSelectedTreeItems(items);
                }
            }
        });
    }

    bindEvents(): void {
        $("#selDevice").on("change", () => {
            this.selectedDeviceId = String($("#selDevice").val() ?? "");
            this.browseRows = [];
            this.treeItems = [];
            this.selectedTagIds = [];

            void this.tagGrid?.setData([]);
            this.updateTagGridFooter();
            this.updateSelectedDeviceInfo();

            if (this.selectedDeviceId.length > 0) {
                void this.checkDeviceConnection();
                void this.loadTags();
                void this.loadSummary();
            } else {
                this.setDeviceConnectionStatus("empty");
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

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-select-all]", () => {
            const tree = this.getActiveTree();

            if (tree == null) {
                return;
            }

            tree.toggleAll(!tree.isAllSelected());
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("change", "[data-check-all]", event => {
            const checked = $(event.currentTarget).prop("checked") === true;

            $(`${this.getActiveRootSelector()} [data-row-check]`).prop("checked", checked);
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-collect-on]", () => {
            $(`${this.getActiveRootSelector()} [data-collect-check]:checked, ${this.getActiveRootSelector()} [data-row-check]:checked`)
                .each((_, el) => {
                    const id = String($(el).data("id") ?? "");
                    $(`${this.getActiveRootSelector()} [data-collect-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", true);
                });
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-collect-off]", () => {
            $(`${this.getActiveRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getActiveRootSelector()} [data-collect-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", false);
            });
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-use-on]", () => {
            $(`${this.getActiveRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getActiveRootSelector()} [data-enabled-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", true);
            });
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-use-off]", () => {
            $(`${this.getActiveRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getActiveRootSelector()} [data-enabled-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", false);
            });
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-remove-row]", event => {
            const id = String($(event.currentTarget).data("id") ?? "");
            const tree = this.getActiveTree();

            if (tree == null) {
                return;
            }

            tree.setSelectedIds(
                tree.getSelectedIds().filter(x => x !== id)
            );
        });

        $("#tagRegisterModal, #tagRegisterDrawer").on("click", "[data-save]", () => {
            void this.saveSelectedTags();
        });

        window.addEventListener("webflex:layoutChanged", () => {
            this.tagGrid?.refreshLayout();
        });
    }

    async loadDevices(): Promise<void> {
        try {
            const result = await api.get({
                url: "/device/tag/devices"
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
                    `<option value="${escapeHtml(device.id)}">${escapeHtml(device.deviceName)} (${escapeHtml(device.deviceType)})</option>`
                );
            }

            if (this.devices.length > 0) {
                this.selectedDeviceId = this.devices[0].id;
                $sel.val(this.selectedDeviceId);

                this.updateSelectedDeviceInfo();
                void this.checkDeviceConnection();

                await this.loadTags();
                await this.loadSummary();
            } else {
                this.setDeviceConnectionStatus("empty");
            }
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }

    async loadSummary(): Promise<void> {
        try {
            const result = await api.get({
                url: `/device/tag/summary?deviceId=${encodeURIComponent(this.selectedDeviceId)}`
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

            const result = await api.get({
                url: `/device/tag/list?deviceId=${encodeURIComponent(this.selectedDeviceId)}&keyword=${keyword}&onlyCollect=false`
            });

            if (!result.success || result.data == null) {
                notify.error(result.message ?? "태그 조회에 실패했습니다.");
                return;
            }

            this.selectedTagIds = [];
            this.rows = result.data;

            await this.tagGrid?.setData(this.rows);

            this.updateTagGridFooter();
            this.updateTagCheckAllState();
            await this.loadSummary();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 조회 중 오류가 발생했습니다.");
        } finally {
            this.tagGrid?.hideLoading();
        }
    }

    async openRegisterPopup(type: PopupType): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            notify.warning("디바이스를 선택해 주세요.");
            return;
        }

        if (this.browseRows.length === 0) {
            const success = await this.browseNodes();

            if (!success) {
                return;
            }
        }

        this.activePopupType = type;
        this.selectedTreeItems = [];

        const device = this.getSelectedDevice();
        const rootSelector = this.getRootSelector(type);
        const tree = this.getTree(type);

        $(rootSelector).find("[data-device-name]").text(
            device == null ? "-" : `${device.deviceName} (${device.deviceType})`
        );

        tree?.setItems(this.treeItems);
        this.renderSelectedTable(type);
        this.updatePopupCount(type);

        if (type === "modal") {
            this.modalPopup?.open();
        } else {
            this.drawerPopup?.open();
        }

        this.refreshIcons();
    }

    async browseNodes(): Promise<boolean> {
        try {
            notify.info("OPC 노드를 조회하고 있습니다.");

            const result = await api.get({
                url: `/device/tag/browse?deviceId=${encodeURIComponent(this.selectedDeviceId)}&onlyCollectable=true`
            });

            if (!result.success || result.data == null) {
                notify.error(result.message ?? "노드 조회에 실패했습니다.");
                return false;
            }

            this.browseRows = result.data;
            this.treeItems = this.browseRows.map(x => ({
                id: x.nodeId,
                parentId: x.parentNodeId,
                text: x.displayName,
                tooltip: x.nodeId,
                selectable: x.nodeClass === "Variable",
                data: x
            }));

            notify.success("OPC 노드를 조회했습니다.");
            return true;
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "노드 조회 중 오류가 발생했습니다.");
            return false;
        }
    }

    syncSelectedTreeItems(items: WebFlexTreeItem[]): void {
        this.selectedTreeItems = items;
        this.renderSelectedTable(this.activePopupType ?? "modal");
        this.updatePopupCount(this.activePopupType ?? "modal");
    }

    renderSelectedTable(type: PopupType): void {
        const rootSelector = this.getRootSelector(type);
        const $host = $(`${rootSelector} [data-selected-host]`);
        const $empty = $(`${rootSelector} [data-empty-host]`);

        $host.empty();

        if (this.selectedTreeItems.length === 0) {
            $empty.removeClass("d-none");
        } else {
            $empty.addClass("d-none");
        }

        for (const item of this.selectedTreeItems) {
            const row = item.data ?? {};
            const id = escapeHtml(item.id);
            const displayName = escapeHtml(row.displayName ?? item.text ?? item.id);
            const dataType = escapeHtml(row.dataType ?? "");
            const description = escapeHtml(row.description ?? "");

            $host.append(`
                <tr>
                    <td class="wf-check-col">
                        <input type="checkbox"
                               class="form-check-input"
                               data-row-check
                               data-id="${id}"
                               checked />
                    </td>
                    <td>
                        <div class="wf-node-origin">
                            <strong>${displayName}</strong>
                            <span>${id}</span>
                        </div>
                    </td>
                    <td>
                        <input type="text"
                               class="form-control"
                               data-tag-name
                               data-id="${id}"
                               value="${displayName}" />
                    </td>
                    <td>${this.createDataTypeBadge(dataType)}</td>
                    <td>
                        <input type="text"
                               class="form-control"
                               data-description
                               data-id="${id}"
                               value="${description}"
                               placeholder="설명 (선택)" />
                    </td>
                    <td class="text-center">
                        <input type="checkbox"
                               class="form-check-input"
                               data-collect-check
                               data-id="${id}"
                               checked />
                    </td>
                    <td class="text-center">
                        <input type="checkbox"
                               class="form-check-input"
                               data-enabled-check
                               data-id="${id}"
                               checked />
                    </td>
                    <td class="text-center">
                        <button type="button"
                                class="wf-row-remove"
                                data-remove-row
                                data-id="${id}">×</button>
                    </td>
                </tr>
            `);
        }

        this.updatePopupCount(type);
    }

    async saveSelectedTags(): Promise<void> {
        const type = this.activePopupType ?? "modal";
        const rootSelector = this.getRootSelector(type);

        if (this.selectedTreeItems.length === 0) {
            notify.warning("등록할 노드를 선택해 주세요.");
            return;
        }

        const nodes = this.selectedTreeItems.map(item => {
            const row = item.data ?? {};
            const selectorId = this.escapeSelector(item.id);

            return {
                nodeId: item.id,
                tagName: String($(`${rootSelector} [data-tag-name][data-id="${selectorId}"]`).val() ?? "").trim(),
                nodeClass: row.nodeClass,
                dataType: row.dataType,
                description: String($(`${rootSelector} [data-description][data-id="${selectorId}"]`).val() ?? "").trim(),
                isCollectEnabled: $(`${rootSelector} [data-collect-check][data-id="${selectorId}"]`).prop("checked") === true,
                saveToDatabase: true,
                showOnDashboard: false,
                isEnabled: $(`${rootSelector} [data-enabled-check][data-id="${selectorId}"]`).prop("checked") === true
            };
        });

        const invalid = nodes.find(x => x.tagName.length === 0);

        if (invalid != null) {
            notify.warning("태그명을 입력해 주세요.");
            return;
        }

        try {
            const result = await api.post({
                url: "/device/tag/save",
                data: {
                    deviceId: this.selectedDeviceId,
                    nodes
                }
            });

            if (!result.success) {
                notify.error(result.message ?? "태그 저장에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "태그가 저장되었습니다.");

            this.browseRows = [];
            this.treeItems = [];

            if (type === "modal") {
                this.modalPopup?.close();
            } else {
                this.drawerPopup?.close();
            }

            await this.loadTags();
            await this.loadSummary();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 저장 중 오류가 발생했습니다.");
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

        try {
            const result = await api.post({
                url: "/device/tag/delete",
                data: {
                    ids: this.selectedTagIds
                }
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
        this.updateTagCheckAllState();
    }

    toggleAllTagSelection(checked: boolean): void {
        if (checked) {
            this.selectedTagIds = this.rows
                .map(x => String(x.id ?? ""))
                .filter(x => x.length > 0);
        } else {
            this.selectedTagIds = [];
        }

        this.tagGrid?.redraw(true);
        this.updateTagGridFooter();
        this.updateTagCheckAllState();
    }

    updateTagCheckAllState(): void {
        const $checkAll = $("#gridTag .wf-tag-check-all");

        if ($checkAll.length === 0) {
            return;
        }

        const totalCount = this.rows.length;
        const selectedCount = this.selectedTagIds.length;

        $checkAll.prop("checked", totalCount > 0 && selectedCount === totalCount);
        $checkAll.prop("indeterminate", selectedCount > 0 && selectedCount < totalCount);
    }

    updateTagGridFooter(): void {
        $("#lblGridSummary").text(`총 ${this.rows.length.toLocaleString()}건`);
        $("#lblGridTagCount").text(`${this.rows.length.toLocaleString()}건`);
        $("#lblSelectedTag").text(
            this.selectedTagIds.length === 0
                ? "선택 없음"
                : `${this.selectedTagIds.length.toLocaleString()}개 선택`
        );
    }

    updatePopupCount(type: PopupType): void {
        const rootSelector = this.getRootSelector(type);
        const tree = this.getTree(type);
        const variableCount = tree?.getSelectableItems().length ?? 0;
        const selectedCount = this.selectedTreeItems.length;
        const checkedCount = $(`${rootSelector} [data-row-check]:checked`).length;

        $(`${rootSelector} [data-selected-count]`).text(`${selectedCount}/${variableCount}`);
        $(`${rootSelector} [data-selected-text]`).text(`${checkedCount}/${selectedCount}개 선택`);
        $(`${rootSelector} [data-loaded-text]`).text(`총 ${variableCount.toLocaleString()}개 노드 로드됨`);
        $(`${rootSelector} [data-loaded-count]`)
            .toggleClass("d-none", variableCount === 0)
            .text(`${variableCount.toLocaleString()}개 로드됨`);
        $(`${rootSelector} [data-save-text]`).text(selectedCount > 0 ? `태그 등록 (${selectedCount})` : "태그 등록");

        const $checkAll = $(`${rootSelector} [data-check-all]`);
        $checkAll.prop("checked", selectedCount > 0 && checkedCount === selectedCount);
        $checkAll.prop("indeterminate", checkedCount > 0 && checkedCount < selectedCount);
    }

    updateSelectedDeviceInfo(): void {
        const device = this.getSelectedDevice();

        if (device == null) {
            this.setDeviceConnectionStatus("empty");
            $("#lblEndpoint").text("Endpoint 없음");
            return;
        }

        this.setDeviceConnectionStatus("checking");
        $("#lblEndpoint").text(device.endpointUrl || "Endpoint 없음");
        $("#lblTagCount").text(`${device.tagCount.toLocaleString()}개`);
    }

    async checkDeviceConnection(): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            this.setDeviceConnectionStatus("empty");
            return;
        }

        const seq = ++this.connectionCheckSeq;
        const deviceId = this.selectedDeviceId;

        this.setDeviceConnectionStatus("checking");

        try {
            const result = await api.get({
                url: `/device/tag/check-connection?deviceId=${encodeURIComponent(deviceId)}`
            });

            if (seq !== this.connectionCheckSeq || deviceId !== this.selectedDeviceId) {
                return;
            }

            if (!result.success || result.data == null) {
                this.setDeviceConnectionStatus("failed", result.message ?? "연결 확인 실패");
                return;
            }

            if (result.data.connected) {
                this.setDeviceConnectionStatus("connected");
            } else {
                this.setDeviceConnectionStatus("failed", result.data.errorMessage || result.message || "연결 실패");
            }
        } catch (e) {
            if (seq !== this.connectionCheckSeq || deviceId !== this.selectedDeviceId) {
                return;
            }

            this.setDeviceConnectionStatus(
                "failed",
                e instanceof Error ? e.message : "연결 확인 중 오류가 발생했습니다."
            );
        }
    }

    setDeviceConnectionStatus(status: DeviceConnectionStatus, message?: string): void {
        const $status = $("#lblDeviceStatus");

        $status
            .removeClass("is-empty is-checking is-connected is-failed")
            .addClass(`is-${status}`);

        if (status === "empty") {
            $status.find("span:last").text("미선택");
            $status.attr("title", "");
            return;
        }

        if (status === "checking") {
            $status.find("span:last").text("확인 중");
            $status.attr("title", "디바이스 연결 상태를 확인하고 있습니다.");
            return;
        }

        if (status === "connected") {
            $status.find("span:last").text("연결됨");
            $status.attr("title", "OPC 서버에 연결되었습니다.");
            return;
        }

        $status.find("span:last").text("연결 실패");
        $status.attr("title", message ?? "OPC 서버 연결에 실패했습니다.");
    }

    getSelectedDevice(): any | null {
        return this.devices.find(x => x.id === this.selectedDeviceId) ?? null;
    }

    getRootSelector(type: PopupType): string {
        return type === "modal" ? "#tagRegisterModal" : "#tagRegisterDrawer";
    }

    getActiveRootSelector(): string {
        return this.getRootSelector(this.activePopupType ?? "modal");
    }

    getTree(type: PopupType): WebFlexCheckTree | null {
        return type === "modal" ? this.modalTree : this.drawerTree;
    }

    getActiveTree(): WebFlexCheckTree | null {
        return this.getTree(this.activePopupType ?? "modal");
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

        return `<span class="wf-tag-badge ${className}">${escapeHtml(value || "-")}</span>`;
    }

    refreshIcons(): void {
        const lucide = (window as any).lucide;

        if (lucide?.createIcons != null) {
            lucide.createIcons();
        }
    }

    escapeSelector(value: string): string {
        return value.replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
    }
}