/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./WebFlex.UI/Scripts/src/ts/framework/page.ts"
/*!*****************************************************!*\
  !*** ./WebFlex.UI/Scripts/src/ts/framework/page.ts ***!
  \*****************************************************/
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

/***/ "./WebFlex.UI/Scripts/src/ts/views/opc/opc1000.ts"
/*!********************************************************!*\
  !*** ./WebFlex.UI/Scripts/src/ts/views/opc/opc1000.ts ***!
  \********************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.devices = [];
        this.selectedDeviceId = "";
        this.isLogAutoRefresh = false;
        this.isSelectedDeviceAutoRefresh = false;
        this.selDevice_onChange = () => {
            var _a;
            this.selectedDeviceId = String((_a = $("#selDevice").val()) !== null && _a !== void 0 ? _a : "");
        };
    }
    init() {
        $("#selDevice").on("change", this.selDevice_onChange);
        $("#btnStopSubscription").on("click", () => this.post("/api/opc-collector/subscription/stop"));
        $("#btnStartSubscription").on("click", () => this.post("/api/opc-collector/subscription/start"));
        $("#btnRefresh").on("click", () => this.refresh());
        $("#btnLoadDeviceStatus").on("click", () => this.startSelectedDeviceAutoRefresh());
        $("#btnStopDeviceSubscription").on("click", () => this.postSelectedDevice("stop"));
        $("#btnStartDeviceSubscription").on("click", () => this.postSelectedDevice("start"));
        $("#btnClearLogs").on("click", () => this.clearLogs());
        $("#btnLoadLogs").on("click", () => this.startLogAutoRefresh());
        this.refresh();
        this.loadDevices();
        window.setInterval(() => {
            this.refresh();
        }, 3000);
    }
    async loadDevices() {
        var _a;
        try {
            const res = await this.get("/device/manage/list");
            this.devices = (_a = res.data) !== null && _a !== void 0 ? _a : [];
            const $selDevice = $("#selDevice");
            $selDevice.empty();
            $selDevice.append(`<option value="">디바이스 선택</option>`);
            for (const device of this.devices) {
                $selDevice.append(`<option value="${this.escapeHtml(device.id)}">${this.escapeHtml(device.deviceName)} (${this.escapeHtml(device.deviceType)})</option>`);
            }
        }
        catch (e) {
            alert(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }
    async get(url) {
        return await $.ajax({
            url,
            method: "GET",
            dataType: "json"
        });
    }
    async postSelectedDevice(action) {
        const deviceId = this.selectedDeviceId;
        if (!deviceId) {
            alert("디바이스를 선택하세요.");
            return;
        }
        const encodedDeviceId = encodeURIComponent(deviceId);
        await this.post(`/api/opc-collector/device/${encodedDeviceId}/subscription/${action}`);
        await this.loadSelectedDeviceStatus(false);
    }
    async post(url) {
        try {
            const response = await fetch(url, {
                method: "POST"
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            await this.refresh();
        }
        catch (e) {
            console.error(e);
            alert("요청 처리 중 오류가 발생했습니다.");
        }
    }
    async refresh() {
        await this.loadStatus();
        await this.loadDeviceSummary();
        if (this.isSelectedDeviceAutoRefresh) {
            await this.loadSelectedDeviceStatus(false);
        }
        if (this.isLogAutoRefresh) {
            await this.loadLogs();
        }
    }
    async loadDeviceSummary() {
        try {
            const response = await fetch("/api/opc-collector/device-summary");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const rows = await response.json();
            const html = rows
                .map(x => {
                var _a, _b;
                return `
                    <tr>
                        <td>${this.escapeHtml((_a = x.deviceName) !== null && _a !== void 0 ? _a : "-")}</td>
                        <td>${this.escapeHtml((_b = x.subscriptionStatus) !== null && _b !== void 0 ? _b : "-")}</td>
                    </tr>
                `;
            })
                .join("");
            $("#deviceSummaryBody").html(html || `<tr><td colspan="3">조회된 디바이스가 없습니다.</td></tr>`);
        }
        catch (e) {
            console.error(e);
            $("#deviceSummaryBody").html(`<tr><td colspan="3">조회 실패</td></tr>`);
        }
    }
    async startSelectedDeviceAutoRefresh() {
        const deviceId = this.selectedDeviceId;
        if (!deviceId) {
            alert("디바이스를 선택하세요.");
            return;
        }
        this.isSelectedDeviceAutoRefresh = true;
        await this.loadSelectedDeviceStatus(false);
    }
    async startLogAutoRefresh() {
        this.isLogAutoRefresh = true;
        await this.loadLogs();
    }
    clearLogs() {
        $("#logBox").empty();
    }
    async loadStatus() {
        try {
            const response = await fetch("/api/opc-collector/status");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            this.setTextWithFlash("#lblDeviceCount", data.deviceCount);
            this.setTextWithFlash("#lblSubscribedCount", data.subscribedCount);
            this.setTextWithFlash("#lblTotalSnapshotRows", data.totalSnapshotRows);
            this.setTextWithFlash("#lblTotalInserted", data.totalInserted);
            this.setTextWithFlash("#lblSubscriptionStopped", data.subscriptionStopped ? "중지" : "동작");
        }
        catch (e) {
            console.error(e);
            $("#lblDeviceCount").text("연결 실패");
        }
    }
    async loadSelectedDeviceStatus(showAlert = true) {
        var _a;
        const deviceId = this.selectedDeviceId;
        if (!deviceId) {
            if (showAlert) {
                alert("디바이스를 선택하세요.");
            }
            return;
        }
        try {
            const encodedDeviceId = encodeURIComponent(deviceId);
            const response = await fetch(`/api/opc-collector/device/${encodedDeviceId}/status`);
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            const runtime = (_a = data.runtimeStatus) !== null && _a !== void 0 ? _a : {};
            this.setTextWithFlash("#lblSelectedDeviceId", data.deviceId);
            this.setTextWithFlash("#lblSelectedDeviceName", data.deviceName || "-");
            this.setTextWithFlash("#lblSelectedTagCount", data.tagCount);
            this.setTextWithFlash("#lblSelectedSubscribedCount", runtime.subscribedCount);
            this.setTextWithFlash("#lblSelectedCurrentValueCount", runtime.currentValueCount);
            this.setTextWithFlash("#lblSelectedTotalSnapshotRows", data.totalSnapshotRows);
            this.setTextWithFlash("#lblSelectedTotalInserted", data.totalInserted);
            this.setTextWithFlash("#lblSelectedSubscriptionStopped", runtime.subscriptionStopped ? "중지" : "동작");
        }
        catch (e) {
            console.error(e);
            $("#lblSelectedDeviceName").text("조회 실패");
        }
    }
    async loadLogs() {
        try {
            const response = await fetch("/api/opc-collector/logs");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const logs = await response.json();
            const html = logs
                .map((x) => {
                var _a, _b, _c;
                const time = this.escapeHtml((_a = x.time) !== null && _a !== void 0 ? _a : "");
                const level = this.escapeHtml((_b = x.level) !== null && _b !== void 0 ? _b : "");
                const message = this.escapeHtml((_c = x.message) !== null && _c !== void 0 ? _c : "");
                return `<div class="opc-log-row">
                        <span class="opc-log-time">${time}</span>
                        <span class="opc-log-level">${level}</span>
                        <span class="opc-log-message">${message}</span>
                    </div>`;
            })
                .join("");
            $("#logBox").html(html || "<div class='opc-log-empty'>로그가 없습니다.</div>");
        }
        catch (e) {
            console.error(e);
            $("#logBox").html("<div class='opc-log-empty'>로그 조회 실패</div>");
        }
    }
    escapeHtml(value) {
        return value
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
        $el.removeClass("value-flash");
        void $el[0].offsetWidth;
        $el.addClass("value-flash");
        window.setTimeout(() => {
            $el.removeClass("value-flash");
        }, 700);
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
/*!**************************************************************!*\
  !*** ./WebFlex.UI/Scripts/.generated/views__opc__opc1000.ts ***!
  \**************************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_opc_opc1000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/opc/opc1000 */ "./WebFlex.UI/Scripts/src/ts/views/opc/opc1000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./WebFlex.UI/Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_opc_opc1000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=opc1000.js.map