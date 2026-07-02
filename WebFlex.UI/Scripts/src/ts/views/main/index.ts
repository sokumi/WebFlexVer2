import { TabulatorFull as Tabulator } from "tabulator-tables";

export default class Page {
    rows: any[] = [];
    rowMap = new Map<string, any>();
    groupRows: any[] = [];

    grid: any = null;
    eventSource: any = null;
    gridReady = false;
    pendingReload = false;

    totalCount = 0;
    loadedCount = 0;
    updateCount = 0;

    selectedGroupId = "";
    keyword = "";

    readonly pageSize = 120;
    isLoading = false;
    hasMore = true;
    searchTimer: any = null;
    reloadSeq = 0;

    init() {
        this.initGrid();
        this.bindEvents();

        void this.loadGroups();
        this.connectStream();

        window.addEventListener("beforeunload", () => {
            this.eventSource?.close();
            this.grid?.destroy?.();
        });
    }

    initGrid() {
        const host = document.getElementById("currentValueGrid");

        if (host == null) {
            console.error("currentValueGrid element not found.");
            return;
        }

        this.grid = new Tabulator(host, {
            index: "rowKey",
            height: "100%",
            layout: "fitColumns",
            reactiveData: false,
            movableColumns: true,
            placeholder: "조회된 데이터가 없습니다.",
            columns: [
                {
                    title: "번호",
                    field: "rowNumber",
                    width: 64,
                    hozAlign: "center",
                    headerSort: false,
                    formatter: (cell: any) => {
                        return `<span class="wf-current-row-index">${this.escapeHtml(cell.getValue())}</span>`;
                    }
                },
                {
                    title: "수집 항목 설정",
                    field: "collectionSetting",
                    minWidth: 190,
                    formatter: (cell: any) => {
                        const value = cell.getValue() ?? "-";
                        return `<span class="wf-current-setting-text" title="${this.escapeHtml(value)}">${this.escapeHtml(value)}</span>`;
                    }
                },
                {
                    title: "그룹",
                    field: "groupName",
                    width: 130,
                    formatter: (cell: any) => {
                        const row = cell.getData();
                        const value = row.groupName ?? row.groupId ?? "-";
                        return `<span class="wf-current-group-text" title="${this.escapeHtml(row.groupId ?? "")}">${this.escapeHtml(value)}</span>`;
                    }
                },
                {
                    title: "태그 ID",
                    field: "tagId",
                    minWidth: 160,
                    formatter: (cell: any) => {
                        const value = cell.getValue() ?? "";
                        return `<strong class="wf-current-tag" title="${this.escapeHtml(value)}">${this.escapeHtml(value)}</strong>`;
                    }
                },
                {
                    title: "현재값",
                    field: "value",
                    minWidth: 160,
                    formatter: (cell: any) => {
                        const value = cell.getValue() ?? "-";
                        return `<span class="wf-current-value" title="${this.escapeHtml(value)}">${this.escapeHtml(value)}</span>`;
                    }
                },
                {
                    title: "변환값",
                    field: "cookieValue",
                    minWidth: 190,
                    formatter: (cell: any) => {
                        const value = cell.getValue() ?? "-";
                        return `<span class="wf-current-cookie-value" title="${this.escapeHtml(value)}">${this.escapeHtml(value)}</span>`;
                    }
                },
                {
                    title: "상태",
                    field: "status",
                    width: 110,
                    formatter: (cell: any) => {
                        const value = cell.getValue();
                        const isGood = this.isGoodStatus(value);
                        return `
                            <span class="wf-current-status ${isGood ? "is-good" : "is-bad"}">
                                <span class="wf-status-dot"></span>
                                <span class="wf-status-text">${this.escapeHtml(this.formatStatus(value))}</span>
                            </span>
                        `;
                    }
                },
                {
                    title: "업데이트",
                    field: "updateCount",
                    width: 120,
                    hozAlign: "right",
                    formatter: (cell: any) => this.escapeHtml(this.formatNumber(cell.getValue()))
                },
                {
                    title: "Source Time",
                    field: "sourceTimestamp",
                    minWidth: 165,
                    formatter: (cell: any) => this.escapeHtml(this.formatDate(cell.getValue()))
                },
                {
                    title: "Updated Time",
                    field: "updatedAt",
                    minWidth: 165,
                    formatter: (cell: any) => this.escapeHtml(this.formatDate(cell.getValue()))
                }
            ],
            rowFormatter: (row: any) => {
                const data = row.getData();
                const element = row.getElement();
                element.setAttribute("data-key", data.rowKey ?? "");
            },
            renderComplete: () => {
                this.bindGridScroll();
                this.updateVisibleRange();
            }
        });

        this.grid?.on?.("tableBuilt", () => {
            this.onGridReady();
        });

        this.grid?.on?.("scrollVertical", () => {
            this.handleGridScroll();
        });

        window.setTimeout(() => {
            this.onGridReady();
        }, 0);
    }

    onGridReady() {
        if (this.gridReady) {
            return;
        }

        const holder = document.querySelector("#currentValueGrid .tabulator-tableholder") as any;

        if (holder == null) {
            window.setTimeout(() => {
                this.onGridReady();
            }, 30);
            return;
        }

        this.gridReady = true;
        this.bindGridScroll();

        if (this.pendingReload) {
            this.pendingReload = false;
        }

        void this.reload();
    }

    bindEvents() {
        $("#btnToggleGroupPanel").on("click", () => {
            this.toggleGroupPanel();
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
            this.grid?.redraw?.(true);
            this.updateVisibleRange();
        });
    }

    bindGridScroll() {
        const holder = document.querySelector("#currentValueGrid .tabulator-tableholder") as any;

        if (holder == null || holder.dataset.webflexScrollBound === "true") {
            return;
        }

        holder.dataset.webflexScrollBound = "true";

        holder.addEventListener("scroll", () => {
            this.handleGridScroll();
        });
    }

    handleGridScroll() {
        const holder = document.querySelector("#currentValueGrid .tabulator-tableholder") as any;

        if (holder == null) {
            return;
        }

        const remain = holder.scrollHeight - holder.scrollTop - holder.clientHeight;

        // loadAllPages() 가 화면 진입 시 전체 데이터를 이미 다 불러오므로
        // 이 시점엔 보통 hasMore 가 false 라 loadNextPage() 는 즉시 반환됩니다.
        // 배경 로딩이 아직 끝나지 않은 극히 짧은 순간을 위한 안전망으로만 남겨둡니다.
        if (remain < 320) {
            void this.loadNextPage(this.reloadSeq);
        }

        this.updateVisibleRange();
    }

    toggleGroupPanel() {
        const $panel = $("#currentGroupPanel");
        const collapsed = !$panel.hasClass("is-collapsed");

        $panel.toggleClass("is-collapsed", collapsed);
        $("#btnToggleGroupPanel").attr("aria-expanded", String(!collapsed));
        $("#lblGroupToggleText").text(collapsed ? "펼치기" : "접기");

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
        window.setTimeout(() => {
            this.grid?.redraw?.(true);
            this.updateVisibleRange();
        }, 210);
    }

    async reload() {
        if (!this.gridReady || this.grid == null) {
            this.pendingReload = true;
            return;
        }

        const seq = ++this.reloadSeq;

        this.isLoading = false;
        this.rows = [];
        this.rowMap.clear();
        this.totalCount = 0;
        this.loadedCount = 0;
        this.hasMore = true;

        this.updateHeaderCount();
        $("#lblVisibleRange").text("0 ~ 0");

        if (this.grid.getDataCount?.() > 0) {
            await this.grid.replaceData([]);
        }

        await this.loadAllPages(seq);
    }

    /**
     * 전체 데이터를 스크롤과 무관하게 백그라운드에서 순차적으로 모두 불러옵니다.
     *
     * Tabulator 의 virtual DOM (가상 스크롤) 은 자체적으로 화면에 보이는 구간만
     * 렌더링(windowing)해주므로, 예전 순수 테이블에서 직접 구현했던
     * "가상 스크롤"과 동일한 역할을 이미 대신 해줍니다. 따라서 서버 페이징을
     * 스크롤 위치에 맞춰 추가로 붙일 필요가 없고, 오히려 스크롤 도중 addData 를
     * 반복 호출하면 virtual DOM 의 내부 렌더 범위 계산이 어긋나는 원인이 됩니다.
     * 태그 수가 (~2000개 수준으로) 크지 않으므로 화면 진입 시 전량을 미리
     * 불러와 버리는 편이 가장 단순하고 안전합니다.
     *
     * setTimeout(0) 으로 매 페이지 사이에 브라우저에 제어권을 양보하여
     * 메인 스레드를 길게 블로킹하지 않도록 합니다.
     */
    async loadAllPages(seq: any) {
        while (this.hasMore && seq === this.reloadSeq) {
            await this.loadNextPage(seq);
            await new Promise(resolve => window.setTimeout(resolve, 0));
        }
    }

    async loadGroups() {
        try {
            const response = await fetch("/main/list/groups");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const result: any = await response.json();

            if (!result.success) {
                return;
            }

            this.groupRows = result.data ?? [];
            this.renderGroups(this.groupRows);
        } catch (e) {
            console.error(e);
        }
    }

    renderGroups(groups: any[]) {
        const total = groups.reduce((sum: any, item: any) => sum + Number(item.count ?? 0), 0);
        const html: any[] = [];

        html.push(`
            <button type="button"
                    class="wf-current-group-chip ${this.selectedGroupId === "" ? "is-active" : ""}"
                    data-group-id=""
                    data-group-name="전체 그룹">
                전체
                <span>${total.toLocaleString()}</span>
            </button>
        `);

        for (const group of groups) {
            const groupId = group.groupId ?? "";
            const groupName = group.groupName ?? groupId;
            const displayName = groupName || "미지정";

            html.push(`
                <button type="button"
                        class="wf-current-group-chip ${this.selectedGroupId === groupId ? "is-active" : ""}"
                        data-group-id="${this.escapeHtml(groupId)}"
                        data-group-name="${this.escapeHtml(displayName)}"
                        title="${this.escapeHtml(groupId)}">
                    ${this.escapeHtml(displayName)}
                    <span>${Number(group.count ?? 0).toLocaleString()}</span>
                    ${Number(group.badCount ?? 0) > 0 ? `<em>${Number(group.badCount ?? 0).toLocaleString()}</em>` : ""}
                </button>
            `);
        }

        $("#groupFilterHost").html(html.join(""));

        const selected = groups.find((x: any) => (x.groupId ?? "") === this.selectedGroupId);
        $("#lblGroupSummary").text(
            this.selectedGroupId.length === 0
                ? "전체 그룹"
                : selected?.groupName ?? this.selectedGroupId
        );

        this.refreshIcons();
    }

    selectGroup(groupId: any) {
        if (this.selectedGroupId === groupId) {
            return;
        }

        this.selectedGroupId = groupId;

        $("#groupFilterHost [data-group-id]").removeClass("is-active");
        $(`#groupFilterHost [data-group-id="${this.escapeSelectorValue(groupId)}"]`).addClass("is-active");

        const groupName = String(
            $(`#groupFilterHost [data-group-id="${this.escapeSelectorValue(groupId)}"]`).attr("data-group-name") ?? ""
        );

        $("#lblGroupSummary").text(groupId.length === 0 ? "전체 그룹" : groupName || groupId);

        void this.reload();
    }

    requestSearch() {
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
        }, 150);
    }

    applySearchImmediately() {
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

    async loadNextPage(seq: any = this.reloadSeq) {
        if (this.isLoading || !this.hasMore || seq !== this.reloadSeq) {
            return;
        }

        this.isLoading = true;
        $("#currentValueLoading").removeClass("d-none");

        const groupId = this.selectedGroupId;
        const keyword = this.keyword;
        const skip = this.loadedCount;

        try {
            const query = new URLSearchParams();
            query.set("skip", String(skip));
            query.set("take", String(this.pageSize));

            if (groupId.length > 0) {
                query.set("groupId", groupId);
            }

            if (keyword.length > 0) {
                query.set("keyword", keyword);
            }

            const response = await fetch(`/main/list/page?${query.toString()}`);

            if (seq !== this.reloadSeq ||
                groupId !== this.selectedGroupId ||
                keyword !== this.keyword) {
                return;
            }

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const result: any = await response.json();

            if (seq !== this.reloadSeq ||
                groupId !== this.selectedGroupId ||
                keyword !== this.keyword) {
                return;
            }

            if (!result.success) {
                throw new Error("CurrentValue 조회 실패");
            }

            const page = result.data ?? {};
            const pageRows = (page.rows ?? []).map((x: any) => this.normalizeCurrentRow(x));
            const newRows: any[] = [];

            this.totalCount = Number(page.totalCount ?? 0);

            for (const row of pageRows) {
                const key = this.makeKey(row.groupId, row.tagId);

                if (!this.rowMap.has(key)) {
                    row.rowKey = key;
                    row.rowNumber = this.rows.length + 1;
                    this.rows.push(row);
                    newRows.push(row);
                }

                this.rowMap.set(key, row);
            }

            this.loadedCount = this.rows.length;
            this.hasMore = this.loadedCount < this.totalCount;

            if (this.grid != null) {
                if (newRows.length > 0) {
                    await this.addDataPreserveScroll(newRows);
                } else if (this.rows.length === 0) {
                    await this.grid.replaceData([]);
                }
            }

            this.updateHeaderCount();
            this.bindGridScroll();
            this.updateVisibleRange();
        } catch (e) {
            console.error(e);

            if (this.rows.length === 0) {
                await this.grid?.replaceData?.([]);
            }
        } finally {
            if (seq === this.reloadSeq) {
                this.isLoading = false;
                $("#currentValueLoading").addClass("d-none");

                window.setTimeout(() => {
                    this.ensureScrollableOrLoadMore();
                }, 0);
            }
        }
    }

    /**
     * addData 후 스크롤 위치를 복원합니다.
     * Tabulator 의 addData 는 내부적으로 스크롤을 초기화할 수 있습니다.
     *
     * virtual 모드에서는 addData 직후 scrollHeight / 렌더 범위(vDom top/bottom)가
     * 즉시 정확히 재계산되지 않는 경우가 있어, 이후 위로 스크롤했을 때 이미
     * 지나간 행이 빈 화면으로 보이는 원인이 됩니다. redraw(true) 로 강제
     * 재계산시켜 방지합니다. 페이지 로딩(loadAllPages) 시에만 호출되므로
     * SSE 실시간 셀 패치(patchRowCells) 성능에는 영향이 없습니다.
     */
    private async addDataPreserveScroll(rows: any[]): Promise<void> {
        const holder = document.querySelector("#currentValueGrid .tabulator-tableholder") as HTMLElement | null;
        const scrollTop = holder?.scrollTop ?? 0;

        await this.grid.addData(rows);
        this.grid.redraw(true);

        if (holder != null && scrollTop > 0) {
            requestAnimationFrame(() => {
                holder.scrollTop = scrollTop;
            });
        }
    }

    ensureScrollableOrLoadMore() {
        if (this.isLoading || !this.hasMore) {
            return;
        }

        const holder = document.querySelector("#currentValueGrid .tabulator-tableholder") as any;

        if (holder == null) {
            return;
        }

        if (holder.scrollHeight <= holder.clientHeight + 20) {
            void this.loadNextPage(this.reloadSeq);
        }
    }

    connectStream() {
        this.eventSource?.close();
        this.setStreamStatus("wait", "연결 중");

        const source = new EventSource("/main/list/stream");
        this.eventSource = source;

        source.addEventListener("connected", () => {
            this.setStreamStatus("connected", "연결됨");
        });

        source.addEventListener("currentvalue", (event: any) => {
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

    applyUpdate(row: any) {
        if (!this.isMatchedCurrentFilter(row)) {
            return;
        }

        const key = this.makeKey(row.groupId, row.tagId);
        const existing = this.rowMap.get(key);

        if (existing == null) {
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
        existing.groupName = this.getGroupDisplayName(existing.groupId);
        existing.value = row.value;
        existing.cookieValue = row.cookieValue;
        existing.status = row.status;
        existing.sourceTimestamp = row.sourceTimestamp;
        existing.receivedAt = row.receivedAt;
        existing.updatedAt = row.updatedAt;
        existing.updateCount = row.updateCount;

        this.updateCount++;
        this.updateHeaderCount();

        if (this.grid == null) {
            return;
        }

        // 화면(가상 DOM 렌더링 창)에 실제로 보이는 행일 때만 DOM 을 패치합니다.
        // grid.getRow()/getCell() 은 호출 시 화면 밖 행이라도 엘리먼트를 강제로
        // 생성(materialize)하기 때문에, 이 체크 없이 patchRowCells 를 그대로
        // 호출하면 보이지도 않는 수천 개 행의 DOM 을 초당 수십~수백 번
        // 만들었다 버리는 낭비가 쌓여 결국 렉으로 이어집니다.
        // data-key 는 rowFormatter 에서 실제로 렌더링된 행에만 찍히므로
        // 이 selector 로 화면에 보이는 행인지 저비용으로 먼저 확인합니다.
        // existing 객체는 이미 최신값으로 갱신되었으므로, 화면 밖 행은 나중에
        // 스크롤되어 다시 보일 때 포매터가 알아서 최신값으로 그려줍니다.
        const isRendered = document.querySelector(
            `#currentValueGrid .tabulator-tableholder [data-key="${this.escapeSelectorValue(key)}"]`
        ) != null;

        if (!isRendered) {
            return;
        }

        // Tabulator 의 updateData() / 포매터 재실행 없이 셀 DOM 만 직접 교체합니다.
        // updateData() 는 Tabulator 내부 렌더링 파이프라인을 타므로
        // 2000 개 행이 로드된 상태에서 초당 수십 번 호출되면 CPU 부하가 급격히 올라갑니다.
        const isGood = this.isGoodStatus(existing.status);

        const patched = this.patchRowCells(key, {
            value: `<span class="wf-current-value" title="${this.escapeHtml(existing.value ?? "")}">${this.escapeHtml(existing.value ?? "-")}</span>`,
            cookieValue: `<span class="wf-current-cookie-value" title="${this.escapeHtml(existing.cookieValue ?? "")}">${this.escapeHtml(existing.cookieValue ?? "-")}</span>`,
            status: `<span class="wf-current-status ${isGood ? "is-good" : "is-bad"}"><span class="wf-status-dot"></span><span class="wf-status-text">${this.escapeHtml(this.formatStatus(existing.status))}</span></span>`,
            updateCount: this.escapeHtml(this.formatNumber(existing.updateCount)),
            sourceTimestamp: this.escapeHtml(this.formatDate(existing.sourceTimestamp)),
            updatedAt: this.escapeHtml(this.formatDate(existing.updatedAt))
        });

        if (patched && changed) {
            this.triggerFlash(key);
        }
    }

    /**
     * Tabulator 포매터 파이프라인을 거치지 않고 특정 행의 셀 innerHTML 을 직접 교체합니다.
     * @returns 행이 존재하면 true, 없으면 false
     */
    private patchRowCells(key: any, patches: Record<string, string>): boolean {
        const row = this.grid?.getRow?.(key);

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

    isMatchedCurrentFilter(row: any) {
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
            String(row.groupName ?? "").toLowerCase().includes(q) ||
            String(row.tagId ?? "").toLowerCase().includes(q) ||
            String(row.value ?? "").toLowerCase().includes(q) ||
            String(row.cookieValue ?? "").toLowerCase().includes(q);
    }

    triggerFlash(key: any) {
        const row = this.grid?.getRow?.(key);

        if (row == null || row === false) {
            return;
        }

        const element = row.getElement() as HTMLElement;

        element.classList.remove("value-flash");
        void element.offsetWidth; // reflow 강제하여 애니메이션 재시작
        element.classList.add("value-flash");

        window.setTimeout(() => {
            element.classList.remove("value-flash");
        }, 1600);
    }

    updateVisibleRange() {
        if (this.rows.length === 0) {
            $("#lblVisibleRange").text("0 ~ 0");
            return;
        }

        const holder = document.querySelector("#currentValueGrid .tabulator-tableholder") as any;
        const rowHeight = 42;

        if (holder == null) {
            $("#lblVisibleRange").text(`1 ~ ${this.loadedCount}`);
            return;
        }

        const start = Math.max(1, Math.floor(holder.scrollTop / rowHeight) + 1);
        const end = Math.min(this.loadedCount, Math.ceil((holder.scrollTop + holder.clientHeight) / rowHeight));

        $("#lblVisibleRange").text(`${start} ~ ${end}`);
    }

    updateHeaderCount() {
        $("#lblTotalCount").text(this.totalCount.toLocaleString());
        $("#lblLoadedCount").text(this.loadedCount.toLocaleString());
        $("#lblUpdateCount").text(this.updateCount.toLocaleString());
    }

    setStreamStatus(status: any, text: any) {
        $("#lblStreamStatus").text(text);
        $("#lblStreamChip")
            .removeClass("is-wait is-connected is-error")
            .addClass(`is-${status}`);
    }

    formatStatus(status: any) {
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

    isGoodStatus(status: any) {
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

    normalizeCurrentRow(row: any) {
        const groupId = this.readValue(row, "groupId", "GROUP_ID", "group_id");
        const tagId = String(this.readValue(row, "tagId", "TAG_ID", "tag_id") ?? "");
        const key = this.makeKey(groupId, tagId);

        return {
            ...row,
            rowKey: key,
            groupId,
            groupName: this.readValue(row, "groupName", "GROUP_NAME", "group_name") ?? this.getGroupDisplayName(groupId),
            tagId,
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

    getGroupDisplayName(groupId: any) {
        const id = String(groupId ?? "");

        if (id.length === 0) {
            return "";
        }

        const group = this.groupRows.find((x: any) => String(x.groupId ?? "") === id);

        return String(group?.groupName ?? id);
    }

    readValue(row: any, ...names: any[]) {
        if (row == null) {
            return null;
        }

        for (const name of names) {
            if (Object.prototype.hasOwnProperty.call(row, name)) {
                return row[name];
            }
        }

        const targets = names.map((x: any) => this.normalizeFieldName(x));

        for (const key of Object.keys(row)) {
            if (targets.includes(this.normalizeFieldName(key))) {
                return row[key];
            }
        }

        return null;
    }

    readNumber(row: any, ...names: any[]) {
        const value = this.readValue(row, ...names);

        if (value == null || value === "") {
            return null;
        }

        const numberValue = Number(value);

        return Number.isFinite(numberValue)
            ? numberValue
            : null;
    }

    normalizeFieldName(value: any) {
        return String(value ?? "")
            .replace(/_/g, "")
            .replace(/-/g, "")
            .toLowerCase();
    }

    makeKey(groupId: any, tagId: any) {
        return `${groupId ?? ""}||${tagId ?? ""}`;
    }

    formatDate(value: any) {
        if (value == null || value === "") {
            return "-";
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return String(value);
        }

        const yyyy = date.getFullYear();
        const mm = String(date.getMonth() + 1).padStart(2, "0");
        const dd = String(date.getDate()).padStart(2, "0");
        const hh = String(date.getHours()).padStart(2, "0");
        const mi = String(date.getMinutes()).padStart(2, "0");
        const ss = String(date.getSeconds()).padStart(2, "0");

        return `${yyyy}-${mm}-${dd} ${hh}:${mi}:${ss}`;
    }

    formatNumber(value: any) {
        if (value == null || value === "") {
            return "-";
        }

        const numberValue = Number(value);

        if (Number.isNaN(numberValue)) {
            return String(value);
        }

        return numberValue.toLocaleString();
    }

    refreshIcons() {
        window.setTimeout(() => {
            const lucide = (window as any).lucide;

            if (lucide?.createIcons != null) {
                lucide.createIcons();
            }
        }, 0);
    }

    escapeSelectorValue(value: any) {
        return String(value ?? "").replace(/\\/g, "\\\\").replace(/"/g, '\\"');
    }

    escapeHtml(value: any) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
}