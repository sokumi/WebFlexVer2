/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./Scripts/src/ts/framework/notify.ts"
/*!********************************************!*\
  !*** ./Scripts/src/ts/framework/notify.ts ***!
  \********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   notify: () => (/* binding */ notify)
/* harmony export */ });
const defaultDuration = 2500;
function getHost() {
    let host = document.getElementById("wfToastHost");
    if (host != null) {
        return host;
    }
    host = document.createElement("div");
    host.id = "wfToastHost";
    host.className = "wf-toast-host";
    host.setAttribute("aria-live", "polite");
    host.setAttribute("aria-atomic", "true");
    document.body.appendChild(host);
    return host;
}
function getIcon(type) {
    if (type === "success") {
        return "✓";
    }
    if (type === "warning") {
        return "!";
    }
    if (type === "error") {
        return "×";
    }
    return "i";
}
function removeToast(toast) {
    toast.classList.add("is-hide");
    window.setTimeout(() => {
        toast.remove();
    }, 180);
}
function show(message, options = {}) {
    var _a, _b;
    const type = (_a = options.type) !== null && _a !== void 0 ? _a : "info";
    const duration = (_b = options.duration) !== null && _b !== void 0 ? _b : defaultDuration;
    const host = getHost();
    const toast = document.createElement("div");
    toast.className = `wf-toast ${type}`;
    toast.innerHTML = `
        <span class="wf-toast-icon">${getIcon(type)}</span>
        <span class="wf-toast-message"></span>
        <button class="wf-toast-close" type="button" aria-label="알림 닫기">×</button>
    `;
    const messageElement = toast.querySelector(".wf-toast-message");
    const closeButton = toast.querySelector(".wf-toast-close");
    if (messageElement != null) {
        messageElement.textContent = message;
    }
    closeButton === null || closeButton === void 0 ? void 0 : closeButton.addEventListener("click", () => {
        removeToast(toast);
    });
    host.appendChild(toast);
    if (duration > 0) {
        window.setTimeout(() => {
            removeToast(toast);
        }, duration);
    }
}
const notify = {
    success(message, duration) {
        show(message, {
            type: "success",
            duration
        });
    },
    info(message, duration) {
        show(message, {
            type: "info",
            duration
        });
    },
    warning(message, duration) {
        show(message, {
            type: "warning",
            duration
        });
    },
    error(message, duration) {
        show(message, {
            type: "error",
            duration
        });
    },
    show
};


/***/ },

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

/***/ "./Scripts/src/ts/views/opc/opc1000.ts"
/*!*********************************************!*\
  !*** ./Scripts/src/ts/views/opc/opc1000.ts ***!
  \*********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
/* harmony import */ var _framework_notify__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../../framework/notify */ "./Scripts/src/ts/framework/notify.ts");

class Page {
    constructor() {
        this.devices = [];
        this.deviceSummaries = [];
        this.deviceStatuses = new Map();
        this.isLogAutoRefresh = false;
        this.isLogCollapsed = true;
    }
    init() {
        $("#deviceCardHost").on("click", "[data-subscription-action]", event => {
            var _a, _b;
            const $button = $(event.currentTarget);
            const deviceId = String((_a = $button.attr("data-device-id")) !== null && _a !== void 0 ? _a : "");
            const action = String((_b = $button.attr("data-subscription-action")) !== null && _b !== void 0 ? _b : "");
            void this.postDeviceSubscription(deviceId, action);
        });
        $("#btnClearLogs").on("click", () => this.clearLogs());
        $("#btnLoadLogs").on("click", () => {
            void this.startLogAutoRefresh();
        });
        $("#btnToggleLogs").on("click", () => this.toggleLogs());
        this.setLogCollapsed(true);
        void this.refresh();
        window.setInterval(() => {
            void this.refresh();
        }, 3000);
    }
    async refresh() {
        await this.loadDevices();
        await this.loadStatus();
        await this.loadDeviceSummary();
        await this.loadDeviceCards();
        if (this.isLogAutoRefresh) {
            await this.loadLogs();
        }
    }
    async loadDevices() {
        var _a;
        try {
            const res = await this.get("/device/manage/list");
            this.devices = ((_a = res.data) !== null && _a !== void 0 ? _a : []).slice(0, 2);
        }
        catch (e) {
            console.error(e);
            this.devices = [];
            this.renderDeviceError("디바이스 조회 실패");
        }
    }
    async loadStatus() {
        var _a, _b, _c, _d;
        try {
            const response = await fetch("/api/opc-collector/status");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            const version = (_b = (_a = data.collectorVersion) !== null && _a !== void 0 ? _a : data.version) !== null && _b !== void 0 ? _b : "v2.4.1.20260618";
            const totalDeviceCount = this.devices.length > 0
                ? this.devices.length
                : (_c = data.deviceCount) !== null && _c !== void 0 ? _c : 0;
            this.setTextWithFlash("#lblDeviceCount", `${this.formatNumber(totalDeviceCount)}/${this.formatNumber(totalDeviceCount)}`);
            this.setTextWithFlash("#lblSubscribedCount", this.formatNumber(data.subscribedCount));
            this.setTextWithFlash("#lblTotalSnapshotRows", this.formatNumber(data.totalSnapshotRows));
            this.setTextWithFlash("#lblTotalInserted", this.formatNumber(data.totalInserted));
            this.setTextWithFlash("#lblCollectorVersion", version);
            $("#lblSnapshotTime").text(`마지막 ${this.getCurrentTimeText()}`);
            $("#lblCollectorDbStatus").text((_d = data.dbStatus) !== null && _d !== void 0 ? _d : "DB 정상");
            this.updateStoppedDeviceCount();
        }
        catch (e) {
            console.error(e);
            this.setTextWithFlash("#lblDeviceCount", "조회 실패");
            this.setTextWithFlash("#lblSubscribedCount", "-");
            this.setTextWithFlash("#lblTotalSnapshotRows", "-");
            this.setTextWithFlash("#lblTotalInserted", "-");
            this.setTextWithFlash("#lblCollectorVersion", "-");
            $("#lblStoppedCount").text("상태 조회 실패");
            $("#lblCollectorDbStatus").text("DB 상태 -");
        }
    }
    async loadDeviceSummary() {
        try {
            const response = await fetch("/api/opc-collector/device-summary");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            this.deviceSummaries = await response.json();
        }
        catch (e) {
            console.error(e);
            this.deviceSummaries = [];
        }
    }
    async loadDeviceCards() {
        if (this.devices.length === 0) {
            this.renderDeviceEmpty();
            return;
        }
        await Promise.all(this.devices.map(async (device) => {
            var _a, _b;
            const deviceId = (_a = device.id) !== null && _a !== void 0 ? _a : "";
            if (deviceId.length === 0) {
                return;
            }
            try {
                const response = await fetch(`/api/opc-collector/device/${encodeURIComponent(deviceId)}/status`);
                if (!response.ok) {
                    throw new Error(await response.text());
                }
                const status = await response.json();
                this.deviceStatuses.set(deviceId, status);
            }
            catch (e) {
                console.error(e);
                this.deviceStatuses.set(deviceId, {
                    deviceId,
                    deviceName: (_b = device.deviceName) !== null && _b !== void 0 ? _b : "-",
                    runtimeStatus: {
                        subscriptionStopped: true
                    }
                });
            }
        }));
        this.renderDeviceCards();
        this.updateStoppedDeviceCount();
        this.refreshIcons();
    }
    renderDeviceCards() {
        const rows = this.devices.map(device => {
            var _a, _b, _c;
            const deviceId = (_a = device.id) !== null && _a !== void 0 ? _a : "";
            const summary = (_b = this.deviceSummaries.find(x => x.deviceId === deviceId)) !== null && _b !== void 0 ? _b : null;
            const status = (_c = this.deviceStatuses.get(deviceId)) !== null && _c !== void 0 ? _c : null;
            return {
                device,
                summary,
                status
            };
        });
        $("#lblDeviceSummaryText").text(`${rows.length.toLocaleString()}개 디바이스`);
        const html = rows
            .map(row => this.createDeviceCardHtml(row))
            .join("");
        $("#deviceCardHost").html(html || this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }
    createDeviceCardHtml(row) {
        var _a, _b, _c, _d, _e, _f, _g, _h, _j, _k, _l, _m, _o, _p, _q;
        const device = row.device;
        const status = row.status;
        const runtime = (_a = status === null || status === void 0 ? void 0 : status.runtimeStatus) !== null && _a !== void 0 ? _a : {};
        const deviceId = (_c = (_b = device.id) !== null && _b !== void 0 ? _b : status === null || status === void 0 ? void 0 : status.deviceId) !== null && _c !== void 0 ? _c : "";
        const deviceName = (_e = (_d = status === null || status === void 0 ? void 0 : status.deviceName) !== null && _d !== void 0 ? _d : device.deviceName) !== null && _e !== void 0 ? _e : "-";
        const deviceCode = (_f = device.deviceCode) !== null && _f !== void 0 ? _f : deviceId;
        const deviceType = (_g = device.deviceType) !== null && _g !== void 0 ? _g : "OPCUA";
        const endpoint = this.getEndpointText(device);
        const tagCount = (_j = (_h = status === null || status === void 0 ? void 0 : status.tagCount) !== null && _h !== void 0 ? _h : device.tagCount) !== null && _j !== void 0 ? _j : 0;
        const subscribedCount = (_k = runtime.subscribedCount) !== null && _k !== void 0 ? _k : 0;
        const currentValueCount = (_l = runtime.currentValueCount) !== null && _l !== void 0 ? _l : 0;
        const totalInserted = (_m = status === null || status === void 0 ? void 0 : status.totalInserted) !== null && _m !== void 0 ? _m : 0;
        const subscriptionStopped = runtime.subscriptionStopped === true
            || ((_o = row.summary) === null || _o === void 0 ? void 0 : _o.subscriptionStatus) === "Stopped"
            || ((_p = row.summary) === null || _p === void 0 ? void 0 : _p.subscriptionStatus) === "SubscriptionStopped"
            || ((_q = row.summary) === null || _q === void 0 ? void 0 : _q.subscriptionStatus) === "중지";
        const isRunning = !subscriptionStopped && subscribedCount > 0;
        const statusText = isRunning ? "연결됨" : "구독중지";
        const cardStateClass = isRunning ? "is-running" : "is-stopped";
        const percent = tagCount > 0
            ? Math.min(100, Math.round((subscribedCount / tagCount) * 100))
            : 0;
        return `
            <article class="wf-opc-device-card ${cardStateClass}">
                <div class="wf-opc-device-head">
                    <div class="wf-opc-device-title">
                        <span class="wf-opc-device-icon">
                            <i data-lucide="cpu"></i>
                        </span>
                        <div>
                            <h4>${this.escapeHtml(deviceName)}</h4>
                            <small>${this.escapeHtml(deviceCode)}</small>
                        </div>
                    </div>

                    <span class="wf-opc-device-status ${cardStateClass}">
                        <span class="wf-status-dot"></span>
                        ${this.escapeHtml(statusText)}
                    </span>
                </div>

                <div class="wf-opc-device-type">${this.escapeHtml(deviceType)}</div>

                <div class="wf-opc-endpoint">
                    <i data-lucide="link"></i>
                    <span>${this.escapeHtml(endpoint)}</span>
                </div>

                <div class="wf-opc-device-metrics">
                    <div>
                        <span>구독 태그</span>
                        <strong>${this.formatNumber(subscribedCount)} <small>/ ${this.formatNumber(tagCount)}</small></strong>
                    </div>
                    <div>
                        <span>현재값</span>
                        <strong>${this.formatNumber(currentValueCount)} <small>rows</small></strong>
                    </div>
                    <div>
                        <span>CPU</span>
                        <strong>-</strong>
                    </div>
                    <div>
                        <span>메모리</span>
                        <strong>-</strong>
                    </div>
                </div>

                <div class="wf-opc-progress">
                    <span style="width:${percent}%"></span>
                </div>

                <div class="wf-opc-device-footer">
                    <div class="wf-opc-device-time">
                        <span>
                            <i data-lucide="timer"></i>
                            -
                        </span>
                        <span>
                            <i data-lucide="clock"></i>
                            ${this.getCurrentTimeText()}
                        </span>
                    </div>

                    <div class="wf-opc-device-actions">
                        <button type="button"
                                class="btn btn-outline-primary btn-sm"
                                data-device-id="${this.escapeHtml(deviceId)}"
                                data-subscription-action="${isRunning ? "stop" : "start"}">
                            <i data-lucide="${isRunning ? "pause-circle" : "play-circle"}"></i>
                            ${isRunning ? "구독중지" : "구독시작"}
                        </button>

                        <span class="wf-opc-db-badge">
                            <i data-lucide="save"></i>
                            ${totalInserted > 0 ? "DB저장중" : "DB대기"}
                        </span>
                    </div>
                </div>
            </article>
        `;
    }
    updateStoppedDeviceCount() {
        const totalCount = this.devices.length;
        if (totalCount === 0) {
            $("#lblStoppedCount").text("0개 구독중지");
            return;
        }
        const stoppedCount = this.devices.filter(device => {
            var _a, _b, _c, _d;
            const deviceId = (_a = device.id) !== null && _a !== void 0 ? _a : "";
            const summary = (_b = this.deviceSummaries.find(x => x.deviceId === deviceId)) !== null && _b !== void 0 ? _b : null;
            const status = (_c = this.deviceStatuses.get(deviceId)) !== null && _c !== void 0 ? _c : null;
            const runtime = (_d = status === null || status === void 0 ? void 0 : status.runtimeStatus) !== null && _d !== void 0 ? _d : {};
            return runtime.subscriptionStopped === true
                || (summary === null || summary === void 0 ? void 0 : summary.subscriptionStatus) === "Stopped"
                || (summary === null || summary === void 0 ? void 0 : summary.subscriptionStatus) === "SubscriptionStopped"
                || (summary === null || summary === void 0 ? void 0 : summary.subscriptionStatus) === "중지";
        }).length;
        if (stoppedCount <= 0) {
            $("#lblStoppedCount").text("0개 구독중지");
            return;
        }
        $("#lblStoppedCount").text(`${stoppedCount.toLocaleString()}개 구독중지`);
    }
    renderDeviceEmpty() {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("0개 구독중지");
        $("#deviceCardHost").html(this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }
    renderDeviceError(message) {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("상태 조회 실패");
        $("#deviceCardHost").html(this.createEmptyHtml(message));
        this.refreshIcons();
    }
    createEmptyHtml(message) {
        return `
            <article class="wf-opc-empty-card">
                <i data-lucide="server-off"></i>
                <strong>${this.escapeHtml(message)}</strong>
                <span>Collector 상태 또는 디바이스 설정을 확인해 주세요.</span>
            </article>
        `;
    }
    async postDeviceSubscription(deviceId, action) {
        if (deviceId.length === 0) {
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.warning("디바이스 정보가 없습니다.");
            return;
        }
        try {
            const response = await fetch(`/api/opc-collector/device/${encodeURIComponent(deviceId)}/subscription/${action}`, {
                method: "POST"
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.success(action === "start" ? "디바이스 구독 재시작 요청 완료" : "디바이스 구독 중지 요청 완료");
            await this.loadStatus();
            await this.loadDeviceSummary();
            await this.loadDeviceCards();
        }
        catch (e) {
            console.error(e);
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.error("요청 처리 중 오류가 발생했습니다.");
        }
    }
    async startLogAutoRefresh() {
        this.isLogAutoRefresh = true;
        this.setLogCollapsed(false);
        await this.loadLogs();
    }
    clearLogs() {
        $("#logBox").empty();
        $("#lblLogCount").text("0개 항목");
    }
    async loadLogs() {
        try {
            const response = await fetch("/api/opc-collector/logs");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const logs = await response.json();
            const html = logs
                .map(x => {
                var _a, _b, _c;
                const time = this.escapeHtml((_a = x.time) !== null && _a !== void 0 ? _a : "");
                const level = this.escapeHtml((_b = x.level) !== null && _b !== void 0 ? _b : "");
                const message = this.escapeHtml((_c = x.message) !== null && _c !== void 0 ? _c : "");
                return `<div class="wf-opc-log-row">
                        <span class="wf-opc-log-time">${time}</span>
                        <span class="wf-opc-log-level">${level}</span>
                        <span class="wf-opc-log-message">${message}</span>
                    </div>`;
            })
                .join("");
            $("#logBox").html(html || "<div class='wf-opc-log-empty'>로그가 없습니다.</div>");
            $("#lblLogCount").text(`${logs.length.toLocaleString()}개 항목`);
        }
        catch (e) {
            console.error(e);
            $("#logBox").html("<div class='wf-opc-log-empty'>로그 조회 실패</div>");
            $("#lblLogCount").text("0개 항목");
        }
    }
    toggleLogs() {
        this.setLogCollapsed(!this.isLogCollapsed);
    }
    setLogCollapsed(isCollapsed) {
        this.isLogCollapsed = isCollapsed;
        $("#opcLogCard").toggleClass("is-collapsed", isCollapsed);
        $("#lblLogToggleText").text(isCollapsed ? "펼치기" : "접기");
        $("#btnToggleLogs").attr("aria-expanded", String(!isCollapsed));
        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }
    getEndpointText(device) {
        var _a;
        if (device.endpointUrl != null && device.endpointUrl.length > 0) {
            return device.endpointUrl.replace(/^opc\.tcp:\/\//i, "");
        }
        const address = (_a = device.deviceAddress) !== null && _a !== void 0 ? _a : "";
        const port = device.port == null ? "" : String(device.port);
        if (address.length > 0 && port.length > 0) {
            return `${address}:${port}`;
        }
        return "-";
    }
    getCurrentTimeText() {
        const now = new Date();
        return [
            String(now.getHours()).padStart(2, "0"),
            String(now.getMinutes()).padStart(2, "0"),
            String(now.getSeconds()).padStart(2, "0")
        ].join(":");
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
    async get(url) {
        return await $.ajax({
            url,
            method: "GET",
            dataType: "json"
        });
    }
    refreshIcons() {
        window.setTimeout(() => {
            const lucide = window.lucide;
            if ((lucide === null || lucide === void 0 ? void 0 : lucide.createIcons) != null) {
                lucide.createIcons();
            }
        }, 0);
    }
    escapeHtml(value) {
        return String(value !== null && value !== void 0 ? value : "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
    setTextWithFlash(selector, value) {
        const $el = $(selector);
        const newText = value == null || value === "" ? "-" : String(value);
        const oldText = $el.text();
        if (oldText === newText) {
            return;
        }
        $el.text(newText);
        const element = $el[0];
        if (element != null) {
            void element.offsetWidth;
        }
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
// This entry needs to be wrapped in an IIFE because it needs to be isolated against other modules in the chunk.
(() => {
/*!***************************************************!*\
  !*** ./Scripts/.generated/views__opc__opc1000.ts ***!
  \***************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_opc_opc1000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/opc/opc1000 */ "./Scripts/src/ts/views/opc/opc1000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_opc_opc1000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=opc1000.js.map