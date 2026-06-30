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

     eventSource: EventSource | null = null;

     totalCount = 0;
     loadedCount = 0;
     updateCount = 0;

     selectedGroupId = "";
     keyword = "";

     readonly pageSize = 120;
     isLoading = false;
     hasMore = true;

    renderTimer: number | null = null;
    searchTimer: number | null = null;
    flashKeys = new Set<string>();

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
        this.totalCount = 0;
        this.loadedCount = 0;
        this.hasMore = true;
        this.flashKeys.clear();

        $("#currentValueBody").html(`
            <tr>
                <td colspan="10" class="wf-current-empty-cell">데이터를 불러오는 중입니다.</td>
            </tr>
        `);

        await this.loadNextPage();
    }

     async loadGroups(): Promise<void> {
        try {
            const response = await fetch("/api/currentvalue/groups");

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

            const response = await fetch(`/api/currentvalue/page?${query.toString()}`);

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
            this.renderRows();
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
    }

     connectStream(): void {
        this.eventSource?.close();
        this.setStreamStatus("wait", "연결 중");

        const source = new EventSource("/api/currentvalue/stream");
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

        let changed = false;

        if (existing == null) {
            // 아직 사용자가 해당 위치까지 스크롤해서 불러오지 않은 데이터는
            // 무한 스크롤 순서를 깨지 않기 위해 즉시 삽입하지 않는다.
            this.updateCount++;
            this.updateHeaderCount();
            return;
        }

         changed =
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

        if (changed) {
            this.flashKeys.add(key);

            window.setTimeout(() => {
                this.flashKeys.delete(key);
                this.requestRender();
            }, 1600);
        }

        this.updateCount++;
        this.updateHeaderCount();
        this.requestRender();
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

    requestRender(): void {
        if (this.renderTimer != null) {
            return;
        }

        this.renderTimer = window.setTimeout(() => {
            this.renderTimer = null;
            this.renderRows();
        }, 80);
    }

    renderRows(): void {
        const tbody = document.getElementById("currentValueBody") as HTMLTableSectionElement | null;

        if (tbody == null) {
            return;
        }

        if (this.rows.length === 0) {
            tbody.innerHTML = `
            <tr>
                <td colspan="10" class="wf-current-empty-cell">조회된 데이터가 없습니다.</td>
            </tr>
        `;
            $("#lblVisibleRange").text("0 ~ 0");
            return;
        }

        tbody.innerHTML = this.rows
            .map((row, index) => this.renderRow(row, index))
            .join("");

        $("#lblVisibleRange").text(`1 ~ ${this.rows.length.toLocaleString()}`);
        this.refreshIcons();
    }

    renderRow(row: CurrentValueRow, index: number): string {
        const groupId = row.groupId ?? "";
        const key = this.makeKey(groupId, row.tagId);
        const isGood = this.isGoodStatus(row.status);
        const statusText = this.formatStatus(row.status);
        const flashClass = this.flashKeys.has(key) ? "value-flash" : "";
        const collectionSetting = row.collectionSetting ?? "";

        return `
        <tr class="${flashClass}">
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
                      title="${this.escapeHtml(row.value ?? "")}">
                    ${this.escapeHtml(row.value ?? "-")}
                </span>
            </td>
            <td>
                <span class="wf-current-cookie-value"
                      title="${this.escapeHtml(row.cookieValue ?? "")}">
                    ${this.escapeHtml(row.cookieValue ?? "-")}
                </span>
            </td>
            <td>
                <span class="wf-current-status ${isGood ? "is-good" : "is-bad"}">
                    <span class="wf-status-dot"></span>
                    ${this.escapeHtml(statusText)}
                </span>
            </td>
            <td class="text-end">${this.formatNumber(row.updateCount)}</td>
            <td>${this.formatDate(row.sourceTimestamp)}</td>
            <td>${this.formatDate(row.updatedAt)}</td>
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