import {
    TabulatorFull as Tabulator,
    type ColumnDefinition,
    type Options
} from "tabulator-tables";

export type WebFlexGridRow = Record<string, unknown>;

export type WebFlexGridColumn = ColumnDefinition;

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
};

type TabulatorInstance = {
    setData: (data?: unknown[]) => Promise<void>;
    replaceData: (data?: unknown[]) => Promise<void>;
    clearData: () => Promise<void>;
    redraw: (force?: boolean) => void;
    destroy: () => void;
    getSelectedData: () => unknown[];
};

export class WebFlexGrid<TRow extends object = WebFlexGridRow> {
    private readonly table: TabulatorInstance;

    public constructor(options: WebFlexGridOptions<TRow>) {
        const element = this.resolveElement(options.selector);

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

        this.table = new Tabulator(element, tableOptions);
    }

    public get instance(): TabulatorInstance {
        return this.table;
    }

    public async setData(rows: TRow[]): Promise<void> {
        await this.table.setData(rows);
    }

    public async replaceData(rows: TRow[]): Promise<void> {
        await this.table.replaceData(rows);
    }

    public async clearData(): Promise<void> {
        await this.table.clearData();
    }

    public redraw(force: boolean = false): void {
        this.table.redraw(force);
    }

    public destroy(): void {
        this.table.destroy();
    }

    public getSelectedData(): TRow[] {
        return this.table.getSelectedData() as TRow[];
    }

    private resolveElement(selector: string | HTMLElement): HTMLElement {
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