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

/***/ "./Scripts/src/ts/views/opc/opc1000.ts"
/*!*********************************************!*\
  !*** ./Scripts/src/ts/views/opc/opc1000.ts ***!
  \*********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.timerId = null;
    }
    init() {
        $("#btnRestartDevice").on("click", () => this.restartDevice());
        $("#btnRestartAllDevices").on("click", () => this.post("/api/opc-collector/RestartAllDevices"));
        $("#btnStopSubscription").on("click", () => this.post("/api/opc-collector/StopSubscription"));
        $("#btnStartSubscription").on("click", () => this.post("/api/opc-collector/StartSubscription"));
        $("#btnStopDbSave").on("click", () => this.post("/api/opc-collector/StopDbSave"));
        $("#btnStartDbSave").on("click", () => this.post("/api/opc-collector/StartDbSave"));
        $("#btnRestartProcess").on("click", () => this.restartProcess());
        $("#btnRefresh").on("click", () => this.refresh());
        this.refresh();
        this.timerId = window.setInterval(() => {
            this.refresh();
        }, 3000);
    }
    async restartDevice() {
        const deviceId = Number($("#txtDeviceId").val());
        if (!deviceId || deviceId <= 0) {
            alert("DeviceId를 입력해줘.");
            return;
        }
        await this.post(`/api/opc-collector/RestartDevice?deviceId=${deviceId}`);
    }
    async restartProcess() {
        if (!confirm("OPC Collector 전체를 재가동할까요?")) {
            return;
        }
        await this.post("/api/opc-collector/RestartProcess");
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
        await Promise.all([
            this.loadStatus(),
            this.loadLogs()
        ]);
    }
    async loadStatus() {
        var _a, _b, _c, _d, _e;
        try {
            const response = await fetch("/api/opc-collector/Status");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            $("#lblDeviceCount").text((_a = data.deviceCount) !== null && _a !== void 0 ? _a : "-");
            $("#lblSubscribedCount").text((_b = data.subscribedCount) !== null && _b !== void 0 ? _b : "-");
            $("#lblQueueCount").text((_c = data.queueCount) !== null && _c !== void 0 ? _c : "-");
            $("#lblTotalEnqueued").text((_d = data.totalEnqueued) !== null && _d !== void 0 ? _d : "-");
            $("#lblTotalInserted").text((_e = data.totalInserted) !== null && _e !== void 0 ? _e : "-");
            $("#lblSubscriptionStopped").text(data.subscriptionStopped ? "중지" : "동작");
            $("#lblDbSaveStopped").text(data.dbSaveStopped ? "중지" : "동작");
        }
        catch (e) {
            console.error(e);
            $("#lblDeviceCount").text("연결 실패");
        }
    }
    async loadLogs() {
        try {
            const response = await fetch("/api/opc-collector/Logs");
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