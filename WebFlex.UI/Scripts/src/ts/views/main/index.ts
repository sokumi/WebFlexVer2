import type { CurrentValueDto } from "../../dtos/currentValueDto";

type CurrentValueRow = CurrentValueDto & {
    collectionSetting?: string | null;
    cookieValue?: string | null;
    updateCount?: number | null;
};

type CurrentValuePageResponse = {
    success: boolean;
    data: {
        totalCount: number;
        skip: number;
        take: number;
        rows: CurrentValueRow[];
    };
};

type CurrentValueGroupResponse = {
    success: boolean;
    data: CurrentValueGroupDto[];
};

type CurrentValueGroupDto = {
    groupId?: string | null;
    count: number;
    badCount: number;
};

type StreamStatus = "wait" | "connected" | "error";

export default class Page {
    rows: CurrentValueRow[] = [];
    rowMap = new Map<string, CurrentValueRow>();

    // 가상 스크롤로 실제 렌더링된 <tr> 만 추적 (key -> element)
    rowElements = new Map<string, HTMLTableRowElement>();

    eventSource: EventSource | null = null;

    totalCount = 0;
    loadedCount = 0;
    updateCount = 0;

    selectedGroupId = "";
    keyword = "";

    readonly pageSize = 120;
    isLoading = false;
    hasMore = true;

    searchTimer: number | null = null;
    flashKeys = new Set<string>();

    // 가상 스크롤 관련 상태
    readonly bufferRows = 8;
    readonly fallbackRowHeight = 40;
    measuredRowHeight: number | null = null;
    windowStart = -1;
    windowEnd = -1;
    renderWindowScheduled = false;

    init(): void {
        $("#btnToggleGroupPanel").on("click", () => {
            this.toggleGroupPanel();
        });

        $("#currentValueScroll").on("scroll", () => {
            this.handleScroll();
        });

        $("#groupFilterHost").on("click", "[data-group-id]", event => {
            const groupId = String($(event.currentTarget).attr("data-group-id") ?? "");
            this.selectGroup(groupId);
        });

        $("#btnRefreshCurrent").on("click", () => {
            this.keyword = String($("#txtKeyword").val() ?? "").trim();
            void this.reload();
        });

        $("#txtKeyword").on("input", () => {
            this.requestSearch();
        });

        $("#txtKeyword").on("keydown", event => {
            if (event.key === "Enter") {
                this.applySearchImmediately();
            }
        });

        window.addEventListener("resize", () => {
            this.scheduleRenderWindow();
        });

        void this.loadGroups();
        void this.reload();
        this.connectStream();

        window.addEventListener("beforeunload", () => {
            this.eventSource?.close();
        });
    }

    toggleGroupPanel(): void {
        const $panel = $("#currentGroupPanel");
        const collapsed = !$panel.hasClass("is-collapsed");

        $panel.toggleClass("is-collapsed", collapsed);
        $("#btnToggleGroupPanel").attr("aria-expanded", String(!collapsed));
        $("#lblGroupToggleText").text(collapsed ? "펼치기" : "접기");

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }

    async reload(): Promise<void> {
        this.rows = [];
        this.rowMap.clear();
        this.rowElements.clear();
        this.totalCount = 0;
        this.loadedCount = 0;
        this.hasMore = true;
        this.flashKeys.clear();
        this.windowStart = -1;
        this.windowEnd = -1;

        $("#currentValueBody").html(`
            <tr>
                <td colspan="10" class="wf-current-empty-cell">데이터를 불러오는 중입니다.</td>
            </tr>
        `);

        await this.loadNextPage();
    }

    async loadGroups(): Promise<void> {
        try {
            const response = await fetch("/main/list/groups");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const result = await response.json() as CurrentValueGroupResponse;

            if (!result.success) {
                return;
            }

            this.renderGroups(result.data ?? []);
        } catch (e) {
            console.error(e);
        }
    }

    renderGroups(groups: CurrentValueGroupDto[]): void {
        const total = groups.reduce((sum, item) => sum + item.count, 0);

        const html: string[] = [];

        html.push(`
            <button type="button"
                    class="wf-current-group-chip ${this.selectedGroupId === "" ? "is-active" : ""}"
                    data-group-id="">
                전체
                <span>${total.toLocaleString()}</span>
            </button>
        `);

        for (const group of groups) {
            const groupId = group.groupId ?? "";

            html.push(`
                <button type="button"
                        class="wf-current-group-chip ${this.selectedGroupId === groupId ? "is-active" : ""}"
                        data-group-id="${this.escapeHtml(groupId)}">
                    ${this.escapeHtml(groupId || "미지정")}
                    <span>${group.count.toLocaleString()}</span>
                    ${group.badCount > 0 ? `<em>${group.badCount.toLocaleString()}</em>` : ""}
                </button>
            `);
        }

        $("#groupFilterHost").html(html.join(""));
        $("#lblGroupSummary").text(this.selectedGroupId.length === 0 ? "전체 그룹" : this.selectedGroupId);

        this.refreshIcons();
    }

    selectGroup(groupId: string): void {
        if (this.selectedGroupId === groupId) {
            return;
        }

        this.selectedGroupId = groupId;

        $("#groupFilterHost [data-group-id]").removeClass("is-active");
        $(`#groupFilterHost [data-group-id="${this.escapeSelectorValue(groupId)}"]`).addClass("is-active");

        $("#lblGroupSummary").text(groupId.length === 0 ? "전체 그룹" : groupId);

        void this.reload();
    }

    requestSearch(): void {
        if (this.searchTimer != null) {
            window.clearTimeout(this.searchTimer);
            this.searchTimer = null;
        }

        this.searchTimer = window.setTimeout(() => {
            this.searchTimer = null;

            const nextKeyword = String($("#txtKeyword").val() ?? "").trim();

            if (this.keyword === nextKeyword) {
                return;
            }

            this.keyword = nextKeyword;
            void this.reload();
        }, 350);
    }

    applySearchImmediately(): void {
        if (this.searchTimer != null) {
            window.clearTimeout(this.searchTimer);
            this.searchTimer = null;
        }

        const nextKeyword = String($("#txtKeyword").val() ?? "").trim();

        if (this.keyword === nextKeyword) {
            return;
        }

        this.keyword = nextKeyword;
        void this.reload();
    }

    async loadNextPage(): Promise<void> {
        if (this.isLoading || !this.hasMore) {
            return;
        }

        this.isLoading = true;
        $("#currentValueLoading").removeClass("d-none");

        try {
            const query = new URLSearchParams();
            query.set("skip", String(this.loadedCount));
            query.set("take", String(this.pageSize));

            if (this.selectedGroupId.length > 0) {
                query.set("groupId", this.selectedGroupId);
            }

            if (this.keyword.length > 0) {
                query.set("keyword", this.keyword);
            }

            const response = await fetch(`/main/list/page?${query.toString()}`);

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const result = await response.json() as CurrentValuePageResponse;

            if (!result.success) {
                throw new Error("CurrentValue 조회 실패");
            }

            const page = result.data;
            const pageRows = (page.rows ?? []).map(x => this.normalizeCurrentRow(x));

            this.totalCount = page.totalCount;

            for (const row of pageRows) {
                const key = this.makeKey(row.groupId, row.tagId);

                if (!this.rowMap.has(key)) {
                    this.rows.push(row);
                }

                this.rowMap.set(key, row);
            }

            this.loadedCount = this.rows.length;
            this.hasMore = this.loadedCount < this.totalCount;

            this.updateHeaderCount();
            this.renderWindow(true);
        } catch (e) {
            console.error(e);

            if (this.rows.length === 0) {
                $("#currentValueBody").html(`
                    <tr>
                        <td colspan="10" class="wf-current-empty-cell">데이터 조회에 실패했습니다.</td>
                    </tr>
                `);
            }
        } finally {
            this.isLoading = false;
            $("#currentValueLoading").addClass("d-none");
        }
    }

    handleScroll(): void {
        const scrollEl = document.getElementById("currentValueScroll") as HTMLDivElement | null;

        if (scrollEl == null) {
            return;
        }

        const remain = scrollEl.scrollHeight - scrollEl.scrollTop - scrollEl.clientHeight;

        if (remain < 260) {
            void this.loadNextPage();
        }

        this.scheduleRenderWindow();
    }

    connectStream(): void {
        this.eventSource?.close();
        this.setStreamStatus("wait", "연결 중");

        const source = new EventSource("/main/list/stream");
        this.eventSource = source;

        source.addEventListener("connected", () => {
            this.setStreamStatus("connected", "연결됨");
        });

        source.addEventListener("currentvalue", (event: MessageEvent) => {
            try {
                const row = this.normalizeCurrentRow(JSON.parse(event.data));
                this.applyUpdate(row);
            } catch (e) {
                console.error("currentvalue parse error", e, event.data);
            }
        });

        source.onerror = e => {
            console.error("currentvalue stream error", e);
            this.setStreamStatus("error", "재연결 중");
        };
    }

    applyUpdate(row: CurrentValueRow): void {
        if (!this.isMatchedCurrentFilter(row)) {
            return;
        }

        const key = this.makeKey(row.groupId, row.tagId);
        const existing = this.rowMap.get(key);

        if (existing == null) {
            // 아직 사용자가 해당 위치까지 스크롤해서 불러오지 않은 데이터는
            // 무한 스크롤 순서를 깨지 않기 위해 즉시 삽입하지 않는다.
            this.updateCount++;
            this.updateHeaderCount();
            return;
        }

        const changed =
            existing.value !== row.value ||
            existing.cookieValue !== row.cookieValue ||
            existing.status !== row.status ||
            existing.updatedAt !== row.updatedAt;

        existing.collectionSetting = row.collectionSetting ?? existing.collectionSetting;
        existing.value = row.value;
        existing.cookieValue = row.cookieValue;
        existing.status = row.status;
        existing.sourceTimestamp = row.sourceTimestamp;
        existing.receivedAt = row.receivedAt;
        existing.updatedAt = row.updatedAt;
        existing.updateCount = row.updateCount;

        this.updateCount++;
        this.updateHeaderCount();

        // 현재 가상 스크롤 윈도우에 렌더링되어 있는 행만 DOM을 직접 패치한다.
        // 화면 밖에 있는 행은 데이터(rowMap)만 갱신하고 DOM 작업은 하지 않는다.
        const tr = this.rowElements.get(key);

        if (tr == null) {
            return;
        }

        this.patchRowElement(tr, existing);

        if (changed) {
            this.triggerFlash(key);
        }
    }

    isMatchedCurrentFilter(row: CurrentValueRow): boolean {
        const groupId = row.groupId ?? "";

        if (this.selectedGroupId.length > 0 && groupId !== this.selectedGroupId) {
            return false;
        }

        if (this.keyword.length === 0) {
            return true;
        }

        const q = this.keyword.toLowerCase();

        return String(row.collectionSetting ?? "").toLowerCase().includes(q) ||
            String(row.groupId ?? "").toLowerCase().includes(q) ||
            String(row.tagId ?? "").toLowerCase().includes(q) ||
            String(row.value ?? "").toLowerCase().includes(q) ||
            String(row.cookieValue ?? "").toLowerCase().includes(q);
    }

    triggerFlash(key: string): void {
        this.flashKeys.add(key);

        const tr = this.rowElements.get(key);

        if (tr != null) {
            tr.classList.add("value-flash");
        }

        window.setTimeout(() => {
            this.flashKeys.delete(key);

            const currentTr = this.rowElements.get(key);

            if (currentTr != null) {
                currentTr.classList.remove("value-flash");
            }
        }, 1600);
    }

    scheduleRenderWindow(): void {
        if (this.renderWindowScheduled) {
            return;
        }

        this.renderWindowScheduled = true;

        window.requestAnimationFrame(() => {
            this.renderWindowScheduled = false;
            this.renderWindow();
        });
    }

    /**
     * 가상 스크롤: 실제 화면에 보이는 구간(+버퍼)만 <tr> 로 렌더링하고,
     * 위/아래 구간은 높이만 차지하는 spacer <tr> 로 채운다.
     * force=true 면 보이는 구간(startIndex~endIndex)이 이전과 같아도
     * (예: 데이터 개수가 늘어난 경우) 무조건 다시 그린다.
     */
    renderWindow(force: boolean = false): void {
        const tbody = document.getElementById("currentValueBody") as HTMLTableSectionElement | null;
        const scrollEl = document.getElementById("currentValueScroll") as HTMLDivElement | null;

        if (tbody == null || scrollEl == null) {
            return;
        }

        if (this.rows.length === 0) {
            tbody.innerHTML = `
            <tr>
                <td colspan="10" class="wf-current-empty-cell">조회된 데이터가 없습니다.</td>
            </tr>
        `;
            this.rowElements.clear();
            this.windowStart = -1;
            this.windowEnd = -1;
            $("#lblVisibleRange").text("0 ~ 0");
            return;
        }

        const rowHeight = this.measuredRowHeight ?? this.fallbackRowHeight;
        const scrollTop = scrollEl.scrollTop;
        const viewportHeight = scrollEl.clientHeight;

        let startIndex = Math.floor(scrollTop / rowHeight) - this.bufferRows;
        let endIndex = Math.ceil((scrollTop + viewportHeight) / rowHeight) + this.bufferRows;

        startIndex = Math.max(0, startIndex);
        endIndex = Math.min(this.rows.length, endIndex);

        if (
            !force &&
            startIndex === this.windowStart &&
            endIndex === this.windowEnd &&
            tbody.childElementCount > 0
        ) {
            return;
        }

        this.windowStart = startIndex;
        this.windowEnd = endIndex;

        const topSpacerHeight = startIndex * rowHeight;
        const bottomSpacerHeight = (this.rows.length - endIndex) * rowHeight;

        const fragment = document.createDocumentFragment();
        fragment.appendChild(this.buildSpacerRow(topSpacerHeight));

        this.rowElements.clear();

        for (let i = startIndex; i < endIndex; i++) {
            const row = this.rows[i];
            const key = this.makeKey(row.groupId, row.tagId);
            const tr = this.buildRowElement(row, i, key);

            this.rowElements.set(key, tr);
            fragment.appendChild(tr);
        }

        fragment.appendChild(this.buildSpacerRow(bottomSpacerHeight));

        tbody.innerHTML = "";
        tbody.appendChild(fragment);

        if (this.measuredRowHeight == null) {
            const sample = tbody.querySelector("tr:not(.wf-virtual-spacer)") as HTMLTableRowElement | null;

            if (sample != null && sample.offsetHeight > 0) {
                this.measuredRowHeight = sample.offsetHeight;
                // 추정치가 아닌 실측 높이로 윈도우를 다시 계산한다.
                this.windowStart = -1;
                this.windowEnd = -1;
                this.renderWindow(true);
                return;
            }
        }

        $("#lblVisibleRange").text(`${startIndex + 1} ~ ${endIndex}`);
        this.refreshIcons();
    }

    buildSpacerRow(height: number): HTMLTableRowElement {
        const tr = document.createElement("tr");
        tr.className = "wf-virtual-spacer";
        tr.style.height = `${Math.max(0, height)}px`;

        const td = document.createElement("td");
        td.colSpan = 10;
        td.style.padding = "0";
        td.style.border = "none";

        tr.appendChild(td);

        return tr;
    }

    buildRowElement(row: CurrentValueRow, index: number, key: string): HTMLTableRowElement {
        const template = document.createElement("template");
        template.innerHTML = this.renderRow(row, index, key).trim();

        return template.content.firstElementChild as HTMLTableRowElement;
    }

    /**
     * 화면에 렌더링되어 있는 <tr> 의 값 관련 셀만 직접 갱신한다.
     * (innerHTML 재생성 없이 textContent / title / class 만 변경)
     */
    patchRowElement(tr: HTMLTableRowElement, row: CurrentValueRow): void {
        const valueEl = tr.querySelector('[data-cell="value"]') as HTMLElement | null;

        if (valueEl != null) {
            valueEl.textContent = row.value ?? "-";
            valueEl.title = row.value ?? "";
        }

        const cookieEl = tr.querySelector('[data-cell="cookie"]') as HTMLElement | null;

        if (cookieEl != null) {
            cookieEl.textContent = row.cookieValue ?? "-";
            cookieEl.title = row.cookieValue ?? "";
        }

        const statusEl = tr.querySelector('[data-cell="status"]') as HTMLElement | null;

        if (statusEl != null) {
            const isGood = this.isGoodStatus(row.status);

            statusEl.classList.toggle("is-good", isGood);
            statusEl.classList.toggle("is-bad", !isGood);

            const statusTextEl = statusEl.querySelector(".wf-status-text") as HTMLElement | null;

            if (statusTextEl != null) {
                statusTextEl.textContent = this.formatStatus(row.status);
            }
        }

        const updateCountEl = tr.querySelector('[data-cell="updateCount"]') as HTMLElement | null;

        if (updateCountEl != null) {
            updateCountEl.textContent = this.formatNumber(row.updateCount);
        }

        const sourceTimeEl = tr.querySelector('[data-cell="sourceTimestamp"]') as HTMLElement | null;

        if (sourceTimeEl != null) {
            sourceTimeEl.textContent = this.formatDate(row.sourceTimestamp);
        }

        const updatedAtEl = tr.querySelector('[data-cell="updatedAt"]') as HTMLElement | null;

        if (updatedAtEl != null) {
            updatedAtEl.textContent = this.formatDate(row.updatedAt);
        }
    }

    renderRow(row: CurrentValueRow, index: number, key: string): string {
        const groupId = row.groupId ?? "";
        const isGood = this.isGoodStatus(row.status);
        const statusText = this.formatStatus(row.status);
        const flashClass = this.flashKeys.has(key) ? "value-flash" : "";
        const collectionSetting = row.collectionSetting ?? "";

        return `
        <tr class="${flashClass}" data-key="${this.escapeHtml(key)}">
            <td>
                <span class="wf-current-row-index">${index + 1}</span>
            </td>
            <td>
                <span class="wf-current-setting-text"
                      title="${this.escapeHtml(collectionSetting)}">
                    ${this.escapeHtml(collectionSetting || "-")}
                </span>
            </td>
            <td>
                <span class="wf-current-group-text"
                      title="${this.escapeHtml(groupId)}">
                    ${this.escapeHtml(groupId || "-")}
                </span>
            </td>
            <td>
                <strong class="wf-current-tag"
                        title="${this.escapeHtml(row.tagId)}">
                    ${this.escapeHtml(row.tagId)}
                </strong>
            </td>
            <td>
                <span class="wf-current-value"
                      data-cell="value"
                      title="${this.escapeHtml(row.value ?? "")}">
                    ${this.escapeHtml(row.value ?? "-")}
                </span>
            </td>
            <td>
                <span class="wf-current-cookie-value"
                      data-cell="cookie"
                      title="${this.escapeHtml(row.cookieValue ?? "")}">
                    ${this.escapeHtml(row.cookieValue ?? "-")}
                </span>
            </td>
            <td>
                <span class="wf-current-status ${isGood ? "is-good" : "is-bad"}" data-cell="status">
                    <span class="wf-status-dot"></span>
                    <span class="wf-status-text">${this.escapeHtml(statusText)}</span>
                </span>
            </td>
            <td class="text-end" data-cell="updateCount">${this.formatNumber(row.updateCount)}</td>
            <td data-cell="sourceTimestamp">${this.formatDate(row.sourceTimestamp)}</td>
            <td data-cell="updatedAt">${this.formatDate(row.updatedAt)}</td>
        </tr>
    `;
    }

    updateHeaderCount(): void {
        $("#lblTotalCount").text(this.totalCount.toLocaleString());
        $("#lblLoadedCount").text(this.loadedCount.toLocaleString());
        $("#lblUpdateCount").text(this.updateCount.toLocaleString());
    }

    setStreamStatus(status: StreamStatus, text: string): void {
        $("#lblStreamStatus").text(text);
        $("#lblStreamChip")
            .removeClass("is-wait is-connected is-error")
            .addClass(`is-${status}`);
    }

    formatStatus(status?: string | number | null): string {
        if (status == null || status === "") {
            return "-";
        }

        if (typeof status === "number") {
            if (status === 0) return "Good";
            if (status === 1) return "Bad";
            return String(status);
        }

        const normalized = String(status).toLowerCase();

        if (normalized === "0" || normalized === "good") {
            return "Good";
        }

        if (normalized === "1" || normalized === "bad") {
            return "Bad";
        }

        return String(status);
    }

    isGoodStatus(status?: string | number | null): boolean {
        if (status == null || status === "") {
            return true;
        }

        if (typeof status === "number") {
            return status === 0;
        }

        const normalized = String(status).toLowerCase();

        return normalized.includes("good") ||
            normalized === "0" ||
            normalized === "0x00000000";
    }

    normalizeCurrentRow(row: any): CurrentValueRow {
        return {
            ...row,
            groupId: this.readValue(row, "groupId", "GROUP_ID", "group_id"),
            tagId: String(this.readValue(row, "tagId", "TAG_ID", "tag_id") ?? ""),
            collectionSetting: this.readValue(row, "collectionSetting", "DESCRIPTION", "description"),
            value: this.readValue(row, "value", "VALUE"),
            cookieValue: this.readValue(row, "cookieValue", "COOKIE_VALUE", "cookie_value"),
            status: this.readValue(row, "status", "STATUS"),
            updateCount: this.readNumber(row, "updateCount", "UPDATE_COUNT", "update_count"),
            sourceTimestamp: this.readValue(row, "sourceTimestamp", "SOURCETIMESTAMP", "source_timestamp"),
            receivedAt: this.readValue(row, "receivedAt", "RECEIVEDAT", "received_at"),
            updatedAt: this.readValue(row, "updatedAt", "UPDATEDAT", "updated_at")
        };
    }

    readValue(row: any, ...names: string[]): any {
        if (row == null) {
            return null;
        }

        for (const name of names) {
            if (Object.prototype.hasOwnProperty.call(row, name)) {
                return row[name];
            }
        }

        const targets = names.map(x => this.normalizeFieldName(x));

        for (const key of Object.keys(row)) {
            if (targets.includes(this.normalizeFieldName(key))) {
                return row[key];
            }
        }

        return null;
    }

    readNumber(row: any, ...names: string[]): number | null {
        const value = this.readValue(row, ...names);

        if (value == null || value === "") {
            return null;
        }

        const numberValue = Number(value);

        return Number.isFinite(numberValue)
            ? numberValue
            : null;
    }

    normalizeFieldName(value: string): string {
        return String(value ?? "")
            .replace(/_/g, "")
            .replace(/-/g, "")
            .toLowerCase();
    }

    makeKey(groupId: string | null | undefined, tagId: string): string {
        return `${groupId ?? ""}||${tagId}`;
    }

    formatDate(value?: string | null): string {
        if (value == null || value === "") {
            return "-";
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return this.escapeHtml(value);
        }

        const yyyy = date.getFullYear();
        const mm = String(date.getMonth() + 1).padStart(2, "0");
        const dd = String(date.getDate()).padStart(2, "0");
        const hh = String(date.getHours()).padStart(2, "0");
        const mi = String(date.getMinutes()).padStart(2, "0");
        const ss = String(date.getSeconds()).padStart(2, "0");

        return `${yyyy}-${mm}-${dd} ${hh}:${mi}:${ss}`;
    }

    formatNumber(value?: number | string | null): string {
        if (value == null || value === "") {
            return "-";
        }

        const numberValue = Number(value);

        if (Number.isNaN(numberValue)) {
            return String(value);
        }

        return numberValue.toLocaleString();
    }

    refreshIcons(): void {
        window.setTimeout(() => {
            const lucide = (window as any).lucide;

            if (lucide?.createIcons != null) {
                lucide.createIcons();
            }
        }, 0);
    }

    escapeSelectorValue(value: string): string {
        return value.replace(/\\/g, "\\\\").replace(/"/g, '\\"');
    }

    escapeHtml(value: string | number | null | undefined): string {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
}