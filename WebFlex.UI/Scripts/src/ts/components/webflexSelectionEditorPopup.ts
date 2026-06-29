import { notify } from "../framework/notify";
import { escapeHtml } from "../framework/common";
import { WebFlexCheckTree, type WebFlexTreeItem } from "./webflexCheckTree";
import { WebFlexPopup, type WebFlexPopupOptions } from "./webflexPopup";

export type WebFlexSelectionEditorField = {
    key: string;
    title: string;
    type?: "text" | "checkbox" | "readonly";
    required?: boolean;
    width?: string;
    defaultValue?: any;
    getValue?: (item: WebFlexTreeItem) => any;
};

export type WebFlexSelectionEditorItem = {
    item: WebFlexTreeItem;
    checked: boolean;
    values: Record<string, any>;
};

export type WebFlexSelectionEditorPopupOpenOptions = WebFlexPopupOptions & {
    title?: string;
    subtitle?: string;
    items: WebFlexTreeItem[];
    treeItems?: WebFlexTreeItem[];
};

export type WebFlexSelectionEditorPopupOptions = WebFlexPopupOptions & {
    selector: string;
    fields: WebFlexSelectionEditorField[];
    cascadeCheck?: boolean;
    saveButtonText?: (count: number) => string;
    onSave: (items: WebFlexSelectionEditorItem[]) => Promise<boolean>;
};

export class WebFlexSelectionEditorPopup {
    readonly root: HTMLElement;
    readonly fields: WebFlexSelectionEditorField[];
    readonly saveButtonText: (count: number) => string;
    readonly onSave: (items: WebFlexSelectionEditorItem[]) => Promise<boolean>;

    readonly popup: WebFlexPopup;
    tree: WebFlexCheckTree | null = null;

    items: WebFlexTreeItem[] = [];
    treeItems: WebFlexTreeItem[] = [];
    selectedItems: WebFlexSelectionEditorItem[] = [];

    constructor(options: WebFlexSelectionEditorPopupOptions) {
        this.root = this.resolveElement(options.selector);
        this.fields = options.fields;
        this.saveButtonText = options.saveButtonText ?? (count => count > 0 ? `저장 (${count})` : "저장");
        this.onSave = options.onSave;

        this.popup = new WebFlexPopup({
            selector: options.selector,
            widthPercent: options.widthPercent,
            heightPercent: options.heightPercent
        });

        this.initTree(options.cascadeCheck ?? true);
        this.bindEvents();
    }

    open(options: WebFlexSelectionEditorPopupOpenOptions): void {
        this.items = options.items;
        this.treeItems = options.treeItems ?? this.buildTree(options.items);
        this.selectedItems = [];

        if (options.title != null) {
            this.root.querySelector("[data-popup-title]")!.textContent = options.title;
        }

        if (options.subtitle != null) {
            this.root.querySelector("[data-popup-subtitle]")!.textContent = options.subtitle;
        }

        this.tree?.setItems(this.items, this.treeItems);
        this.renderSelectedTable();
        this.updateCount();

        this.popup.open({
            widthPercent: options.widthPercent,
            heightPercent: options.heightPercent
        });

        this.refreshIcons();
    }

    close(): void {
        this.popup.close();
    }

    initTree(cascadeCheck: boolean): void {
        const treeHost = this.root.querySelector<HTMLElement>("[data-tree-host]");

        if (treeHost == null) {
            return;
        }

        this.tree = new WebFlexCheckTree<WebFlexTreeItem>({
            selector: treeHost,
            items: [],
            treeItems: [],
            cascadeCheck,
            classPrefix: "wf-tag-tree",
            onSelectionChanged: (items: WebFlexTreeItem[]) => {
                this.syncSelectedItems(items);
            }
        });
    }

    bindEvents(): void {
        this.root.addEventListener("click", event => {
            const target = event.target as HTMLElement | null;

            if (target?.closest("[data-select-all]") != null) {
                const allSelected = this.tree?.isAllSelected() === true;
                this.tree?.toggleAll(!allSelected);
                return;
            }

            if (target?.closest("[data-save]") != null) {
                void this.save();
                return;
            }

            const removeButton = target?.closest<HTMLElement>("[data-remove-row]");
            if (removeButton != null) {
                const id = removeButton.getAttribute("data-id") ?? "";
                const nextIds = this.selectedItems
                    .filter(x => x.item.id !== id)
                    .map(x => x.item.id);

                this.tree?.setSelectedIds(nextIds);
            }
        });

        this.root.addEventListener("change", event => {
            const target = event.target as HTMLInputElement | null;

            if (target == null) {
                return;
            }

            if (target.matches("[data-check-all]")) {
                for (const selected of this.selectedItems) {
                    selected.checked = target.checked;
                }

                this.renderSelectedTable();
                return;
            }

            if (target.matches("[data-row-check]")) {
                const id = target.getAttribute("data-id") ?? "";
                const selected = this.selectedItems.find(x => x.item.id === id);

                if (selected != null) {
                    selected.checked = target.checked;
                }

                this.updateCount();
                return;
            }

            if (target.matches("[data-field]")) {
                const id = target.getAttribute("data-id") ?? "";
                const key = target.getAttribute("data-key") ?? "";
                const selected = this.selectedItems.find(x => x.item.id === id);

                if (selected != null) {
                    selected.values[key] = target.type === "checkbox"
                        ? target.checked
                        : target.value;
                }
            }
        });

        this.root.addEventListener("input", event => {
            const target = event.target as HTMLInputElement | null;

            if (target?.matches("[data-field]") !== true) {
                return;
            }

            const id = target.getAttribute("data-id") ?? "";
            const key = target.getAttribute("data-key") ?? "";
            const selected = this.selectedItems.find(x => x.item.id === id);

            if (selected != null) {
                selected.values[key] = target.value;
            }
        });
    }

    syncSelectedItems(items: WebFlexTreeItem[]): void {
        const oldMap = new Map(this.selectedItems.map(x => [x.item.id, x]));

        this.selectedItems = items.map(item => {
            const exists = oldMap.get(item.id);

            if (exists != null) {
                return exists;
            }

            return {
                item,
                checked: true,
                values: this.createDefaultValues(item)
            };
        });

        this.renderSelectedTable();
    }

    createDefaultValues(item: WebFlexTreeItem): Record<string, any> {
        const values: Record<string, any> = {};

        for (const field of this.fields) {
            if (field.getValue != null) {
                values[field.key] = field.getValue(item);
            } else if (field.defaultValue != null) {
                values[field.key] = field.defaultValue;
            } else {
                values[field.key] = "";
            }
        }

        return values;
    }

    renderSelectedTable(): void {
        const host = this.root.querySelector<HTMLElement>("[data-selected-host]");
        const empty = this.root.querySelector<HTMLElement>("[data-empty-host]");
        const head = this.root.querySelector<HTMLElement>("[data-selected-head]");

        if (host == null || head == null) {
            return;
        }

        head.innerHTML = this.createHeaderHtml();
        host.innerHTML = "";

        if (empty != null) {
            empty.classList.toggle("d-none", this.selectedItems.length > 0);
        }

        for (const selected of this.selectedItems) {
            host.insertAdjacentHTML("beforeend", this.createRowHtml(selected));
        }

        this.updateCount();
    }

    createHeaderHtml(): string {
        const fields = this.fields.map(field => {
            const width = field.width == null ? "" : ` style="width:${field.width}"`;
            return `<th${width}>${escapeHtml(field.title)}${field.required ? ` <span class="text-danger">*</span>` : ""}</th>`;
        }).join("");

        return `
            <tr>
                <th class="wf-check-col">✓</th>
                <th>항목</th>
                ${fields}
                <th></th>
            </tr>
        `;
    }

    createRowHtml(selected: WebFlexSelectionEditorItem): string {
        const id = escapeHtml(selected.item.id);
        const fields = this.fields.map(field => this.createFieldHtml(selected, field)).join("");

        return `
            <tr>
                <td class="wf-check-col">
                    <input type="checkbox"
                           class="form-check-input"
                           data-row-check
                           data-id="${id}"
                           ${selected.checked ? "checked" : ""} />
                </td>
                <td>
                    <div class="wf-node-origin">
                        <strong>${escapeHtml(selected.item.text)}</strong>
                        <span>${id}</span>
                    </div>
                </td>
                ${fields}
                <td class="text-center">
                    <button type="button"
                            class="wf-row-remove"
                            data-remove-row
                            data-id="${id}">×</button>
                </td>
            </tr>
        `;
    }

    createFieldHtml(selected: WebFlexSelectionEditorItem, field: WebFlexSelectionEditorField): string {
        const id = escapeHtml(selected.item.id);
        const key = escapeHtml(field.key);
        const value = selected.values[field.key];

        if (field.type === "checkbox") {
            return `
                <td class="text-center">
                    <input type="checkbox"
                           class="form-check-input"
                           data-field
                           data-id="${id}"
                           data-key="${key}"
                           ${value === true ? "checked" : ""} />
                </td>
            `;
        }

        if (field.type === "readonly") {
            return `<td>${escapeHtml(value)}</td>`;
        }

        return `
            <td>
                <input type="text"
                       class="form-control"
                       data-field
                       data-id="${id}"
                       data-key="${key}"
                       value="${escapeHtml(value)}" />
            </td>
        `;
    }

    updateCount(): void {
        const selectableCount = this.tree?.getSelectableItems().length ?? 0;
        const selectedCount = this.selectedItems.length;
        const checkedCount = this.selectedItems.filter(x => x.checked).length;

        this.setText("[data-selected-count]", `${selectedCount}/${selectableCount}`);
        this.setText("[data-selected-text]", `${checkedCount}/${selectedCount}개 선택`);
        this.setText("[data-loaded-text]", `총 ${selectableCount.toLocaleString()}개 항목 로드됨`);
        this.setText("[data-loaded-count]", `${selectableCount.toLocaleString()}개 로드됨`);
        this.setText("[data-save-text]", this.saveButtonText(selectedCount));

        const checkAll = this.root.querySelector<HTMLInputElement>("[data-check-all]");
        if (checkAll != null) {
            checkAll.checked = selectedCount > 0 && checkedCount === selectedCount;
            checkAll.indeterminate = checkedCount > 0 && checkedCount < selectedCount;
        }
    }

    async save(): Promise<void> {
        if (this.selectedItems.length === 0) {
            notify.warning("저장할 항목을 선택해 주세요.");
            return;
        }

        for (const field of this.fields.filter(x => x.required)) {
            const invalid = this.selectedItems.find(x => String(x.values[field.key] ?? "").trim().length === 0);

            if (invalid != null) {
                notify.warning(`${field.title} 값을 입력해 주세요.`);
                return;
            }
        }

        const success = await this.onSave(this.selectedItems);

        if (success) {
            this.close();
        }
    }

    buildTree(items: WebFlexTreeItem[]): WebFlexTreeItem[] {
        const map = new Map<string, WebFlexTreeItem>();
        const roots: WebFlexTreeItem[] = [];

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

    setText(selector: string, text: string): void {
        const element = this.root.querySelector<HTMLElement>(selector);

        if (element != null) {
            element.textContent = text;
        }
    }

    refreshIcons(): void {
        const lucide = (window as any).lucide;

        if (lucide?.createIcons != null) {
            lucide.createIcons();
        }
    }

    resolveElement(selector: string): HTMLElement {
        const element = document.querySelector<HTMLElement>(selector);

        if (element == null) {
            throw new Error(`WebFlexSelectionEditorPopup element not found. selector=${selector}`);
        }

        return element;
    }
}