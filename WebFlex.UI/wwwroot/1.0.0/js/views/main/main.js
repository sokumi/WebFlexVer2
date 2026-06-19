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

/***/ "./Scripts/src/ts/views/main/main.ts"
/*!*******************************************!*\
  !*** ./Scripts/src/ts/views/main/main.ts ***!
  \*******************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.rows = [];
        this.rowMap = new Map();
        this.eventSource = null;
        this.updateCount = 0;
        this.rowHeight = 34;
        this.buffer = 12;
        this.renderRequested = false;
        this.flashKeys = new Set();
        this.renderTimer = null;
    }
    init() {
        $("#currentValueScroll").on("scroll", () => {
            this.requestRender();
        });
        this.loadInitialData();
        this.connectStream();
    }
    async loadInitialData() {
        try {
            $("#lblStreamStatus").text("초기 조회 중");
            const response = await fetch("/api/currentvalue/list");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            this.rows = data;
            this.rowMap.clear();
            for (const row of this.rows) {
                this.rowMap.set(this.makeKey(row.endpointUrl, row.nodeId), row);
            }
            $("#lblTotalCount").text(String(this.rows.length));
            $("#lblStreamStatus").text("초기 조회 완료");
            this.requestRender();
        }
        catch (e) {
            console.error(e);
            $("#lblStreamStatus").text("초기 조회 실패");
            $("#currentValueBody").html(`<tr><td colspan="6">초기 조회 실패</td></tr>`);
        }
    }
    connectStream() {
        if (this.eventSource != null) {
            this.eventSource.close();
            this.eventSource = null;
        }
        $("#lblStreamStatus").text("연결 중");
        const source = new EventSource("/api/currentvalue/stream");
        this.eventSource = source;
        source.addEventListener("connected", () => {
            $("#lblStreamStatus").text("연결됨");
        });
        source.addEventListener("currentvalue", (event) => {
            try {
                const row = JSON.parse(event.data);
                this.applyUpdate(row);
            }
            catch (e) {
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
    applyUpdate(row) {
        const key = this.makeKey(row.endpointUrl, row.nodeId);
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
        }
        else {
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
    //private requestRender(): void {
    //    if (this.renderRequested) {
    //        return;
    //    }
    //    this.renderRequested = true;
    //    window.requestAnimationFrame(() => {
    //        this.renderRequested = false;
    //        this.renderVisibleRows();
    //    });
    //}
    requestRender() {
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
    renderVisibleRows() {
        const scrollEl = document.getElementById("currentValueScroll");
        const tbody = document.getElementById("currentValueBody");
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
        const html = [];
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
    renderRow(row) {
        var _a, _b, _c;
        const key = this.makeKey(row.endpointUrl, row.nodeId);
        const statusClass = this.isGoodStatus(row.status) ? "status-good" : "status-bad";
        const flashClass = this.flashKeys.has(key) ? "value-flash" : "";
        return `
        <tr style="height:${this.rowHeight}px">
            <td title="${this.escapeHtml(row.endpointUrl)}">${this.escapeHtml(row.endpointUrl)}</td>
            <td title="${this.escapeHtml(row.nodeId)}">${this.escapeHtml(row.nodeId)}</td>
            <td class="value-cell ${flashClass}" title="${this.escapeHtml((_a = row.value) !== null && _a !== void 0 ? _a : "")}">${this.escapeHtml((_b = row.value) !== null && _b !== void 0 ? _b : "")}</td>
            <td class="${statusClass}">${this.escapeHtml((_c = row.status) !== null && _c !== void 0 ? _c : "")}</td>
            <td>${this.formatDate(row.sourceTimestamp)}</td>
            <td>${this.formatDate(row.updatedAt)}</td>
        </tr>
    `;
    }
    isGoodStatus(status) {
        if (status == null || status === "") {
            return true;
        }
        const normalized = status.toLowerCase();
        return normalized.includes("good") ||
            normalized === "0" ||
            normalized === "0x00000000";
    }
    makeKey(endpointUrl, nodeId) {
        return `${endpointUrl}||${nodeId}`;
    }
    formatDate(value) {
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
/*!*************************************************!*\
  !*** ./Scripts/.generated/views__main__main.ts ***!
  \*************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_main_main__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/main/main */ "./Scripts/src/ts/views/main/main.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_main_main__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=main.js.map