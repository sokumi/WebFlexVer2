type CurrentValueRow = {
    groupId?: string | null;
    tagId: string;
    value?: string | null;
    status?: string | number | null;
    sourceTimestamp?: string | null;
    receivedAt?: string | null;
    updatedAt?: string | null;
};

export default class Page {
    private rows: CurrentValueRow[] = [];
    private rowMap = new Map<string, CurrentValueRow>();

    private eventSource: EventSource | null = null;
    private updateCount = 0;

    private readonly rowHeight = 34;
    private readonly buffer = 12;

    private renderRequested = false;
    private flashKeys = new Set<string>();

    private renderTimer: number | null = null;

    init(): void {
        $("#currentValueScroll").on("scroll", () => {
            this.requestRender();
        });

        this.loadInitialData();
        this.connectStream();
    }

    private async loadInitialData(): Promise<void> {
        try {
            $("#lblStreamStatus").text("초기 조회 중");

            const response = await fetch("/api/currentvalue/list");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json() as CurrentValueRow[];

            this.rows = data;
            this.rowMap.clear();

            for (const row of this.rows) {
                this.rowMap.set(this.makeKey(row.groupId, row.tagId), row);
            }

            $("#lblTotalCount").text(String(this.rows.length));
            $("#lblStreamStatus").text("초기 조회 완료");

            this.requestRender();
        } catch (e) {
            console.error(e);
            $("#lblStreamStatus").text("초기 조회 실패");
            $("#currentValueBody").html(`<tr><td colspan="6">초기 조회 실패</td></tr>`);
        }
    }

    private connectStream(): void {
        if (this.eventSource != null) {
            this.eventSource.close();
            this.eventSource = null;
        }

        $("#lblStreamStatus").text("연결 중");

        // SSE로 받아서 currentvalue 이벤트의 JSON을 파싱
        const source = new EventSource("/api/currentvalue/stream");
        this.eventSource = source;

        source.addEventListener("connected", () => {
            $("#lblStreamStatus").text("연결됨");
        });

        source.addEventListener("currentvalue", (event: MessageEvent) => {

            try {
                const row = JSON.parse(event.data) as CurrentValueRow;
                this.applyUpdate(row);
            } catch (e) {
                console.error("currentvalue parse error", e, event.data);
            }
        });

        source.onerror = (e) => {
            console.error("currentvalue stream error", e);
            $("#lblStreamStatus").text("재연결 중");
        };

        window.addEventListener("beforeunload", () => {
            source.close();
        });
    }

    private applyUpdate(row: CurrentValueRow): void {
        const key = this.makeKey(row.groupId, row.tagId);
        const existing = this.rowMap.get(key);

        let changed = false;

        if (existing == null) {
            this.rowMap.set(key, row);
            this.rows.push(row);
            //this.rows.sort((a, b) => {
            //    const endpointCompare = a.endpointUrl.localeCompare(b.endpointUrl);
            //    if (endpointCompare !== 0) {
            //        return endpointCompare;
            //    }

            //    return a.nodeId.localeCompare(b.nodeId);
            //});

            $("#lblTotalCount").text(String(this.rows.length));
            changed = true;
        } else {
            changed =
                existing.value !== row.value ||
                existing.status !== row.status;

            existing.value = row.value;
            existing.status = row.status;
            existing.sourceTimestamp = row.sourceTimestamp;
            existing.receivedAt = row.receivedAt;
            existing.updatedAt = row.updatedAt;
        }

        // notify 모아두고 1초마다 updqte
        if (changed) {
            this.flashKeys.add(key);

            window.setTimeout(() => {
                this.flashKeys.delete(key);
                this.requestRender();
            }, 2000);
        }

        this.updateCount++;
        $("#lblUpdateCount").text(String(this.updateCount));

        this.requestRender();
    }


    private requestRender(): void {
        if (this.renderTimer != null) {
            return;
        }

        this.renderTimer = window.setTimeout(() => {
            this.renderTimer = null;

            if (this.renderRequested) {
                return;
            }

            this.renderRequested = true;

            window.requestAnimationFrame(() => {
                this.renderRequested = false;
                this.renderVisibleRows();
            });
        }, 100);
    }

    private renderVisibleRows(): void {
        const scrollEl = document.getElementById("currentValueScroll") as HTMLDivElement | null;
        const tbody = document.getElementById("currentValueBody") as HTMLTableSectionElement | null;

        if (scrollEl == null || tbody == null) {
            return;
        }

        if (this.rows.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6">조회된 데이터가 없습니다.</td></tr>`;
            $("#lblVisibleCount").text("0");
            return;
        }

        const scrollTop = scrollEl.scrollTop;
        const viewportHeight = scrollEl.clientHeight;

        const startIndex = Math.max(0, Math.floor(scrollTop / this.rowHeight) - this.buffer);
        const visibleCount = Math.ceil(viewportHeight / this.rowHeight) + this.buffer * 2;
        const endIndex = Math.min(this.rows.length, startIndex + visibleCount);

        const topPadding = startIndex * this.rowHeight;
        const bottomPadding = Math.max(0, (this.rows.length - endIndex) * this.rowHeight);

        const visibleRows = this.rows.slice(startIndex, endIndex);

        const html: string[] = [];

        if (topPadding > 0) {
            html.push(`<tr style="height:${topPadding}px"><td colspan="6"></td></tr>`);
        }

        for (const row of visibleRows) {
            html.push(this.renderRow(row));
        }

        if (bottomPadding > 0) {
            html.push(`<tr style="height:${bottomPadding}px"><td colspan="6"></td></tr>`);
        }

        tbody.innerHTML = html.join("");

        $("#lblVisibleCount").text(`${startIndex + 1} ~ ${endIndex}`);
    }

    private renderRow(row: CurrentValueRow): string {
        const groupId = row.groupId ?? "";
        const statusText = this.formatStatus(row.status);

        const key = this.makeKey(groupId, row.tagId);
        const statusClass = this.isGoodStatus(row.status) ? "status-good" : "status-bad";
        const flashClass = this.flashKeys.has(key) ? "value-flash" : "";

        return `
    <tr style="height:${this.rowHeight}px">
        <td title="${this.escapeHtml(groupId)}">${this.escapeHtml(groupId)}</td>
        <td title="${this.escapeHtml(row.tagId)}">${this.escapeHtml(row.tagId)}</td>
        <td class="value-cell ${flashClass}" title="${this.escapeHtml(row.value ?? "")}">${this.escapeHtml(row.value ?? "")}</td>
        <td class="${statusClass}">${this.escapeHtml(statusText)}</td>
        <td>${this.formatDate(row.sourceTimestamp)}</td>
        <td>${this.formatDate(row.updatedAt)}</td>
    </tr>
`;
    }

    private formatStatus(status?: string | number | null): string {
        if (status == null || status === "") {
            return "";
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

    private isGoodStatus(status?: string | number | null): boolean {
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

    private makeKey(groupId: string | null | undefined, tagId: string): string {
        return `${groupId ?? ""}||${tagId}`;
    }

    private formatDate(value?: string | null): string {
        if (value == null || value === "") {
            return "";
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

    private escapeHtml(value: string): string {
        return value
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
}