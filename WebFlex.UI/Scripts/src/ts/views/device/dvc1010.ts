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

type DeviceConnectionStatus = "empty" | "checking" | "connected" | "failed";

const DATA_TYPE_OPTIONS = [
    "bit",
    "bool",
    "uint8",
    "int8",
    "uint16",
    "int16",
    "bcd16",
    "uint32",
    "int32",
    "float",
    "bcd32",
    "uint64",
    "int64",
    "double",
    "ascii",
    "utf8",
    "datetime",
    "timestamp(ms)",
    "timestamp(s)"
];

const PROTECT_TYPE_OPTIONS = [
    { value: "ReadOnly", text: "읽기 전용" },
    { value: "ReadWrite", text: "읽고 쓰기" },
    { value: "WriteOnly", text: "쓰기 전용" }
];

export default class Page {
    devices: any[] = [];
    rows: any[] = [];
    selectedDeviceId = "";
    selectedTagIds: string[] = [];
    selectedTagRow: any | null = null;

    browseRows: any[] = [];
    treeItems: WebFlexTreeItem[] = [];
    selectedTreeItems: WebFlexTreeItem[] = [];

    connectionCheckSeq = 0;

    tagGrid: WebFlexGrid<any> | null = null;
    drawerPopup: WebFlexPopup | null = null;
    drawerTree: WebFlexCheckTree | null = null;

    init(): void {
        $(".wf-tag-content").removeClass("has-detail");
        $("#tagDetailPanel").addClass("is-hidden");

        this.initTagGrid();
        this.initPopup();
        this.initDetailOptions();
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
                    title: "아이디",
                    field: "id",
                    width: 130,
                    formatter: textFormatter
                },
                {
                    title: "설명",
                    field: "description",
                    minWidth: 220,
                    formatter: textFormatter
                },
                {
                    title: "DataType",
                    field: "dataType",
                    width: 120,
                    formatter: (cell: any) => this.createDataTypeBadge(String(cell.getValue() ?? ""))
                },
                {
                    title: "권한",
                    field: "protectType",
                    width: 110,
                    formatter: (cell: any) => this.createProtectTypeBadge(String(cell.getValue() ?? "ReadOnly"))
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
            ])
            .onRowClick(row => {
                this.openTagDetail(row);
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

    initPopup(): void {
        this.drawerPopup = new WebFlexPopup({
            selector: "#tagRegisterDrawer",
            widthPercent: 70,
            heightPercent: 100
        });

        this.drawerTree = new WebFlexCheckTree({
            selector: "#tagRegisterDrawer [data-tree-host]",
            cascadeCheck: true,
            classPrefix: "wf-tag-tree",
            onSelectionChanged: items => {
                this.syncSelectedTreeItems(items);
            }
        });
    }

    initDetailOptions(): void {
        const $dataType = $("#selDetailDataType");
        $dataType.empty();

        for (const item of DATA_TYPE_OPTIONS) {
            $dataType.append(`<option value="${escapeHtml(item)}">${escapeHtml(item)}</option>`);
        }

        const $protectType = $("#selDetailProtectType");
        $protectType.empty();

        for (const item of PROTECT_TYPE_OPTIONS) {
            $protectType.append(`<option value="${escapeHtml(item.value)}">${escapeHtml(item.text)}</option>`);
        }
    }

    bindEvents(): void {
        $("#selDevice").on("change", () => {
            this.selectedDeviceId = String($("#selDevice").val() ?? "");
            this.browseRows = [];
            this.treeItems = [];
            this.selectedTagIds = [];
            this.closeTagDetail();

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

        $("#btnOpenTagDrawer").on("click", () => {
            void this.openRegisterPopup();
        });

        $("#btnDelete").on("click", () => {
            void this.deleteTags();
        });

        $("#btnCloseTagDetail").on("click", () => {
            this.closeTagDetail();
        });

        $("#btnSaveTagDetail").on("click", () => {
            void this.saveTagDetail();
        });

        $("#btnTestTagDetail").on("click", () => {
            void this.runExpressionTest();
        });

        $("#txtDetailSetting").on("keydown", event => {
            if (event.key !== "Tab") {
                return;
            }

            event.preventDefault();

            const element = event.currentTarget as HTMLTextAreaElement;
            const start = element.selectionStart;
            const end = element.selectionEnd;
            const value = element.value;

            element.value = `${value.substring(0, start)}    ${value.substring(end)}`;
            element.selectionStart = start + 4;
            element.selectionEnd = start + 4;
        });

        $("#tagRegisterDrawer").on("click", "[data-select-all]", () => {
            if (this.drawerTree == null) {
                return;
            }

            this.drawerTree.toggleAll(!this.drawerTree.isAllSelected());
        });

        $("#tagRegisterDrawer").on("change", "[data-check-all]", event => {
            const checked = $(event.currentTarget).prop("checked") === true;

            $(`${this.getRootSelector()} [data-row-check]`).prop("checked", checked);
            this.updatePopupCount();
        });

        $("#tagRegisterDrawer").on("change", "[data-row-check]", () => {
            this.updatePopupCount();
        });

        $("#tagRegisterDrawer").on("click", "[data-collect-on]", () => {
            $(`${this.getRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getRootSelector()} [data-collect-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", true);
            });
        });

        $("#tagRegisterDrawer").on("click", "[data-collect-off]", () => {
            $(`${this.getRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getRootSelector()} [data-collect-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", false);
            });
        });

        $("#tagRegisterDrawer").on("click", "[data-use-on]", () => {
            $(`${this.getRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getRootSelector()} [data-enabled-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", true);
            });
        });

        $("#tagRegisterDrawer").on("click", "[data-use-off]", () => {
            $(`${this.getRootSelector()} [data-row-check]:checked`).each((_, el) => {
                const id = String($(el).data("id") ?? "");
                $(`${this.getRootSelector()} [data-enabled-check][data-id="${this.escapeSelector(id)}"]`).prop("checked", false);
            });
        });

        $("#tagRegisterDrawer").on("click", "[data-remove-row]", event => {
            const id = String($(event.currentTarget).data("id") ?? "");

            if (this.drawerTree == null) {
                return;
            }

            this.drawerTree.setSelectedIds(
                this.drawerTree.getSelectedIds().filter(x => x !== id)
            );
        });

        $("#tagRegisterDrawer").on("click", "[data-save]", () => {
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
            this.rows = (result.data ?? []).map((x: any) => this.normalizeTagRow(x));

            await this.tagGrid?.setData(this.rows);

            this.updateTagGridFooter();
            this.updateTagCheckAllState();
            await this.loadSummary();

            if (this.selectedTagRow != null) {
                const nextRow = this.rows.find(x => x.id === this.selectedTagRow.id);

                if (nextRow != null) {
                    this.openTagDetail(nextRow);
                } else {
                    this.closeTagDetail();
                }
            }
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 조회 중 오류가 발생했습니다.");
        } finally {
            this.tagGrid?.hideLoading();
        }
    }

    async openRegisterPopup(): Promise<void> {
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

        this.selectedTreeItems = [];

        const device = this.getSelectedDevice();
        const rootSelector = this.getRootSelector();

        $(rootSelector).find("[data-device-name]").text(
            device == null ? "-" : `${device.deviceName} (${device.deviceType})`
        );

        this.drawerTree?.setItems(this.treeItems);
        this.renderSelectedTable();
        this.updatePopupCount();

        this.drawerPopup?.open();
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
                data: {
                    ...x,
                    protectType: x.protectType ?? "ReadOnly"
                }
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
        this.renderSelectedTable();
        this.updatePopupCount();
    }

    renderSelectedTable(): void {
        const rootSelector = this.getRootSelector();
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
            const dataType = String(row.dataType ?? "");
            const protectType = String(row.protectType ?? "ReadOnly");
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
                    <td>
                        <select class="form-select form-select-sm wf-edit-select wf-edit-select-datatype"
                                data-data-type
                                data-id="${id}">
                            ${this.createDataTypeOptions(dataType)}
                        </select>
                    </td>
                    <td>
                        <select class="form-select form-select-sm wf-edit-select wf-edit-select-protect"
                                data-protect-type
                                data-id="${id}">
                            ${this.createProtectTypeOptions(protectType)}
                        </select>
                    </td>
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

        this.updatePopupCount();
    }

    async saveSelectedTags(): Promise<void> {
        const rootSelector = this.getRootSelector();

        if (this.selectedTreeItems.length === 0) {
            notify.warning("등록할 노드를 선택해 주세요.");
            return;
        }

        const nodes = this.selectedTreeItems.map(item => {
            const row = item.data ?? {};
            const selectorId = this.escapeSelector(item.id);

            return {
                deviceId: this.selectedDeviceId,
                nodeId: item.id,
                tagName: String($(`${rootSelector} [data-tag-name][data-id="${selectorId}"]`).val() ?? "").trim(),
                nodeClass: row.nodeClass,
                dataType: String($(`${rootSelector} [data-data-type][data-id="${selectorId}"]`).val() ?? row.dataType ?? "").trim(),
                protectType: String($(`${rootSelector} [data-protect-type][data-id="${selectorId}"]`).val() ?? "ReadOnly").trim(),
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
                data: nodes
            });

            if (!result.success) {
                notify.error(result.message ?? "태그 저장에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "태그가 저장되었습니다.");

            this.browseRows = [];
            this.treeItems = [];

            this.drawerPopup?.close();

            await this.loadTags();
            await this.loadSummary();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 저장 중 오류가 발생했습니다.");
        }
    }

    openTagDetail(row: any): void {
        this.selectedTagRow = row;

        $(".wf-tag-content").addClass("has-detail");
        $("#tagDetailPanel").removeClass("is-hidden");

        this.tagGrid?.refreshLayout();

        const tag = this.normalizeTagRow(row);

        this.selectedTagRow = tag;

        $("#txtDetailId").val(tag.id ?? "");
        $("#txtDetailIdView").val(tag.id ?? "");
        $("#txtDetailDeviceId").val(tag.deviceId ?? this.selectedDeviceId);
        $("#txtDetailGroupId").val(tag.groupId ?? "");
        $("#txtDetailNodeId").val(tag.nodeId ?? "");
        $("#txtDetailTagName").val(tag.tagName ?? "");

        this.setSelectValue("#selDetailDataType", String(tag.dataType ?? ""));
        this.setSelectValue("#selDetailProtectType", this.normalizeProtectType(tag.protectType));

        $("#numDetailSamplingIntervalMs").val(tag.samplingIntervalMs ?? "");
        $("#numDetailSortOrder").val(tag.sortOrder ?? "");
        $("#txtDetailDescription").val(tag.description ?? "");
        $("#txtDetailSetting").val(tag.expressions ?? "");
        $("#txtDetailTestValue").val("");

        $("#chkDetailCollect").prop("checked", tag.isCollectEnabled === true);
        $("#chkDetailSaveDb").prop("checked", tag.saveToDatabase === true);
        $("#chkDetailDashboard").prop("checked", tag.showOnDashboard === true);
        $("#chkDetailEnabled").prop("checked", tag.isEnabled !== false);

        $("#lblDetailSubTitle").text(`${tag.id ?? ""} / ${tag.tagName ?? ""}`);

        this.refreshIcons();
    }

    closeTagDetail(): void {
        this.selectedTagRow = null;
        $(".wf-tag-content").removeClass("has-detail");
        $("#tagDetailPanel").addClass("is-hidden");

        this.tagGrid?.refreshLayout();
        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }

    async saveTagDetail(): Promise<void> {
        const id = String($("#txtDetailId").val() ?? "");

        if (id.length === 0) {
            notify.warning("수정할 태그를 선택해 주세요.");
            return;
        }

        const tagName = String($("#txtDetailTagName").val() ?? "").trim();

        if (tagName.length === 0) {
            notify.warning("태그명을 입력해 주세요.");
            return;
        }

        try {
            const result = await api.post({
                url: "/device/tag/update",
                data: {
                    id,
                    deviceId: String($("#txtDetailDeviceId").val() ?? "").trim(),
                    groupId: String($("#txtDetailGroupId").val() ?? "").trim(),
                    nodeId: String($("#txtDetailNodeId").val() ?? "").trim(),
                    tagName,
                    dataType: String($("#selDetailDataType").val() ?? "").trim(),
                    protectType: this.normalizeProtectType($("#selDetailProtectType").val()),
                    samplingIntervalMs: Number($("#numDetailSamplingIntervalMs").val() ?? 0),
                    sortOrder: Number($("#numDetailSortOrder").val() ?? 0),
                    description: String($("#txtDetailDescription").val() ?? "").trim(),
                    expressions: String($("#txtDetailSetting").val() ?? "").trim(),
                    isCollectEnabled: $("#chkDetailCollect").prop("checked") === true,
                    saveToDatabase: $("#chkDetailSaveDb").prop("checked") === true,
                    showOnDashboard: $("#chkDetailDashboard").prop("checked") === true,
                    isEnabled: $("#chkDetailEnabled").prop("checked") === true
                }
            });

            if (!result.success) {
                notify.error(result.message ?? "태그 수정에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "태그 정보가 수정되었습니다.");

            await this.loadTags();
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "태그 수정 중 오류가 발생했습니다.");
        }
    }

    async runExpressionTest(): Promise<void> {
        const raw = String($("#txtDetailTestValue").val() ?? "").trim();
        const expression = String($("#txtDetailSetting").val() ?? "").trim();
        const dataType = String($("#selDetailDataType").val() ?? "").trim();

        if (expression.length === 0) {
            notify.warning("계산식을 입력해 주세요.");
            return;
        }

        if (raw.length === 0) {
            notify.warning("테스트 값을 입력해 주세요.");
            return;
        }

        try {
            const result = await api.post({
                url: "/device/tag/runtest",
                data: {
                    dataType,
                    raw,
                    expression
                }
            });

            if (!result.success) {
                notify.error(result.message ?? "계산식 테스트에 실패했습니다.");
                return;
            }

            const calculatedValue = result.data == null
                ? ""
                : String(result.data);

            notify.success(`계산된 값은 '${calculatedValue}' 입니다.`);
        } catch (e) {
            notify.error(e instanceof Error ? e.message : "계산식 테스트 중 오류가 발생했습니다.");
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

    updatePopupCount(): void {
        const rootSelector = this.getRootSelector();
        const variableCount = this.drawerTree?.getSelectableItems().length ?? 0;
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

    getRootSelector(): string {
        return "#tagRegisterDrawer";
    }

    createDataTypeBadge(dataType: string): string {
        const value = String(dataType ?? "");
        const lower = value.toLowerCase();

        let className = "default";

        if (lower.includes("string") || lower === "ascii" || lower === "utf8") {
            className = "string";
        } else if (lower.includes("bool") || lower === "bit") {
            className = "boolean";
        } else if (
            lower.includes("int") ||
            lower.includes("float") ||
            lower.includes("double") ||
            lower.includes("decimal") ||
            lower.includes("bcd")
        ) {
            className = "number";
        }

        return `<span class="wf-tag-badge ${className}">${escapeHtml(value || "-")}</span>`;
    }

    createProtectTypeBadge(value: string): string {
        const normalizedValue = this.normalizeProtectType(value);
        const item = PROTECT_TYPE_OPTIONS.find(x => x.value === normalizedValue);

        return `<span class="wf-tag-badge default">${escapeHtml(item?.text ?? "읽기 전용")}</span>`;
    }

    createDataTypeOptions(selected: string): string {
        const values = DATA_TYPE_OPTIONS.includes(selected)
            ? DATA_TYPE_OPTIONS
            : selected.length > 0
                ? [selected, ...DATA_TYPE_OPTIONS]
                : DATA_TYPE_OPTIONS;

        return values
            .map(x => `<option value="${escapeHtml(x)}" ${x === selected ? "selected" : ""}>${escapeHtml(x)}</option>`)
            .join("");
    }

    createProtectTypeOptions(selected: string): string {
        return PROTECT_TYPE_OPTIONS
            .map(x => `<option value="${escapeHtml(x.value)}" ${x.value === selected ? "selected" : ""}>${escapeHtml(x.text)}</option>`)
            .join("");
    }

    refreshIcons(): void {
        const lucide = (window as any).lucide;

        if (lucide?.createIcons != null) {
            lucide.createIcons();
        }
    }

    normalizeTagRow(row: any): any {
        return {
            ...row,

            id: this.readValue(row, "id", "ID", "tagId", "TAG_ID"),
            deviceId: this.readValue(row, "deviceId", "DEVICE_ID"),
            groupId: this.readValue(row, "groupId", "GROUP_ID"),
            nodeId: this.readValue(row, "nodeId", "NODE_ID"),

            tagName: this.readValue(row, "tagName", "TAG_NAME", "displayName") ?? "",
            displayName: this.readValue(row, "displayName", "tagName", "TAG_NAME") ?? "",

            dataType: this.readValue(row, "dataType", "DATA_TYPE") ?? "",
            protectType: this.normalizeProtectType(this.readValue(row, "protectType", "PROTECT_TYPE")),

            description: this.readValue(row, "description", "DESCRIPTION") ?? "",
            expressions: this.readValue(row, "expressions", "expression", "EXPRESSIONS") ?? "",

            isCollectEnabled: this.readBool(row, true, "isCollectEnabled", "IS_COLLECTENABLED"),
            saveToDatabase: this.readBool(row, true, "saveToDatabase", "SAVE_TO_DATABASE"),
            showOnDashboard: this.readBool(row, false, "showOnDashboard", "SHOW_ON_DASHBOARD"),
            isEnabled: this.readBool(row, true, "isEnabled", "IsEnabled"),

            samplingIntervalMs: this.readNumber(row, "samplingIntervalMs", "SAMPLINGINTERVALMS"),
            sortOrder: this.readNumber(row, "sortOrder", "SORT_ORDER"),
            queueSize: this.readNumber(row, "queueSize", "QUEUE_SIZE") ?? 1
        };
    }

    readValue(row: any, ...names: string[]): any {
        if (row == null) {
            return null;
        }

        for (const name of names) {
            if (Object.prototype.hasOwnProperty.call(row, name)) {
                return row[name];
            }
        }

        const normalizedNames = names.map(x => this.normalizeFieldName(x));

        for (const key of Object.keys(row)) {
            if (normalizedNames.includes(this.normalizeFieldName(key))) {
                return row[key];
            }
        }

        return null;
    }

    readBool(row: any, defaultValue: boolean, ...names: string[]): boolean {
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

    readNumber(row: any, ...names: string[]): number | null {
        const value = this.readValue(row, ...names);

        if (value == null || value === "") {
            return null;
        }

        const numberValue = Number(value);

        return Number.isFinite(numberValue) ? numberValue : null;
    }

    normalizeFieldName(value: string): string {
        return String(value ?? "")
            .replace(/_/g, "")
            .replace(/-/g, "")
            .toLowerCase();
    }

    setSelectValue(selector: string, value: string): void {
        const $select = $(selector);
        const targetValue = String(value ?? "").trim();

        if (targetValue.length === 0) {
            $select.val("");
            return;
        }

        const exists = $select
            .find("option")
            .toArray()
            .some(x => String($(x).val() ?? "") === targetValue);

        if (!exists) {
            $select.prepend(
                `<option value="${escapeHtml(targetValue)}">${escapeHtml(targetValue)}</option>`
            );
        }

        $select.val(targetValue);
    }

    normalizeProtectType(value: unknown): string {
        const text = String(value ?? "").trim();

        if (
            text === "ReadWrite" ||
            text === "READ_WRITE" ||
            text === "읽고 쓰기"
        ) {
            return "ReadWrite";
        }

        if (
            text === "WriteOnly" ||
            text === "WRITE_ONLY" ||
            text === "쓰기 전용"
        ) {
            return "WriteOnly";
        }

        return "ReadOnly";
    }

    escapeSelector(value: string): string {
        return value.replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
    }
}