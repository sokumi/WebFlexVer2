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
    private focusedRow: any = null;
    private focusRowOnClick: boolean = true;

    constructor(selector: string | HTMLElement, options: Options = {}) {
        this.element = this.resolveElement(selector);
        const webFlexOptions = options as any;
        this.focusRowOnClick = webFlexOptions.focusRowOnClick !== false;

        const tabulatorOptions = {
            ...options
        } as any;

        delete tabulatorOptions.focusRowOnClick;

        this.tableOptions = {
            layout: "fitColumns",
            height: "100%",
            movableColumns: true,
            selectableRows: false,
            placeholder: "조회된 데이터가 없습니다.",
            ...tabulatorOptions
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
            this.clearFocusedRow();
            this.table.destroy();
        }

        this.table = new Tabulator(this.element, this.tableOptions);
        this.bindFocusedRowClick();
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
        this.clearFocusedRow();
    }

    async replaceData(rows: TRow[] = []): Promise<void> {
        await this.instance.replaceData(rows);
        this.clearFocusedRow();
    }

    async clearData(): Promise<void> {
        await this.instance.clearData();
        this.clearFocusedRow();
    }

    async addRow(row: TRow, top: boolean = false): Promise<void> {
        await this.instance.addRow(row, top);
    }

    async updateData(rows: TRow[]): Promise<void> {
        await this.instance.updateData(rows);
    }

    /**
     * addData 후 스크롤 위치를 보존합니다.
     * 기본 addData 는 내부적으로 스크롤을 초기화하는 경우가 있어
     * 무한 스크롤 시 위치가 맨 위로 돌아가는 문제를 방지합니다.
     *
     * renderVertical: "virtual" (기본값) 사용 중 스크롤 도중 addData 로 데이터가
     * 계속 늘어나면, Tabulator 내부의 가상 렌더링 범위(scrollHeight, vDom top/bottom)
     * 계산이 어긋나 이후 위로 스크롤했을 때 이미 지나간 행이 빈 화면으로 보이는
     * 문제가 발생할 수 있습니다. addData 직후 redraw(true) 로 내부 range 를
     * 강제로 재계산시켜 방지합니다. 페이지 로딩 시에만 호출되므로
     * 실시간 셀 업데이트(patchRowCells) 성능에는 영향이 없습니다.
     */
    async addDataPreserveScroll(rows: TRow[]): Promise<void> {
        const holder = this.element.querySelector(".tabulator-tableholder") as HTMLElement | null;
        const scrollTop = holder?.scrollTop ?? 0;

        await this.instance.addData(rows);
        this.instance.redraw(true);

        if (holder != null && scrollTop > 0) {
            requestAnimationFrame(() => {
                holder.scrollTop = scrollTop;
            });
        }
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

    clearFocusedRow(): void {
        this.getFocusedRowElement()?.classList.remove("is-focused");
        this.focusedRow = null;
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
        this.clearFocusedRow();
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

    /**
     * 지정한 attribute 를 가진 행이 현재 화면(가상 DOM 렌더링 창)에
     * 실제로 존재하는지 저비용으로 확인합니다.
     *
     * Tabulator 의 getRow()/getCell() API 는 호출 시 화면 밖(virtual DOM 밖)
     * 행이라도 엘리먼트를 강제로 생성(materialize)합니다. 실시간 업데이트가
     * 초당 수십~수백 건 들어오는 상황에서 patchRowCells 를 화면 밖 행에도
     * 그대로 호출하면, 보이지도 않는 수천 개 행의 DOM 을 만들었다 버리는
     * 낭비가 누적되어 결국 렉으로 이어집니다.
     *
     * 사용법: rowFormatter 에서 행 엘리먼트에 attribute 를 심어두고
     * (예: element.setAttribute("data-key", data.rowKey)),
     * 실시간 업데이트 시 patchRowCells 호출 전에 이 메서드로 먼저
     * 화면에 렌더링된 행인지 확인한 뒤에만 patchRowCells 를 호출하세요.
     *
     * @param attributeName  rowFormatter 에서 심어둔 attribute 이름 (예: "data-key")
     * @param attributeValue 확인할 행의 키 값
     */
    isRowRendered(attributeName: string, attributeValue: any): boolean {
        const selector = `[${attributeName}="${this.escapeAttributeSelector(attributeValue)}"]`;
        return this.element.querySelector(selector) != null;
    }

    /**
     * Tabulator 의 포매터/재렌더링 파이프라인을 거치지 않고
     * 특정 행의 셀 innerHTML 을 직접 교체합니다.
     *
     * SSE 실시간 업데이트처럼 초당 수십~수백 건이 들어올 때
     * updateData() 대신 이 메서드를 사용하면 렌더링 부하를 대폭 줄일 수 있습니다.
     *
     * 주의: 화면 밖(virtual DOM 밖) 행에 대해 호출하면 Tabulator 가 해당 행의
     * 엘리먼트를 강제로 생성합니다. 대량 실시간 업데이트 시에는 isRowRendered() 로
     * 먼저 화면에 렌더링된 행인지 확인한 뒤 호출하는 것을 권장합니다.
     *
     * @param key    Tabulator index 필드 값 (rowKey)
     * @param patches  { 필드명: 새 innerHTML } 맵
     * @returns 행이 존재하면 true, 없으면 false
     */
    patchRowCells(key: any, patches: Record<string, string>): boolean {
        const row = this.instance?.getRow(key);

        if (row == null || row === false) {
            return false;
        }

        for (const [field, html] of Object.entries(patches)) {
            const cell = row.getCell(field);

            if (cell == null || cell === false) {
                continue;
            }

            cell.getElement().innerHTML = html;
        }

        return true;
    }

    /**
     * Tabulator 행의 루트 <tr> 엘리먼트를 반환합니다.
     * flash 클래스 토글 등 행 전체에 직접 접근할 때 사용합니다.
     *
     * @param key  Tabulator index 필드 값 (rowKey)
     * @returns HTMLElement 또는 null
     */
    getRowElement(key: any): HTMLElement | null {
        const row = this.instance?.getRow(key);

        if (row == null || row === false) {
            return null;
        }

        return row.getElement() as HTMLElement;
    }

    /**
     * .tabulator-tableholder 요소를 반환합니다.
     * 스크롤 위치 제어나 무한 스크롤 감지에 사용합니다.
     */
    getScrollHolder(): HTMLElement | null {
        return this.element.querySelector(".tabulator-tableholder") as HTMLElement | null;
    }

    private applyOptions(): void {
        if (this.table == null) {
            return;
        }

        this.table.setOptions?.(this.tableOptions);
    }

    private bindFocusedRowClick(): void {
        if (!this.focusRowOnClick || this.table == null) {
            return;
        }

        this.table.on("rowClick", (_event: Event, row: any) => {
            this.focusRow(row);
        });
    }

    private focusRow(row: any): void {
        this.clearFocusedRow();

        this.focusedRow = row;
        this.getFocusedRowElement()?.classList.add("is-focused");
    }

    private getFocusedRowElement(): HTMLElement | null {
        if (this.focusedRow == null || typeof this.focusedRow.getElement !== "function") {
            return null;
        }

        try {
            return this.focusedRow.getElement() as HTMLElement;
        } catch {
            return null;
        }
    }

    private escapeAttributeSelector(value: any): string {
        return String(value ?? "").replace(/\\/g, "\\\\").replace(/"/g, '\\"');
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
