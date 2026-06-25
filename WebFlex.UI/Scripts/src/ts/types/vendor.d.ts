declare module "*.css"; declare module "tabulator-tables" {
    export type ColumnDefinition = Record<string, unknown>;
    export type Options = Record<string, unknown>;

    export type CellComponent = {
        getValue: () => unknown;
        getData: () => unknown;
        getRow: () => unknown;
    };

    export class TabulatorFull {
        constructor(element: HTMLElement, options: Options);

        setData(data?: unknown[]): Promise<void>;
        replaceData(data?: unknown[]): Promise<void>;
        clearData(): Promise<void>;

        addRow(data?: unknown, top?: boolean): Promise<unknown>;
        updateData(data?: unknown[]): Promise<void>;
        deleteRow(row: unknown): Promise<void>;

        redraw(force?: boolean): void;
        destroy(): void;

        getData(): unknown[];
        getSelectedData(): unknown[];
        deselectRow(): void;

        setFilter(field: string, type: string, value: unknown): void;
        clearFilter(): void;

        on(eventName: string, callback: (...args: unknown[]) => void): void;
    }
}

declare module "bootstrap" {
    export class Modal {
        constructor(element: Element, options?: Record<string, unknown>);
        show(): void;
        hide(): void;
        toggle(): void;
        dispose(): void;
    }
}