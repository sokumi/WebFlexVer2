import {
    TabulatorFull as Tabulator,
    type ColumnDefinition,
    type Options
} from "tabulator-tables";

export type WebFlexGridRow = Record<string, unknown>;

export type WebFlexGridColumn = ColumnDefinition;

export type WebFlexGridRowComponent<TRow extends object = WebFlexGridRow> = {
    getData: () => TRow;
    select?: () => void;
    deselect?: () => void;
};

export type WebFlexGridOptions<TRow extends object = WebFlexGridRow> = {
    selector: string | HTMLElement;
    columns: WebFlexGridColumn[];
    data?: TRow[];
    ajaxUrl?: string;
    height?: string | number;
    layout?: "fitColumns" | "fitData" | "fitDataFill" | "fitDataStretch" | "fitDataTable";
    pagination?: boolean;
    paginationSize?: number;
    movableColumns?: boolean;
    selectableRows?: boolean | number;
    placeholder?: string;
    options?: Options;

    onRowClick?: (row: TRow, event: Event) => void;
    onRowDoubleClick?: (row: TRow, event: Event) => void;
    onSelectionChanged?: (rows: TRow[]) => void;
};

type TabulatorInstance = {
    setData: (data?: unknown[]) => Promise<void>;
    replaceData: (data?: unknown[]) => Promise<void>;
    clearData: () => Promise<void>;

    addRow: (data?: unknown, top?: boolean) => Promise<unknown>;
    updateData: (data?: unknown[]) => Promise<void>;
    deleteRow: (row: unknown) => Promise<void>;

    redraw: (force?: boolean) => void;
    destroy: () => void;

    getData: () => unknown[];
    getSelectedData: () => unknown[];
    deselectRow: () => void;

    setFilter: (field: string, type: string, value: unknown) => void;
    clearFilter: () => void;

    on: (eventName: string, callback: (...args: unknown[]) => void) => void;
};

export class WebFlexGrid<TRow extends object = WebFlexGridRow> {
    readonly table: TabulatorInstance;
    readonly element: HTMLElement;

    constructor(options: WebFlexGridOptions<TRow>) {
        this.element = this.resolveElement(options.selector);

        const tableOptions: Options = {
            layout: options.layout ?? "fitColumns",
            height: options.height,
            columns: options.columns,
            data: options.data,
            ajaxURL: options.ajaxUrl,
            movableColumns: options.movableColumns ?? true,
            selectableRows: options.selectableRows ?? false,
            placeholder: options.placeholder ?? "조회된 데이터가 없습니다.",
            pagination: options.pagination === true,
            paginationSize: options.paginationSize ?? 20,
            ...options.options
        };

        this.table = new Tabulator(this.element, tableOptions);

        this.bindEvents(options);
    }

    get instance(): TabulatorInstance {
        return this.table;
    }

    async setData(rows: TRow[]): Promise<void> {
        await this.table.setData(rows);
    }

    async replaceData(rows: TRow[]): Promise<void> {
        await this.table.replaceData(rows);
    }

    async clearData(): Promise<void> {
        await this.table.clearData();
    }

    async addRow(row: TRow, top: boolean = false): Promise<void> {
        await this.table.addRow(row, top);
    }

    async updateData(rows: TRow[]): Promise<void> {
        await this.table.updateData(rows);
    }

    async deleteRow(row: TRow): Promise<void> {
        await this.table.deleteRow(row);
    }

    getData(): TRow[] {
        return this.table.getData() as TRow[];
    }

    getSelectedData(): TRow[] {
        return this.table.getSelectedData() as TRow[];
    }

    clearSelection(): void {
        this.table.deselectRow();
    }

    setFilter(field: string, type: string, value: unknown): void {
        this.table.setFilter(field, type, value);
    }

    clearFilter(): void {
        this.table.clearFilter();
    }

    redraw(force: boolean = false): void {
        this.table.redraw(force);
    }

    refreshLayout(): void {
        window.requestAnimationFrame(() => {
            this.table.redraw(true);
        });
    }

    destroy(): void {
        this.table.destroy();
    }

    showLoading(message: string = "조회 중입니다..."): void {
        this.element.classList.add("is-loading");
        this.element.setAttribute("data-loading-message", message);
    }

    hideLoading(): void {
        this.element.classList.remove("is-loading");
        this.element.removeAttribute("data-loading-message");
    }

    selectRowByField<TKey extends keyof TRow>(field: TKey, value: TRow[TKey]): boolean {
        const rows = this.getData();

        const target = rows.find(row => row[field] === value);

        if (target == null) {
            return false;
        }

        // Tabulator row component 직접 접근은 타입 선언을 단순화해둔 상태라,
        // 현재는 데이터 기준 재선택 대신 선택값 유지용으로 true만 반환.
        // 실제 row select까지 필요하면 getRows 타입을 추가해서 확장하면 됨.
        return true;
    }

    bindEvents(options: WebFlexGridOptions<TRow>): void {
        if (options.onRowClick != null) {
            this.table.on("rowClick", (event: unknown, row: unknown) => {
                const rowComponent = row as WebFlexGridRowComponent<TRow>;
                options.onRowClick?.(rowComponent.getData(), event as Event);
            });
        }

        if (options.onRowDoubleClick != null) {
            this.table.on("rowDblClick", (event: unknown, row: unknown) => {
                const rowComponent = row as WebFlexGridRowComponent<TRow>;
                options.onRowDoubleClick?.(rowComponent.getData(), event as Event);
            });
        }

        if (options.onSelectionChanged != null) {
            this.table.on("rowSelectionChanged", (data: unknown) => {
                options.onSelectionChanged?.(data as TRow[]);
            });
        }
    }

    resolveElement(selector: string | HTMLElement): HTMLElement {
        if (typeof selector !== "string") {
            return selector;
        }

        const element = document.querySelector<HTMLElement>(selector);

        if (element == null) {
            throw new Error(`Grid element not found. selector=${selector}`);
        }

        return element;
    }
}