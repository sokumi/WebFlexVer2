/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./Scripts/src/ts/framework/page.ts"
/*!******************************************!*\
  !*** ./Scripts/src/ts/framework/page.ts ***!
  \******************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   runPage: () => (/* binding */ runPage)
/* harmony export */ });
function runPage(Page) {
    document.addEventListener("DOMContentLoaded", () => {
        const page = new Page();
        window.viewModel = page;
        page.init();
    });
}


/***/ },

/***/ "./Scripts/src/ts/views/main/index.ts"
/*!********************************************!*\
  !*** ./Scripts/src/ts/views/main/index.ts ***!
  \********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.rows = [];
        this.rowMap = new Map();
        // 가상 스크롤로 실제 렌더링된 <tr> 만 추적 (key -> element)
        this.rowElements = new Map();
        this.eventSource = null;
        this.totalCount = 0;
        this.loadedCount = 0;
        this.updateCount = 0;
        this.selectedGroupId = "";
        this.keyword = "";
        this.pageSize = 120;
        this.isLoading = false;
        this.hasMore = true;
        this.searchTimer = null;
        this.flashKeys = new Set();
        // 가상 스크롤 관련 상태
        this.bufferRows = 8;
        this.fallbackRowHeight = 40;
        this.measuredRowHeight = null;
        this.windowStart = -1;
        this.windowEnd = -1;
        this.renderWindowScheduled = false;
    }
    init() {
        $("#btnToggleGroupPanel").on("click", () => {
            this.toggleGroupPanel();
        });
        $("#currentValueScroll").on("scroll", () => {
            this.handleScroll();
        });
        $("#groupFilterHost").on("click", "[data-group-id]", event => {
            var _a;
            const groupId = String((_a = $(event.currentTarget).attr("data-group-id")) !== null && _a !== void 0 ? _a : "");
            this.selectGroup(groupId);
        });
        $("#btnRefreshCurrent").on("click", () => {
            var _a;
            this.keyword = String((_a = $("#txtKeyword").val()) !== null && _a !== void 0 ? _a : "").trim();
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
            var _a;
            (_a = this.eventSource) === null || _a === void 0 ? void 0 : _a.close();
        });
    }
    toggleGroupPanel() {
        const $panel = $("#currentGroupPanel");
        const collapsed = !$panel.hasClass("is-collapsed");
        $panel.toggleClass("is-collapsed", collapsed);
        $("#btnToggleGroupPanel").attr("aria-expanded", String(!collapsed));
        $("#lblGroupToggleText").text(collapsed ? "펼치기" : "접기");
        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }
    async reload() {
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
    async loadGroups() {
        var _a;
        try {
            const response = await fetch("/api/currentvalue/groups");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const result = await response.json();
            if (!result.success) {
                return;
            }
            this.renderGroups((_a = result.data) !== null && _a !== void 0 ? _a : []);
        }
        catch (e) {
            console.error(e);
        }
    }
    renderGroups(groups) {
        var _a;
        const total = groups.reduce((sum, item) => sum + item.count, 0);
        const html = [];
        html.push(`
            <button type="button"
                    class="wf-current-group-chip ${this.selectedGroupId === "" ? "is-active" : ""}"
                    data-group-id="">
                전체
                <span>${total.toLocaleString()}</span>
            </button>
        `);
        for (const group of groups) {
            const groupId = (_a = group.groupId) !== null && _a !== void 0 ? _a : "";
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
    selectGroup(groupId) {
        if (this.selectedGroupId === groupId) {
            return;
        }
        this.selectedGroupId = groupId;
        $("#groupFilterHost [data-group-id]").removeClass("is-active");
        $(`#groupFilterHost [data-group-id="${this.escapeSelectorValue(groupId)}"]`).addClass("is-active");
        $("#lblGroupSummary").text(groupId.length === 0 ? "전체 그룹" : groupId);
        void this.reload();
    }
    requestSearch() {
        if (this.searchTimer != null) {
            window.clearTimeout(this.searchTimer);
            this.searchTimer = null;
        }
        this.searchTimer = window.setTimeout(() => {
            var _a;
            this.searchTimer = null;
            const nextKeyword = String((_a = $("#txtKeyword").val()) !== null && _a !== void 0 ? _a : "").trim();
            if (this.keyword === nextKeyword) {
                return;
            }
            this.keyword = nextKeyword;
            void this.reload();
        }, 350);
    }
    applySearchImmediately() {
        var _a;
        if (this.searchTimer != null) {
            window.clearTimeout(this.searchTimer);
            this.searchTimer = null;
        }
        const nextKeyword = String((_a = $("#txtKeyword").val()) !== null && _a !== void 0 ? _a : "").trim();
        if (this.keyword === nextKeyword) {
            return;
        }
        this.keyword = nextKeyword;
        void this.reload();
    }
    async loadNextPage() {
        var _a;
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
            const result = await response.json();
            if (!result.success) {
                throw new Error("CurrentValue 조회 실패");
            }
            const page = result.data;
            const pageRows = ((_a = page.rows) !== null && _a !== void 0 ? _a : []).map(x => this.normalizeCurrentRow(x));
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
        }
        catch (e) {
            console.error(e);
            if (this.rows.length === 0) {
                $("#currentValueBody").html(`
                    <tr>
                        <td colspan="10" class="wf-current-empty-cell">데이터 조회에 실패했습니다.</td>
                    </tr>
                `);
            }
        }
        finally {
            this.isLoading = false;
            $("#currentValueLoading").addClass("d-none");
        }
    }
    handleScroll() {
        const scrollEl = document.getElementById("currentValueScroll");
        if (scrollEl == null) {
            return;
        }
        const remain = scrollEl.scrollHeight - scrollEl.scrollTop - scrollEl.clientHeight;
        if (remain < 260) {
            void this.loadNextPage();
        }
        this.scheduleRenderWindow();
    }
    connectStream() {
        var _a;
        (_a = this.eventSource) === null || _a === void 0 ? void 0 : _a.close();
        this.setStreamStatus("wait", "연결 중");
        const source = new EventSource("/api/currentvalue/stream");
        this.eventSource = source;
        source.addEventListener("connected", () => {
            this.setStreamStatus("connected", "연결됨");
        });
        source.addEventListener("currentvalue", (event) => {
            try {
                const row = this.normalizeCurrentRow(JSON.parse(event.data));
                this.applyUpdate(row);
            }
            catch (e) {
                console.error("currentvalue parse error", e, event.data);
            }
        });
        source.onerror = e => {
            console.error("currentvalue stream error", e);
            this.setStreamStatus("error", "재연결 중");
        };
    }
    applyUpdate(row) {
        var _a;
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
        const changed = existing.value !== row.value ||
            existing.cookieValue !== row.cookieValue ||
            existing.status !== row.status ||
            existing.updatedAt !== row.updatedAt;
        existing.collectionSetting = (_a = row.collectionSetting) !== null && _a !== void 0 ? _a : existing.collectionSetting;
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
    isMatchedCurrentFilter(row) {
        var _a, _b, _c, _d, _e, _f;
        const groupId = (_a = row.groupId) !== null && _a !== void 0 ? _a : "";
        if (this.selectedGroupId.length > 0 && groupId !== this.selectedGroupId) {
            return false;
        }
        if (this.keyword.length === 0) {
            return true;
        }
        const q = this.keyword.toLowerCase();
        return String((_b = row.collectionSetting) !== null && _b !== void 0 ? _b : "").toLowerCase().includes(q) ||
            String((_c = row.groupId) !== null && _c !== void 0 ? _c : "").toLowerCase().includes(q) ||
            String((_d = row.tagId) !== null && _d !== void 0 ? _d : "").toLowerCase().includes(q) ||
            String((_e = row.value) !== null && _e !== void 0 ? _e : "").toLowerCase().includes(q) ||
            String((_f = row.cookieValue) !== null && _f !== void 0 ? _f : "").toLowerCase().includes(q);
    }
    triggerFlash(key) {
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
    scheduleRenderWindow() {
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
    renderWindow(force = false) {
        var _a;
        const tbody = document.getElementById("currentValueBody");
        const scrollEl = document.getElementById("currentValueScroll");
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
        const rowHeight = (_a = this.measuredRowHeight) !== null && _a !== void 0 ? _a : this.fallbackRowHeight;
        const scrollTop = scrollEl.scrollTop;
        const viewportHeight = scrollEl.clientHeight;
        let startIndex = Math.floor(scrollTop / rowHeight) - this.bufferRows;
        let endIndex = Math.ceil((scrollTop + viewportHeight) / rowHeight) + this.bufferRows;
        startIndex = Math.max(0, startIndex);
        endIndex = Math.min(this.rows.length, endIndex);
        if (!force &&
            startIndex === this.windowStart &&
            endIndex === this.windowEnd &&
            tbody.childElementCount > 0) {
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
            const sample = tbody.querySelector("tr:not(.wf-virtual-spacer)");
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
    buildSpacerRow(height) {
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
    buildRowElement(row, index, key) {
        const template = document.createElement("template");
        template.innerHTML = this.renderRow(row, index, key).trim();
        return template.content.firstElementChild;
    }
    /**
     * 화면에 렌더링되어 있는 <tr> 의 값 관련 셀만 직접 갱신한다.
     * (innerHTML 재생성 없이 textContent / title / class 만 변경)
     */
    patchRowElement(tr, row) {
        var _a, _b, _c, _d;
        const valueEl = tr.querySelector('[data-cell="value"]');
        if (valueEl != null) {
            valueEl.textContent = (_a = row.value) !== null && _a !== void 0 ? _a : "-";
            valueEl.title = (_b = row.value) !== null && _b !== void 0 ? _b : "";
        }
        const cookieEl = tr.querySelector('[data-cell="cookie"]');
        if (cookieEl != null) {
            cookieEl.textContent = (_c = row.cookieValue) !== null && _c !== void 0 ? _c : "-";
            cookieEl.title = (_d = row.cookieValue) !== null && _d !== void 0 ? _d : "";
        }
        const statusEl = tr.querySelector('[data-cell="status"]');
        if (statusEl != null) {
            const isGood = this.isGoodStatus(row.status);
            statusEl.classList.toggle("is-good", isGood);
            statusEl.classList.toggle("is-bad", !isGood);
            const statusTextEl = statusEl.querySelector(".wf-status-text");
            if (statusTextEl != null) {
                statusTextEl.textContent = this.formatStatus(row.status);
            }
        }
        const updateCountEl = tr.querySelector('[data-cell="updateCount"]');
        if (updateCountEl != null) {
            updateCountEl.textContent = this.formatNumber(row.updateCount);
        }
        const sourceTimeEl = tr.querySelector('[data-cell="sourceTimestamp"]');
        if (sourceTimeEl != null) {
            sourceTimeEl.textContent = this.formatDate(row.sourceTimestamp);
        }
        const updatedAtEl = tr.querySelector('[data-cell="updatedAt"]');
        if (updatedAtEl != null) {
            updatedAtEl.textContent = this.formatDate(row.updatedAt);
        }
    }
    renderRow(row, index, key) {
        var _a, _b, _c, _d, _e, _f;
        const groupId = (_a = row.groupId) !== null && _a !== void 0 ? _a : "";
        const isGood = this.isGoodStatus(row.status);
        const statusText = this.formatStatus(row.status);
        const flashClass = this.flashKeys.has(key) ? "value-flash" : "";
        const collectionSetting = (_b = row.collectionSetting) !== null && _b !== void 0 ? _b : "";
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
                      title="${this.escapeHtml((_c = row.value) !== null && _c !== void 0 ? _c : "")}">
                    ${this.escapeHtml((_d = row.value) !== null && _d !== void 0 ? _d : "-")}
                </span>
            </td>
            <td>
                <span class="wf-current-cookie-value"
                      data-cell="cookie"
                      title="${this.escapeHtml((_e = row.cookieValue) !== null && _e !== void 0 ? _e : "")}">
                    ${this.escapeHtml((_f = row.cookieValue) !== null && _f !== void 0 ? _f : "-")}
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
    updateHeaderCount() {
        $("#lblTotalCount").text(this.totalCount.toLocaleString());
        $("#lblLoadedCount").text(this.loadedCount.toLocaleString());
        $("#lblUpdateCount").text(this.updateCount.toLocaleString());
    }
    setStreamStatus(status, text) {
        $("#lblStreamStatus").text(text);
        $("#lblStreamChip")
            .removeClass("is-wait is-connected is-error")
            .addClass(`is-${status}`);
    }
    formatStatus(status) {
        if (status == null || status === "") {
            return "-";
        }
        if (typeof status === "number") {
            if (status === 0)
                return "Good";
            if (status === 1)
                return "Bad";
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
    isGoodStatus(status) {
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
    normalizeCurrentRow(row) {
        var _a;
        return {
            ...row,
            groupId: this.readValue(row, "groupId", "GROUP_ID", "group_id"),
            tagId: String((_a = this.readValue(row, "tagId", "TAG_ID", "tag_id")) !== null && _a !== void 0 ? _a : ""),
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
    readValue(row, ...names) {
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
    readNumber(row, ...names) {
        const value = this.readValue(row, ...names);
        if (value == null || value === "") {
            return null;
        }
        const numberValue = Number(value);
        return Number.isFinite(numberValue)
            ? numberValue
            : null;
    }
    normalizeFieldName(value) {
        return String(value !== null && value !== void 0 ? value : "")
            .replace(/_/g, "")
            .replace(/-/g, "")
            .toLowerCase();
    }
    makeKey(groupId, tagId) {
        return `${groupId !== null && groupId !== void 0 ? groupId : ""}||${tagId}`;
    }
    formatDate(value) {
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
    formatNumber(value) {
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
            const lucide = window.lucide;
            if ((lucide === null || lucide === void 0 ? void 0 : lucide.createIcons) != null) {
                lucide.createIcons();
            }
        }, 0);
    }
    escapeSelectorValue(value) {
        return value.replace(/\\/g, "\\\\").replace(/"/g, '\\"');
    }
    escapeHtml(value) {
        return String(value !== null && value !== void 0 ? value : "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
}


/***/ }

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		var cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		if (!(moduleId in __webpack_modules__)) {
/******/ 			delete __webpack_module_cache__[moduleId];
/******/ 			var e = new Error("Cannot find module '" + moduleId + "'");
/******/ 			e.code = 'MODULE_NOT_FOUND';
/******/ 			throw e;
/******/ 		}
/******/ 		__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/define property getters */
/******/ 	(() => {
/******/ 		// define getter functions for harmony exports
/******/ 		__webpack_require__.d = (exports, definition) => {
/******/ 			for(var key in definition) {
/******/ 				if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 					Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	(() => {
/******/ 		__webpack_require__.o = (obj, prop) => (Object.prototype.hasOwnProperty.call(obj, prop))
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	(() => {
/******/ 		// define __esModule on exports
/******/ 		__webpack_require__.r = (exports) => {
/******/ 			if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 				Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 			}
/******/ 			Object.defineProperty(exports, '__esModule', { value: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
var __webpack_exports__ = {};
// This entry needs to be wrapped in an IIFE because it needs to be isolated against other entry modules.
(() => {
var __webpack_exports__ = {};
/*!**************************************************!*\
  !*** ./Scripts/.generated/views__main__index.ts ***!
  \**************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_main_index__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/main/index */ "./Scripts/src/ts/views/main/index.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_main_index__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

// This entry needs to be wrapped in an IIFE because it needs to be isolated against other entry modules.
(() => {
/*!**********************************************!*\
  !*** ./Scripts/src/css/views/main/index.css ***!
  \**********************************************/
__webpack_require__.r(__webpack_exports__);
// extracted by mini-css-extract-plugin

})();

/******/ })()
;
//# sourceMappingURL=index.js.map