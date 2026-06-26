export type WebFlexCheckTreeOptions<TNode> = {
    selector: string | HTMLElement;

    nodes: TNode[];
    treeNodes: TNode[];

    getId: (node: TNode) => string;
    getText: (node: TNode) => string;
    getChildren: (node: TNode) => TNode[] | undefined;
    isSelectable: (node: TNode) => boolean;

    getTooltip?: (node: TNode) => string;
    cascadeCheck?: boolean;
    showCount?: boolean;
    defaultExpanded?: boolean;
    classPrefix?: string;

    onSelectionChanged?: (selectedNodes: TNode[]) => void;
};

export class WebFlexCheckTree<TNode> {
    readonly root: HTMLElement;
    readonly getId: (node: TNode) => string;
    readonly getText: (node: TNode) => string;
    readonly getChildren: (node: TNode) => TNode[] | undefined;
    readonly isSelectable: (node: TNode) => boolean;
    readonly getTooltip?: (node: TNode) => string;
    readonly cascadeCheck: boolean;
    readonly showCount: boolean;
    readonly defaultExpanded: boolean;
    readonly classPrefix: string;
    readonly onSelectionChanged?: (selectedNodes: TNode[]) => void;

    collapsedIds = new Set<string>();

    nodes: TNode[];
    treeNodes: TNode[];
    selectedIds = new Set<string>();

    keySeq = 0;
    keyById = new Map<string, string>();
    nodeByKey = new Map<string, TNode>();

     constructor(options: WebFlexCheckTreeOptions<TNode>) {
        this.root = this.resolveElement(options.selector);
        this.nodes = options.nodes;
        this.treeNodes = options.treeNodes;

        this.getId = options.getId;
        this.getText = options.getText;
        this.getChildren = options.getChildren;
        this.isSelectable = options.isSelectable;
        this.getTooltip = options.getTooltip;

        this.cascadeCheck = options.cascadeCheck ?? true;
        this.showCount = options.showCount ?? true;
        this.defaultExpanded = options.defaultExpanded ?? true;
        this.classPrefix = options.classPrefix ?? "wf-check-tree";
        this.onSelectionChanged = options.onSelectionChanged;

        this.bindEvents();
        this.render();
    }

     setNodes(nodes: TNode[], treeNodes: TNode[]): void {
        this.nodes = nodes;
        this.treeNodes = treeNodes;
        this.selectedIds.clear();
        this.collapsedIds.clear();
        this.resetKeys();

        if (!this.defaultExpanded) {
            this.collapseAllGroups();
        }

        this.render();
        this.emitSelectionChanged();
    }

     clear(): void {
        this.nodes = [];
        this.treeNodes = [];
        this.selectedIds.clear();
        this.collapsedIds.clear();
        this.resetKeys();
        this.render();
        this.emitSelectionChanged();
    }

     clearSelection(): void {
        this.selectedIds.clear();
        this.render();
        this.emitSelectionChanged();
    }

     setSelectedIds(ids: string[]): void {
        this.selectedIds = new Set(ids);
        this.render();
        this.emitSelectionChanged();
    }

     getSelectedIds(): string[] {
        return Array.from(this.selectedIds);
    }

     getSelectedNodes(): TNode[] {
        return this.nodes.filter(node => this.selectedIds.has(this.getId(node)));
    }

     getSelectableNodes(): TNode[] {
        return this.nodes.filter(node => this.isSelectable(node));
    }

     isAllSelected(): boolean {
        const selectable = this.getSelectableNodes();
        return selectable.length > 0 && selectable.every(node => this.selectedIds.has(this.getId(node)));
    }

     toggleAll(checked: boolean): void {
        const selectable = this.getSelectableNodes();

        for (const node of selectable) {
            const id = this.getId(node);

            if (checked) {
                this.selectedIds.add(id);
            } else {
                this.selectedIds.delete(id);
            }
        }

        this.render();
        this.emitSelectionChanged();
    }

     render(): void {
        this.root.innerHTML = "";

        if (this.treeNodes.length === 0) {
            this.root.innerHTML = `
                <div class="wf-tag-empty">
                    <i data-lucide="git-branch"></i>
                    <strong>조회된 노드가 없습니다.</strong>
                    <span>디바이스 연결 상태 또는 OPC 노드 조회 결과를 확인하세요.</span>
                </div>
            `;
            return;
        }

        const fragment = document.createDocumentFragment();

        for (const node of this.treeNodes) {
            fragment.appendChild(this.createNodeElement(node, 0));
        }

        this.root.appendChild(fragment);
    }

    bindEvents(): void {
        this.root.addEventListener("click", event => {
            const target = event.target as HTMLElement | null;

            if (target == null) {
                return;
            }

            const toggle = target.closest<HTMLElement>("[data-wf-tree-toggle]");

            if (toggle != null) {
                event.preventDefault();
                event.stopPropagation();

                const key = toggle.getAttribute("data-wf-tree-key") ?? "";
                const node = this.nodeByKey.get(key);

                if (node == null) {
                    return;
                }

                this.toggleExpanded(node);
                return;
            }

            const check = target.closest<HTMLInputElement>("[data-wf-tree-check]");

            if (check == null) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();

            const key = check.getAttribute("data-wf-tree-key") ?? "";
            const node = this.nodeByKey.get(key);

            if (node == null) {
                return;
            }

            const nextChecked = !this.isNodeChecked(node);

            this.toggleNode(node, nextChecked);
        });
    }

    isNodeChecked(node: TNode): boolean {
        if (this.isSelectable(node)) {
            return this.selectedIds.has(this.getId(node));
        }

        const childNodes = this.getDescendantSelectableNodes(node);

        return childNodes.length > 0
            && childNodes.every(child => this.selectedIds.has(this.getId(child)));
    }

    toggleExpanded(node: TNode): void {
        const id = this.getId(node);

        if (this.collapsedIds.has(id)) {
            this.collapsedIds.delete(id);
        } else {
            this.collapsedIds.add(id);
        }

        this.render();
    }

    collapseAllGroups(): void {
        const walk = (node: TNode): void => {
            const children = this.getChildren(node) ?? [];

            if (children.length > 0) {
                this.collapsedIds.add(this.getId(node));
            }

            for (const child of children) {
                walk(child);
            }
        };

        for (const root of this.treeNodes) {
            walk(root);
        }
    }

    toggleNode(node: TNode, checked: boolean): void {
        if (this.isSelectable(node)) {
            this.toggleSingleNode(node, checked);
        } else if (this.cascadeCheck) {
            const childNodes = this.getDescendantSelectableNodes(node);

            for (const child of childNodes) {
                this.toggleSingleNode(child, checked);
            }
        }

        this.render();
        this.emitSelectionChanged();
    }

    toggleSingleNode(node: TNode, checked: boolean): void {
        const id = this.getId(node);

        if (checked) {
            this.selectedIds.add(id);
        } else {
            this.selectedIds.delete(id);
        }
    }

    createNodeElement(node: TNode, depth: number): HTMLElement {
        const id = this.getId(node);
        const key = this.getKey(node);
        const children = this.getChildren(node) ?? [];
        const hasChildren = children.length > 0;
        const selectable = this.isSelectable(node);
        const selected = this.selectedIds.has(id);
        const isCollapsed = this.collapsedIds.has(id);

        const childSelectableNodes = this.getDescendantSelectableNodes(node);
        const selectedChildCount = childSelectableNodes.filter(x => this.selectedIds.has(this.getId(x))).length;
        const hasSelectedChild = selectedChildCount > 0;
        const allChildSelected = childSelectableNodes.length > 0 && selectedChildCount === childSelectableNodes.length;

        const wrapper = document.createElement("div");
        wrapper.className = `${this.classPrefix}-node`;

        const row = document.createElement("div");
        row.className = [
            `${this.classPrefix}-row`,
            selected ? "is-selected" : "",
            hasSelectedChild ? "has-selected-child" : "",
            isCollapsed ? "is-collapsed" : "",
            hasChildren ? "has-children" : ""
        ].filter(Boolean).join(" ");

        row.style.paddingLeft = `${depth * 14 + 7}px`;
        row.title = this.getTooltip?.(node) ?? id;

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = `${this.classPrefix}-toggle ${hasChildren ? "" : "is-placeholder"}`;
        toggle.setAttribute("data-wf-tree-toggle", "true");
        toggle.setAttribute("data-wf-tree-key", key);
        toggle.setAttribute("aria-label", isCollapsed ? "펼치기" : "접기");
        toggle.setAttribute("aria-expanded", String(!isCollapsed));
        toggle.textContent = hasChildren ? "⌄" : "";

        row.appendChild(toggle);

        const check = document.createElement("input");
        check.type = "checkbox";
        check.className = `form-check-input ${this.classPrefix}-check`;
        check.setAttribute("data-wf-tree-check", "true");
        check.setAttribute("data-wf-tree-key", key);

        if (selectable) {
            check.checked = selected;
        } else {
            check.checked = allChildSelected;
            check.indeterminate = hasSelectedChild && !allChildSelected;
            check.disabled = childSelectableNodes.length === 0;
        }

        row.appendChild(check);

        const title = document.createElement("span");
        title.className = `${this.classPrefix}-title`;
        title.textContent = this.getText(node);
        row.appendChild(title);

        if (!selectable && this.showCount) {
            const count = document.createElement("span");
            count.className = `${this.classPrefix}-count`;
            count.textContent = String(childSelectableNodes.length);
            row.appendChild(count);
        }

        wrapper.appendChild(row);

        if (!isCollapsed) {
            for (const child of children) {
                wrapper.appendChild(this.createNodeElement(child, depth + 1));
            }
        }

        return wrapper;
    }

    getDescendantSelectableNodes(node: TNode): TNode[] {
        const result: TNode[] = [];

        const walk = (target: TNode): void => {
            if (this.isSelectable(target)) {
                result.push(target);
            }

            for (const child of this.getChildren(target) ?? []) {
                walk(child);
            }
        };

        walk(node);

        return result;
    }

    emitSelectionChanged(): void {
        this.onSelectionChanged?.(this.getSelectedNodes());
    }

    getKey(node: TNode): string {
        const id = this.getId(node);
        const exists = this.keyById.get(id);

        if (exists != null) {
            this.nodeByKey.set(exists, node);
            return exists;
        }

        const key = `node_${++this.keySeq}`;

        this.keyById.set(id, key);
        this.nodeByKey.set(key, node);

        return key;
    }

    resetKeys(): void {
        this.keySeq = 0;
        this.keyById.clear();
        this.nodeByKey.clear();
    }

    resolveElement(selector: string | HTMLElement): HTMLElement {
        if (typeof selector !== "string") {
            return selector;
        }

        const element = document.querySelector<HTMLElement>(selector);

        if (element == null) {
            throw new Error(`WebFlexCheckTree element not found. selector=${selector}`);
        }

        return element;
    }
}