//import $ from "jquery";

//import { notify } from "../framework/notify";
//import { WebFlexCheckTree } from "./webflexCheckTree";

//export type WebFlexTagRegisterNode = {
//    [key: string]: any;

//    nodeId: string;
//    parentNodeId?: string | null;
//    displayName: string;
//    nodeClass?: string | null;
//    dataType?: string | null;
//    description?: string | null;
//    children?: WebFlexTagRegisterNode[];
//};

//export type WebFlexTagRegisterSaveNode = {
//    [key: string]: any;

//    nodeId: string;
//    parentNodeId?: string | null;
//    displayName: string;
//    originalDisplayName?: string | null;
//    nodeClass?: string | null;
//    dataType?: string | null;
//    description?: string | null;
//    isCollectEnabled?: boolean;
//    isEnabled?: boolean;
//};

//type PopupSelectedNode = WebFlexTagRegisterSaveNode & {
//    isChecked: boolean;
//};

//export type WebFlexPopupSizeOptions = {
//    widthPercent?: number;
//    heightPercent?: number;
//};

//export type WebFlexTagRegisterPopupOpenOptions = WebFlexPopupSizeOptions & {
//    deviceName: string;
//    nodes: WebFlexTagRegisterNode[];
//    treeNodes: WebFlexTagRegisterNode[];
//};

//export type WebFlexTagRegisterPopupOptions = WebFlexPopupSizeOptions & {
//    selector: string;
//    cascadeCheck?: boolean;
//    saveButtonText?: (count: number) => string;
//    onSave: (nodes: WebFlexTagRegisterSaveNode[]) => Promise<boolean>;
//    isSelectable?: (node: WebFlexTagRegisterNode) => boolean;
//    getNodeId?: (node: WebFlexTagRegisterNode) => string;
//    getNodeText?: (node: WebFlexTagRegisterNode) => string;
//    getNodeTooltip?: (node: WebFlexTagRegisterNode) => string;
//};

//export class WebFlexTagRegisterPopup {
//    readonly $root: JQuery<HTMLElement>;
//    readonly cascadeCheck: boolean;
//    readonly saveButtonText: (count: number) => string;
//    readonly onSave: (nodes: WebFlexTagRegisterSaveNode[]) => Promise<boolean>;
//    readonly isSelectable: (node: WebFlexTagRegisterNode) => boolean;
//    readonly getNodeId: (node: WebFlexTagRegisterNode) => string;
//    readonly getNodeText: (node: WebFlexTagRegisterNode) => string;
//    readonly getNodeTooltip: (node: WebFlexTagRegisterNode) => string;

//    readonly widthPercent?: number;
//    readonly heightPercent?: number;

//    tree: WebFlexCheckTree<WebFlexTagRegisterNode> | null = null;
//    nodes: WebFlexTagRegisterNode[] = [];
//    treeNodes: WebFlexTagRegisterNode[] = [];
//    selectedNodes: PopupSelectedNode[] = [];

//    constructor(options: WebFlexTagRegisterPopupOptions) {
//        this.$root = $(options.selector);
//        this.cascadeCheck = options.cascadeCheck ?? true;
//        this.saveButtonText = options.saveButtonText ?? (count => count > 0 ? `태그 등록 (${count})` : "태그 등록");
//        this.onSave = options.onSave;
//        this.isSelectable = options.isSelectable ?? (node => node.nodeClass === "Variable");
//        this.getNodeId = options.getNodeId ?? (node => node.nodeId);
//        this.getNodeText = options.getNodeText ?? (node => node.displayName);
//        this.getNodeTooltip = options.getNodeTooltip ?? (node => node.nodeId);

//        this.widthPercent = options.widthPercent;
//        this.heightPercent = options.heightPercent;

//        this.initTree();
//        this.bindEvents();
//    }

//    open(options: WebFlexTagRegisterPopupOpenOptions): void {
//        this.nodes = options.nodes;
//        this.treeNodes = options.treeNodes;
//        this.selectedNodes = [];

//        this.applySize({
//            widthPercent: options.widthPercent ?? this.widthPercent,
//            heightPercent: options.heightPercent ?? this.heightPercent
//        });

//        this.$root.find("[data-device-name]").text(options.deviceName);
//        this.$root.attr("aria-hidden", "false");
//        this.$root.addClass("is-open");
//        $("body").addClass("wf-popup-open");

//        this.tree?.setNodes(this.nodes, this.treeNodes);
//        this.renderSelectedTable();
//        this.updateCount();
//        this.refreshIcons();

//        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
//    }

//    close(): void {
//        this.$root.removeClass("is-open");
//        this.$root.attr("aria-hidden", "true");
//        $("body").removeClass("wf-popup-open");

//        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
//    }

//    applySize(options: WebFlexPopupSizeOptions): void {
//        const root = this.$root.get(0);

//        if (root == null) {
//            return;
//        }

//        const width = this.normalizePercent(options.widthPercent);
//        const height = this.normalizePercent(options.heightPercent);

//        if (width == null) {
//            root.style.removeProperty("--wf-tag-popup-width");
//        } else {
//            root.style.setProperty("--wf-tag-popup-width", `${width}vw`);
//        }

//        if (height == null) {
//            root.style.removeProperty("--wf-tag-popup-height");
//        } else {
//            root.style.setProperty("--wf-tag-popup-height", `${height}vh`);
//        }
//    }

//    normalizePercent(value: number | undefined): number | null {
//        if (value == null || Number.isNaN(value)) {
//            return null;
//        }

//        if (value < 10) {
//            return 10;
//        }

//        if (value > 100) {
//            return 100;
//        }

//        return value;
//    }

//    initTree(): void {
//        const treeHost = this.$root.find("[data-tree-host]").get(0);

//        if (treeHost == null) {
//            return;
//        }

//        this.tree = new WebFlexCheckTree<WebFlexTagRegisterNode>({
//            selector: treeHost,
//            nodes: [],
//            treeNodes: [],
//            cascadeCheck: this.cascadeCheck,
//            classPrefix: "wf-tag-tree",
//            getId: node => this.getNodeId(node),
//            getText: node => this.getNodeText(node),
//            getTooltip: node => this.getNodeTooltip(node),
//            getChildren: node => node.children,
//            isSelectable: node => this.isSelectable(node),
//            onSelectionChanged: nodes => {
//                this.syncSelectedNodes(nodes);
//            }
//        });
//    }

//    bindEvents(): void {
//        this.$root.on("click", "[data-popup-close]", () => {
//            this.close();
//        });

//        this.$root.on("click", "[data-select-all]", () => {
//            const allSelected = this.tree?.isAllSelected() === true;
//            this.tree?.toggleAll(!allSelected);
//        });

//        this.$root.on("change", "[data-row-check]", event => {
//            const nodeId = String($(event.currentTarget).attr("data-node-id") ?? "");
//            const checked = $(event.currentTarget).prop("checked") === true;
//            const target = this.selectedNodes.find(x => x.nodeId === nodeId);

//            if (target != null) {
//                target.isChecked = checked;
//            }

//            this.renderSelectedTable();
//        });

//        this.$root.on("change", "[data-check-all]", event => {
//            const checked = $(event.currentTarget).prop("checked") === true;

//            for (const node of this.selectedNodes) {
//                node.isChecked = checked;
//            }

//            this.renderSelectedTable();
//        });

//        this.$root.on("input", "[data-tag-name]", event => {
//            const nodeId = String($(event.currentTarget).attr("data-node-id") ?? "");
//            const target = this.selectedNodes.find(x => x.nodeId === nodeId);

//            if (target != null) {
//                target.displayName = String($(event.currentTarget).val() ?? "");
//            }
//        });

//        this.$root.on("input", "[data-description]", event => {
//            const nodeId = String($(event.currentTarget).attr("data-node-id") ?? "");
//            const target = this.selectedNodes.find(x => x.nodeId === nodeId);

//            if (target != null) {
//                target.description = String($(event.currentTarget).val() ?? "");
//            }
//        });

//        this.$root.on("change", "[data-collect-check]", event => {
//            const nodeId = String($(event.currentTarget).attr("data-node-id") ?? "");
//            const target = this.selectedNodes.find(x => x.nodeId === nodeId);

//            if (target != null) {
//                target.isCollectEnabled = $(event.currentTarget).prop("checked") === true;
//            }
//        });

//        this.$root.on("change", "[data-enabled-check]", event => {
//            const nodeId = String($(event.currentTarget).attr("data-node-id") ?? "");
//            const target = this.selectedNodes.find(x => x.nodeId === nodeId);

//            if (target != null) {
//                target.isEnabled = $(event.currentTarget).prop("checked") === true;
//            }
//        });

//        this.$root.on("click", "[data-remove-row]", event => {
//            const nodeId = String($(event.currentTarget).attr("data-node-id") ?? "");
//            const nextIds = this.selectedNodes
//                .filter(x => x.nodeId !== nodeId)
//                .map(x => x.nodeId);

//            this.tree?.setSelectedIds(nextIds);
//        });

//        this.$root.on("click", "[data-collect-on]", () => {
//            this.applyCheckedRows(node => node.isCollectEnabled = true);
//        });

//        this.$root.on("click", "[data-collect-off]", () => {
//            this.applyCheckedRows(node => node.isCollectEnabled = false);
//        });

//        this.$root.on("click", "[data-use-on]", () => {
//            this.applyCheckedRows(node => node.isEnabled = true);
//        });

//        this.$root.on("click", "[data-use-off]", () => {
//            this.applyCheckedRows(node => node.isEnabled = false);
//        });

//        this.$root.on("click", "[data-save]", () => {
//            void this.save();
//        });
//    }

//    syncSelectedNodes(nodes: WebFlexTagRegisterNode[]): void {
//        const oldMap = new Map(this.selectedNodes.map(node => [node.nodeId, node]));

//        this.selectedNodes = nodes.map(node => {
//            const exists = oldMap.get(node.nodeId);

//            if (exists != null) {
//                return exists;
//            }

//            return this.toSelectedNode(node);
//        });

//        this.renderSelectedTable();
//    }

//    renderSelectedTable(): void {
//        const $host = this.$root.find("[data-selected-host]");
//        const $empty = this.$root.find("[data-empty-host]");

//        $host.empty();

//        if (this.selectedNodes.length === 0) {
//            $empty.removeClass("d-none");
//        } else {
//            $empty.addClass("d-none");
//        }

//        for (const node of this.selectedNodes) {
//            const nodeId = this.escapeHtml(node.nodeId);

//            $host.append(`
//                <tr>
//                    <td class="wf-check-col">
//                        <input type="checkbox"
//                               class="form-check-input"
//                               data-row-check
//                               data-node-id="${nodeId}"
//                               ${node.isChecked ? "checked" : ""} />
//                    </td>
//                    <td>
//                        <div class="wf-node-origin">
//                            <strong>${this.escapeHtml(node.originalDisplayName)}</strong>
//                            <span>${nodeId}</span>
//                        </div>
//                    </td>
//                    <td>
//                        <input type="text"
//                               class="form-control"
//                               data-tag-name
//                               data-node-id="${nodeId}"
//                               value="${this.escapeHtml(node.displayName)}" />
//                    </td>
//                    <td>${this.createDataTypeBadge(String(node.dataType ?? ""))}</td>
//                    <td>
//                        <input type="text"
//                               class="form-control"
//                               data-description
//                               data-node-id="${nodeId}"
//                               value="${this.escapeHtml(node.description ?? "")}"
//                               placeholder="설명 (선택)" />
//                    </td>
//                    <td class="text-center">
//                        <input type="checkbox"
//                               class="form-check-input"
//                               data-collect-check
//                               data-node-id="${nodeId}"
//                               ${node.isCollectEnabled ? "checked" : ""} />
//                    </td>
//                    <td class="text-center">
//                        <input type="checkbox"
//                               class="form-check-input"
//                               data-enabled-check
//                               data-node-id="${nodeId}"
//                               ${node.isEnabled ? "checked" : ""} />
//                    </td>
//                    <td class="text-center">
//                        <button type="button"
//                                class="wf-row-remove"
//                                data-remove-row
//                                data-node-id="${nodeId}">×</button>
//                    </td>
//                </tr>
//            `);
//        }

//        this.updateCount();
//    }

//    updateCount(): void {
//        const variableCount = this.tree?.getSelectableNodes().length ?? 0;
//        const selectedCount = this.selectedNodes.length;
//        const checkedCount = this.selectedNodes.filter(x => x.isChecked).length;

//        this.$root.find("[data-selected-count]").text(`${selectedCount}/${variableCount}`);
//        this.$root.find("[data-selected-text]").text(`${checkedCount}/${selectedCount}개 선택`);
//        this.$root.find("[data-loaded-text]").text(`총 ${variableCount.toLocaleString()}개 노드 로드됨`);
//        this.$root.find("[data-loaded-count]")
//            .toggleClass("d-none", variableCount === 0)
//            .text(`${variableCount.toLocaleString()}개 로드됨`);
//        this.$root.find("[data-save-text]").text(this.saveButtonText(selectedCount));
//        this.$root.find("[data-check-all]").prop("checked", selectedCount > 0 && checkedCount === selectedCount);
//    }

//    applyCheckedRows(apply: (node: PopupSelectedNode) => void): void {
//        const checkedRows = this.selectedNodes.filter(x => x.isChecked);

//        if (checkedRows.length === 0) {
//            notify.warning("적용할 행을 선택해 주세요.");
//            return;
//        }

//        for (const node of checkedRows) {
//            apply(node);
//        }

//        this.renderSelectedTable();
//    }

//    async save(): Promise<void> {
//        if (this.selectedNodes.length === 0) {
//            notify.warning("등록할 노드를 선택해 주세요.");
//            return;
//        }

//        const invalid = this.selectedNodes.find(x => x.displayName.trim().length === 0);

//        if (invalid != null) {
//            notify.warning("태그명을 입력해 주세요.");
//            return;
//        }

//        const nodes: WebFlexTagRegisterSaveNode[] = this.selectedNodes.map(x => {
//            const { isChecked, children, ...node } = x;

//            return {
//                ...node,
//                displayName: x.displayName.trim(),
//                description: x.description
//            };
//        });

//        const success = await this.onSave(nodes);

//        if (success) {
//            this.close();
//        }
//    }

//    toSelectedNode(node: WebFlexTagRegisterNode): PopupSelectedNode {
//        const isCollectEnabled = typeof node.isCollectEnabled === "boolean"
//            ? node.isCollectEnabled
//            : true;

//        const isEnabled = typeof node.isEnabled === "boolean"
//            ? node.isEnabled
//            : true;

//        return {
//            ...node,
//            nodeId: node.nodeId,
//            parentNodeId: node.parentNodeId,
//            displayName: node.displayName,
//            originalDisplayName: String(node.originalDisplayName ?? node.displayName),
//            nodeClass: node.nodeClass,
//            dataType: node.dataType,
//            description: node.description,
//            isChecked: true,
//            isCollectEnabled,
//            isEnabled
//        };
//    }

//    createDataTypeBadge(dataType: string): string {
//        const value = String(dataType ?? "");
//        const lower = value.toLowerCase();

//        let className = "default";

//        if (lower.includes("string")) {
//            className = "string";
//        } else if (lower.includes("bool")) {
//            className = "boolean";
//        } else if (lower.includes("int") || lower.includes("float") || lower.includes("double") || lower.includes("decimal")) {
//            className = "number";
//        }

//        return `<span class="wf-tag-badge ${className}">${this.escapeHtml(value || "-")}</span>`;
//    }

//    refreshIcons(): void {
//        const lucide = (window as any).lucide;

//        if (lucide?.createIcons != null) {
//            lucide.createIcons();
//        }
//    }

//    escapeHtml(value: string | null | undefined): string {
//        return String(value ?? "")
//            .replace(/&/g, "&amp;")
//            .replace(/</g, "&lt;")
//            .replace(/>/g, "&gt;")
//            .replace(/\"/g, "&quot;")
//            .replace(/'/g, "&#039;");
//    }
//}