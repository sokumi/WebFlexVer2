import { api } from "../../framework/common";
import { WebFlexGrid } from "../../framework/grid/webflexGrid";
import {
    numberFormatter,
    textFormatter
} from "../../framework/grid/webflexGridFormatters";

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

type BrowseNodeDto = {
    deviceId: string;
    nodeId: string;
    parentNodeId: string;
    displayName: string;
    nodeClass: string;
    dataType: string;
    accessLevel: string;
    engineeringUnit: string;
    description?: string | null;
    children?: BrowseNodeDto[];
};

type SelectedNodeDto = {
    nodeId: string;
    displayName: string;
    nodeClass: string;
    dataType: string;
    description?: string | null;
};

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

type TabulatorRowComponent = {
    getData: () => unknown;
};

export default class Page {
     devices: DeviceRowDto[] = [];
     nodes: BrowseNodeDto[] = [];
     treeNodes: BrowseNodeDto[] = [];
     selectedNodes: SelectedNodeDto[] = [];
     selectedTagIds: string[] = [];
     selectedDeviceId = "";

     selectedNodeGrid: WebFlexGrid<SelectedNodeDto> | null = null;
     tagGrid: WebFlexGrid<TagRowDto> | null = null;

    public init(): void {
        this.initSelectedNodeGrid();
        this.initTagGrid();
        this.bindEvents();

        void this.loadDevices();
        void this.loadSummary();
    }

     bindEvents(): void {
        $("#selDevice").on("change", () => {
            this.selectedDeviceId = String($("#selDevice").val() ?? "");

            this.nodes = [];
            this.treeNodes = [];
            this.selectedNodes = [];
            this.selectedTagIds = [];

            this.renderNodes();
            void this.selectedNodeGrid?.setData([]);
            void this.tagGrid?.setData([]);

            this.updateSelectedCount();

            if (this.selectedDeviceId.length > 0) {
                void this.loadSummary();
                void this.loadTags();
            }
        });

        $("#btnBrowse").on("click", () => {
            void this.browse();
        });

        $("#btnSelectAll").on("click", () => {
            this.selectAllVariableNodes();
        });

        $("#btnClearSelect").on("click", () => {
            this.clearSelectedNodes();
        });

        $("#btnSave").on("click", () => {
            void this.save();
        });

        $("#btnSearch").on("click", () => {
            void this.loadTags();
        });

        $("#txtTagKeyword").on("keydown", event => {
            if (event.key === "Enter") {
                void this.loadTags();
            }
        });

        $("#chkOnlyCollectTag").on("change", () => {
            void this.loadTags();
        });

        $("#btnTagSelectAll").on("click", () => {
            this.selectAllTags();
        });

        $("#btnTagClearSelect").on("click", () => {
            this.clearSelectedTags();
        });

        $("#btnDelete").on("click", () => {
            void this.deleteTags();
        });
    }

     initSelectedNodeGrid(): void {
        this.selectedNodeGrid = new WebFlexGrid<SelectedNodeDto>({
            selector: "#gridSelectedNode",
            height: 430,
            pagination: true,
            paginationSize: 10,
            columns: [
                {
                    title: "DisplayName",
                    field: "displayName",
                    minWidth: 160,
                    formatter: textFormatter
                },
                {
                    title: "NodeId",
                    field: "nodeId",
                    minWidth: 260,
                    formatter: textFormatter
                },
                {
                    title: "DataType",
                    field: "dataType",
                    width: 120,
                    formatter: textFormatter
                }
            ]
        });
    }

     initTagGrid(): void {
        this.tagGrid = new WebFlexGrid<TagRowDto>({
            selector: "#gridTag",
            height: 380,
            pagination: true,
            paginationSize: 10,
            selectableRows: true,
            columns: [
                {
                    title: "",
                    field: "id",
                    width: 54,
                    hozAlign: "center",
                    headerSort: false,
                    formatter: (cell: { getValue: () => any; }) => {
                        const id = String(cell.getValue() ?? "");
                        const checked = this.selectedTagIds.includes(id) ? "checked" : "";
                        return `<input type="checkbox" class="wf-tag-check" data-id="${id}" ${checked} />`;
                    }
                },
                {
                    title: "TagId",
                    field: "id",
                    width: 110,
                    formatter: textFormatter
                },
                {
                    title: "TagName",
                    field: "tagName",
                    minWidth: 160,
                    formatter: textFormatter
                },
                {
                    title: "NodeId",
                    field: "nodeId",
                    minWidth: 280,
                    formatter: textFormatter
                },
                {
                    title: "DataType",
                    field: "dataType",
                    width: 120,
                    formatter: textFormatter
                },
                {
                    title: "수집",
                    field: "isCollectEnabled",
                    width: 90,
                    formatter: (cell: { getValue: () => boolean; }) => {
                        return cell.getValue() === true
                            ? `<span class="wf-status good">Y</span>`
                            : `<span class="wf-status bad">N</span>`;
                    }
                },
                {
                    title: "DB저장",
                    field: "saveToDatabase",
                    width: 100,
                    formatter: (cell: { getValue: () => boolean; }) => {
                        return cell.getValue() === true
                            ? `<span class="wf-status good">Y</span>`
                            : `<span class="wf-status bad">N</span>`;
                    }
                },
                {
                    title: "Sampling",
                    field: "samplingIntervalMs",
                    width: 110,
                    hozAlign: "right",
                    formatter: numberFormatter
                },
                {
                    title: "Queue",
                    field: "queueSize",
                    width: 90,
                    hozAlign: "right",
                    formatter: numberFormatter
                }
            ],
            options: {
                rowClick: (_event: Event, row: TabulatorRowComponent) => {
                    const data = row.getData() as TagRowDto;
                    this.toggleTagSelection(data.id);
                }
            }
        });

        $("#gridTag").on("change", ".wf-tag-check", event => {
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
                this.showAlert(result.message ?? "디바이스 조회에 실패했습니다.");
                return;
            }

            this.devices = result.data;

            const $sel = $("#selDevice");
            $sel.empty();
            $sel.append(`<option value="">디바이스 선택</option>`);

            for (const device of this.devices) {
                const collectText = device.isCollectEnabled ? "수집" : "중지";
                $sel.append(
                    `<option value="${this.escapeHtml(device.id)}">${this.escapeHtml(device.deviceName)} (${this.escapeHtml(device.deviceType)} / ${collectText})</option>`
                );
            }
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }

     async loadSummary(): Promise<void> {
        try {
            const deviceId = encodeURIComponent(this.selectedDeviceId);

            const result = await api.get<DeviceTagSummaryDto>({
                url: `/test/devicetag/summary?deviceId=${deviceId}`
            });

            if (!result.success || result.data == null) {
                this.showAlert(result.message ?? "요약 조회에 실패했습니다.");
                return;
            }

            $("#lblDeviceCount").text(result.data.deviceCount.toLocaleString());
            $("#lblNodeCount").text(result.data.nodeCount.toLocaleString());
            $("#lblVariableNodeCount").text(result.data.variableNodeCount.toLocaleString());
            $("#lblTagCount").text(result.data.tagCount.toLocaleString());
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "요약 조회 중 오류가 발생했습니다.");
        }
    }

     async browse(): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            this.showAlert("디바이스를 선택해 주세요.");
            return;
        }

        try {
            const onlyCollectable = $("#chkOnlyCollectable").prop("checked") === true;

            const result = await api.get<BrowseNodeDto[]>({
                url: `/test/devicetag/browse?deviceId=${encodeURIComponent(this.selectedDeviceId)}&onlyCollectable=${onlyCollectable}`
            });

            if (!result.success || result.data == null) {
                this.showAlert(result.message ?? "노드 조회에 실패했습니다.");
                return;
            }

            this.nodes = result.data;
            this.treeNodes = this.buildTree(this.nodes);
            this.selectedNodes = [];

            this.renderNodes();
            await this.selectedNodeGrid?.setData([]);
            this.updateSelectedCount();

            this.showAlert("노드를 조회했습니다.");
            await this.loadSummary();
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "노드 조회 중 오류가 발생했습니다.");
        }
    }

     async loadTags(): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            this.showAlert("디바이스를 선택해 주세요.");
            return;
        }

        try {
            const keyword = encodeURIComponent(String($("#txtTagKeyword").val() ?? "").trim());
            const onlyCollect = $("#chkOnlyCollectTag").prop("checked") === true;

            const result = await api.get<TagRowDto[]>({
                url: `/test/devicetag/list?deviceId=${encodeURIComponent(this.selectedDeviceId)}&keyword=${keyword}&onlyCollect=${onlyCollect}`
            });

            if (!result.success || result.data == null) {
                this.showAlert(result.message ?? "태그 조회에 실패했습니다.");
                return;
            }

            this.selectedTagIds = [];
            await this.tagGrid?.setData(result.data);

            this.showAlert("태그 목록을 조회했습니다.");
            await this.loadSummary();
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "태그 조회 중 오류가 발생했습니다.");
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

     renderNodes(): void {
        const $area = $("#nodeList");
        $area.empty();

        if (this.treeNodes.length === 0) {
            $area.append(`<div class="wf-empty-message">디바이스를 선택한 뒤 노드 조회 버튼을 클릭하세요.</div>`);
            return;
        }

        for (const node of this.treeNodes) {
            $area.append(this.createNodeElement(node, 0));
        }
    }

     createNodeElement(node: BrowseNodeDto, depth: number): JQuery<HTMLElement> {
        const isVariable = node.nodeClass === "Variable";
        const checked = this.selectedNodes.some(x => x.nodeId === node.nodeId);
        const padding = depth * 18;

        const $wrapper = $(`<div class="wf-node-item"></div>`);

        const unitText = node.engineeringUnit ? ` [${this.escapeHtml(node.engineeringUnit)}]` : "";
        const descText = node.description ? ` · ${this.escapeHtml(node.description)}` : "";
        const rowClass = isVariable ? "variable" : "object";
        const icon = isVariable ? "●" : "▾";

        const checkboxHtml = isVariable
            ? `<input type="checkbox" ${checked ? "checked" : ""} />`
            : `<span class="wf-node-object-icon">${icon}</span>`;

        const $row = $(`
            <div class="wf-node-row ${rowClass}" style="padding-left:${padding + 8}px">
                ${checkboxHtml}
                <div class="wf-node-content">
                    <div class="wf-node-title">
                        <span>${icon}</span>
                        <span>${this.escapeHtml(node.displayName)}${unitText}</span>
                    </div>
                    <div class="wf-node-id">${this.escapeHtml(node.nodeId)}</div>
                    <div class="wf-node-meta">${this.escapeHtml(node.nodeClass)} ${this.escapeHtml(node.dataType)} ${this.escapeHtml(node.accessLevel)}${descText}</div>
                </div>
            </div>
        `);

        if (isVariable) {
            $row.find("input").on("change", event => {
                const isChecked = $(event.currentTarget).prop("checked") === true;
                this.toggleNode(node, isChecked);
            });
        }

        $wrapper.append($row);

        for (const child of node.children ?? []) {
            $wrapper.append(this.createNodeElement(child, depth + 1));
        }

        return $wrapper;
    }

     toggleNode(node: BrowseNodeDto, checked: boolean): void {
        if (checked) {
            const exists = this.selectedNodes.some(x => x.nodeId === node.nodeId);

            if (!exists) {
                this.selectedNodes.push({
                    nodeId: node.nodeId,
                    displayName: node.displayName,
                    nodeClass: node.nodeClass,
                    dataType: node.dataType,
                    description: node.description
                });
            }
        } else {
            this.selectedNodes = this.selectedNodes.filter(x => x.nodeId !== node.nodeId);
        }

        void this.selectedNodeGrid?.setData(this.selectedNodes);
        this.updateSelectedCount();
    }

     selectAllVariableNodes(): void {
        this.selectedNodes = this.nodes
            .filter(x => x.nodeClass === "Variable")
            .map(x => ({
                nodeId: x.nodeId,
                displayName: x.displayName,
                nodeClass: x.nodeClass,
                dataType: x.dataType,
                description: x.description
            }));

        this.renderNodes();
        void this.selectedNodeGrid?.setData(this.selectedNodes);
        this.updateSelectedCount();
    }

     clearSelectedNodes(): void {
        this.selectedNodes = [];

        this.renderNodes();
        void this.selectedNodeGrid?.setData([]);
        this.updateSelectedCount();
    }

     async save(): Promise<void> {
        if (this.selectedDeviceId.length === 0) {
            this.showAlert("디바이스를 선택해 주세요.");
            return;
        }

        if (this.selectedNodes.length === 0) {
            this.showAlert("저장할 노드를 선택해 주세요.");
            return;
        }

        const request: SaveRequest = {
            deviceId: this.selectedDeviceId,
            nodes: this.selectedNodes
        };

        try {
            const result = await api.post<unknown, SaveRequest>({
                url: "/test/devicetag/save",
                data: request
            });

            if (!result.success) {
                this.showAlert(result.message ?? "태그 저장에 실패했습니다.");
                return;
            }

            this.showAlert(result.message ?? "태그가 저장되었습니다.");

            this.clearSelectedNodes();
            await this.loadTags();
            await this.loadSummary();
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "태그 저장 중 오류가 발생했습니다.");
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

        this.tagGrid?.instance.redraw(true);
    }

     selectAllTags(): void {
        const rows = this.tagGrid?.instance.getSelectedData() as TagRowDto[];

        if (rows.length > 0) {
            this.selectedTagIds = rows.map(x => x.id);
        } else {
            this.showAlert("행 선택 기준이 아니라 현재 표시 데이터 기준 선택은 다음 단계에서 보강할게.");
        }

        this.tagGrid?.instance.redraw(true);
    }

     clearSelectedTags(): void {
        this.selectedTagIds = [];
        this.tagGrid?.instance.redraw(true);
    }

     async deleteTags(): Promise<void> {
        if (this.selectedTagIds.length === 0) {
            this.showAlert("삭제할 태그를 선택해 주세요.");
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
                this.showAlert(result.message ?? "태그 삭제에 실패했습니다.");
                return;
            }

            this.showAlert(result.message ?? "태그가 삭제되었습니다.");

            this.selectedTagIds = [];
            await this.loadTags();
            await this.loadSummary();
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "태그 삭제 중 오류가 발생했습니다.");
        }
    }

     updateSelectedCount(): void {
        $("#lblSelectedCount").text(`${this.selectedNodes.length.toLocaleString()}개 선택`);
    }

     showAlert(message: string): void {
        $("#testAlertMessage").text(message);
        $("#testAlert").removeClass("d-none");

        window.setTimeout(() => {
            $("#testAlert").addClass("d-none");
        }, 2500);
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