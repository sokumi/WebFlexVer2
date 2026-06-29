import {
    TabulatorFull as Tabulator,
    type ColumnDefinition,
    type Options
} from "tabulator-tables";

export type WebFlexGridRow = Record<string, any>;
export type WebFlexGridColumn = ColumnDefinition;

type TabulatorInstance = any;

export class WebFlexGrid<TRow extends object = WebFlexGridRow> {
    readonly element: HTMLElement;

    private table: TabulatorInstance | null = null;
    private tableOptions: Options = {};

    constructor(selector: string | HTMLElement, options: Options = {}) {
        this.element = this.resolveElement(selector);

        this.tableOptions = {
            layout: "fitColumns",
            height: "100%",
            movableColumns: true,
            selectableRows: false,
            placeholder: "조회된 데이터가 없습니다.",
            ...options
        };
    }

    static create<TRow extends object = WebFlexGridRow>(
        selector: string | HTMLElement,
        options: Options = {}
    ): WebFlexGrid<TRow> {
        return new WebFlexGrid<TRow>(selector, options);
    }

    options(options: Options): this {
        this.tableOptions = {
            ...this.tableOptions,
            ...options
        };

        this.applyOptions();
        return this;
    }

    height(value: string | number): this {
        return this.options({ height: value });
    }

    layout(value: "fitColumns" | "fitData" | "fitDataFill" | "fitDataStretch" | "fitDataTable"): this {
        return this.options({ layout: value });
    }

    columns(columns: WebFlexGridColumn[]): this {
        return this.options({ columns });
    }

    data(rows: TRow[]): this {
        return this.options({ data: rows });
    }

    pagination(pageSize: number = 20): this {
        return this.options({
            pagination: true,
            paginationSize: pageSize
        });
    }

    noPagination(): this {
        return this.options({
            pagination: false
        });
    }

    selectableRows(value: boolean | number = true): this {
        return this.options({
            selectableRows: value
        });
    }

    movableColumns(value: boolean = true): this {
        return this.options({
            movableColumns: value
        });
    }

    placeholder(value: string): this {
        return this.options({
            placeholder: value
        });
    }

    ajaxUrl(value: string): this {
        return this.options({
            ajaxURL: value
        });
    }

    onRowClick(callback: (row: TRow, event: Event, component: any) => void): this {
        return this.on("rowClick", (event: Event, row: any) => {
            callback(row.getData() as TRow, event, row);
        });
    }

    onRowDoubleClick(callback: (row: TRow, event: Event, component: any) => void): this {
        return this.on("rowDblClick", (event: Event, row: any) => {
            callback(row.getData() as TRow, event, row);
        });
    }

    onSelectionChanged(callback: (rows: TRow[]) => void): this {
        return this.on("rowSelectionChanged", (data: any[]) => {
            callback(data as TRow[]);
        });
    }

    on(eventName: string, callback: (...args: any[]) => void): this {
        if (this.table != null) {
            this.table.on(eventName, callback);
            return this;
        }

        const oldBuild = this.build.bind(this);

        this.build = () => {
            oldBuild();
            this.table?.on(eventName, callback);
            return this;
        };

        return this;
    }

    build(): this {
        if (this.table != null) {
            this.table.destroy();
        }

        this.table = new Tabulator(this.element, this.tableOptions);
        return this;
    }

    get instance(): TabulatorInstance {
        if (this.table == null) {
            this.build();
        }

        return this.table;
    }

    async setData(rows: TRow[] = []): Promise<void> {
        await this.instance.setData(rows);
    }

    async replaceData(rows: TRow[] = []): Promise<void> {
        await this.instance.replaceData(rows);
    }

    async clearData(): Promise<void> {
        await this.instance.clearData();
    }

    async addRow(row: TRow, top: boolean = false): Promise<void> {
        await this.instance.addRow(row, top);
    }

    async updateData(rows: TRow[]): Promise<void> {
        await this.instance.updateData(rows);
    }

    async deleteRow(row: any): Promise<void> {
        await this.instance.deleteRow(row);
    }

    async refresh(): Promise<void> {
        await this.instance.redraw(true);
    }

    getData(): TRow[] {
        return this.instance.getData() as TRow[];
    }

    getSelectedData(): TRow[] {
        return this.instance.getSelectedData() as TRow[];
    }

    clearSelection(): void {
        this.instance.deselectRow();
    }

    setFilter(field: string, type: string, value: any): this {
        this.instance.setFilter(field, type, value);
        return this;
    }

    clearFilter(): this {
        this.instance.clearFilter();
        return this;
    }

    redraw(force: boolean = false): void {
        this.instance.redraw(force);
    }

    refreshLayout(): void {
        window.requestAnimationFrame(() => {
            this.instance.redraw(true);
        });
    }

    destroy(): void {
        this.table?.destroy();
        this.table = null;
    }

    showLoading(message: string = "조회 중입니다..."): void {
        this.element.classList.add("is-loading");
        this.element.setAttribute("data-loading-message", message);
    }

    hideLoading(): void {
        this.element.classList.remove("is-loading");
        this.element.removeAttribute("data-loading-message");
    }

    private applyOptions(): void {
        if (this.table == null) {
            return;
        }

        this.table.setOptions?.(this.tableOptions);
    }

    private resolveElement(selector: string | HTMLElement): HTMLElement {
        if (typeof selector !== "string") {
            return selector;
        }

        const element = document.querySelector<HTMLElement>(selector);

        if (element == null) {
            throw new Error(`WebFlexGrid element not found. selector=${selector}`);
        }

        return element;
    }
}