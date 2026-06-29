export type WebFlexTreeItem = {
    id: string;
    parentId?: string | null;
    text: string;
    tooltip?: string | null;
    selectable?: boolean;
    data?: any;
    children?: WebFlexTreeItem[];
};

export type WebFlexCheckTreeOptions<TItem extends WebFlexTreeItem = WebFlexTreeItem> = {
    selector: string | HTMLElement;
    items?: TItem[];
    treeItems?: TItem[];
    cascadeCheck?: boolean;
    showCount?: boolean;
    defaultExpanded?: boolean;
    classPrefix?: string;
    onSelectionChanged?: (items: TItem[]) => void;
};

export class WebFlexCheckTree<TItem extends WebFlexTreeItem = WebFlexTreeItem> {
    readonly element: HTMLElement;
    readonly cascadeCheck: boolean;
    readonly showCount: boolean;
    readonly defaultExpanded: boolean;
    readonly classPrefix: string;

    onSelectionChanged?: (items: TItem[]) => void;

    items: TItem[] = [];
    treeItems: TItem[] = [];
    selectedIds = new Set<string>();
    collapsedIds = new Set<string>();

    constructor(options: WebFlexCheckTreeOptions<TItem>) {
        this.element = this.resolveElement(options.selector);
        this.items = options.items ?? [];
        this.treeItems = options.treeItems ?? this.buildTree(this.items);
        this.cascadeCheck = options.cascadeCheck ?? true;
        this.showCount = options.showCount ?? true;
        this.defaultExpanded = options.defaultExpanded ?? true;
        this.classPrefix = options.classPrefix ?? "wf-check-tree";
        this.onSelectionChanged = options.onSelectionChanged;

        this.bindEvents();
        this.render();
    }

    setItems(items: TItem[], treeItems?: TItem[]): void {
        this.items = items;
        this.treeItems = treeItems ?? this.buildTree(items);
        this.selectedIds.clear();
        this.collapsedIds.clear();

        if (!this.defaultExpanded) {
            this.collapseAll();
        }

        this.render();
        this.emitSelectionChanged();
    }

    clear(): void {
        this.setItems([]);
    }

    getSelectedIds(): string[] {
        return Array.from(this.selectedIds);
    }

    getSelectedItems(): TItem[] {
        return this.getAllTreeItems().filter(x => this.selectedIds.has(x.id));
    }

    getSelectableItems(): TItem[] {
        return this.getAllTreeItems().filter(x => this.isSelectable(x));
    }

    private getAllTreeItems(): TItem[] {
        const result: TItem[] = [];

        const walk = (item: TItem): void => {
            result.push(item);

            for (const child of item.children ?? []) {
                walk(child as TItem);
            }
        };

        for (const item of this.treeItems) {
            walk(item);
        }

        return result;
    }

    setSelectedIds(ids: string[]): void {
        this.selectedIds = new Set(ids);
        this.render();
        this.emitSelectionChanged();
    }

    clearSelection(): void {
        this.selectedIds.clear();
        this.render();
        this.emitSelectionChanged();
    }

    isAllSelected(): boolean {
        const selectable = this.getSelectableItems();
        return selectable.length > 0 && selectable.every(x => this.selectedIds.has(x.id));
    }

    toggleAll(checked: boolean): void {
        for (const item of this.getSelectableItems()) {
            if (checked) {
                this.selectedIds.add(item.id);
            } else {
                this.selectedIds.delete(item.id);
            }
        }

        this.render();
        this.emitSelectionChanged();
    }

    render(): void {
        this.element.innerHTML = "";

        if (this.treeItems.length === 0) {
            this.element.innerHTML = `
                <div class="wf-tag-empty">
                    <i data-lucide="git-branch"></i>
                    <strong>조회된 항목이 없습니다.</strong>
                    <span>데이터를 조회한 뒤 다시 시도하세요.</span>
                </div>
            `;
            return;
        }

        const fragment = document.createDocumentFragment();

        for (const item of this.treeItems) {
            fragment.appendChild(this.createItemElement(item, 0));
        }

        this.element.appendChild(fragment);
    }

    buildTree(items: TItem[]): TItem[] {
        const map = new Map<string, TItem>();
        const roots: TItem[] = [];

        for (const item of items) {
            map.set(item.id, {
                ...item,
                children: []
            });
        }

        for (const item of map.values()) {
            if (item.parentId && map.has(item.parentId)) {
                map.get(item.parentId)?.children?.push(item);
            } else {
                roots.push(item);
            }
        }

        return roots;
    }

    private bindEvents(): void {
        this.element.addEventListener("click", event => {
            const target = event.target as HTMLElement | null;

            if (target == null) {
                return;
            }

            const toggle = target.closest<HTMLElement>("[data-wf-tree-toggle]");
            if (toggle != null) {
                event.preventDefault();
                event.stopPropagation();

                const id = toggle.getAttribute("data-wf-tree-id") ?? "";
                const item = this.findItem(id);

                if (item != null) {
                    this.toggleExpanded(item);
                }

                return;
            }

            const check = target.closest<HTMLInputElement>("[data-wf-tree-check]");
            if (check != null) {
                event.preventDefault();
                event.stopPropagation();

                const id = check.getAttribute("data-wf-tree-id") ?? "";
                const item = this.findItem(id);

                if (item != null) {
                    this.toggleItem(item, !this.isItemChecked(item));
                }
            }
        });
    }

    private findItem(id: string): TItem | null {
        return this.getAllTreeItems().find(x => x.id === id) ?? null;
    }


    private isSelectable(item: TItem): boolean {
        if (item.selectable != null) {
            return item.selectable;
        }

        return (item.children ?? []).length === 0;
    }

    private isItemChecked(item: TItem): boolean {
        const children = this.getDescendantSelectableItems(item, false);

        if (children.length > 0) {
            return children.every(x => this.selectedIds.has(x.id));
        }

        return this.isSelectable(item) && this.selectedIds.has(item.id);
    }

    private toggleExpanded(item: TItem): void {
        if (this.collapsedIds.has(item.id)) {
            this.collapsedIds.delete(item.id);
        } else {
            this.collapsedIds.add(item.id);
        }

        this.render();
    }

    private toggleItem(item: TItem, checked: boolean): void {
        const children = this.getDescendantSelectableItems(item, false);

        if (this.cascadeCheck && children.length > 0) {
            for (const child of children) {
                this.toggleSingleItem(child, checked);
            }
        } else if (this.isSelectable(item)) {
            this.toggleSingleItem(item, checked);
        }

        this.render();
        this.emitSelectionChanged();
    }

    private toggleSingleItem(item: TItem, checked: boolean): void {
        if (checked) {
            this.selectedIds.add(item.id);
        } else {
            this.selectedIds.delete(item.id);
        }
    }

    private createItemElement(item: TItem, depth: number): HTMLElement {
        const children = item.children ?? [];
        const hasChildren = children.length > 0;
        const selectable = this.isSelectable(item);
        const selected = this.selectedIds.has(item.id);
        const isCollapsed = this.collapsedIds.has(item.id);

        const childSelectableItems = this.getDescendantSelectableItems(item, false);
        const selectedChildCount = childSelectableItems.filter(x => this.selectedIds.has(x.id)).length;
        const hasSelectedChild = selectedChildCount > 0;
        const allChildSelected = childSelectableItems.length > 0 && selectedChildCount === childSelectableItems.length;

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
        row.title = item.tooltip ?? item.id;

        const toggle = document.createElement("button");
        toggle.type = "button";
        toggle.className = `${this.classPrefix}-toggle ${hasChildren ? "" : "is-placeholder"}`;
        toggle.setAttribute("data-wf-tree-toggle", "true");
        toggle.setAttribute("data-wf-tree-id", item.id);
        toggle.setAttribute("aria-label", isCollapsed ? "펼치기" : "접기");
        toggle.setAttribute("aria-expanded", String(!isCollapsed));
        toggle.textContent = hasChildren ? "⌄" : "";

        row.appendChild(toggle);

        const check = document.createElement("input");
        check.type = "checkbox";
        check.className = `form-check-input ${this.classPrefix}-check`;
        check.setAttribute("data-wf-tree-check", "true");
        check.setAttribute("data-wf-tree-id", item.id);

        if (selectable) {
            check.checked = selected;
        } else {
            check.checked = allChildSelected;
            check.indeterminate = hasSelectedChild && !allChildSelected;
            check.disabled = childSelectableItems.length === 0;
        }

        row.appendChild(check);

        const title = document.createElement("span");
        title.className = `${this.classPrefix}-title`;
        title.textContent = item.text;
        row.appendChild(title);

        if (!selectable && this.showCount) {
            const count = document.createElement("span");
            count.className = `${this.classPrefix}-count`;
            count.textContent = String(childSelectableItems.length);
            row.appendChild(count);
        }

        wrapper.appendChild(row);

        if (!isCollapsed) {
            for (const child of children) {
                wrapper.appendChild(this.createItemElement(child as TItem, depth + 1));
            }
        }

        return wrapper;
    }

    private getDescendantSelectableItems(item: TItem, includeSelf = true): TItem[] {
        const result: TItem[] = [];

        const walk = (target: TItem, isSelf: boolean): void => {
            if ((includeSelf || !isSelf) && this.isSelectable(target)) {
                result.push(target);
            }

            for (const child of target.children ?? []) {
                walk(child as TItem, false);
            }
        };

        walk(item, true);

        return result;
    }

    private collapseAll(): void {
        const walk = (item: TItem): void => {
            if ((item.children ?? []).length > 0) {
                this.collapsedIds.add(item.id);
            }

            for (const child of item.children ?? []) {
                walk(child as TItem);
            }
        };

        for (const item of this.treeItems) {
            walk(item);
        }
    }

    private emitSelectionChanged(): void {
        this.onSelectionChanged?.(this.getSelectedItems());
    }

    private resolveElement(selector: string | HTMLElement): HTMLElement {
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