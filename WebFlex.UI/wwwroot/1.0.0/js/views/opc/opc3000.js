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

/***/ "./WebFlex.UI/Scripts/src/ts/views/opc/opc3000.ts"
/*!********************************************************!*\
  !*** ./WebFlex.UI/Scripts/src/ts/views/opc/opc3000.ts ***!
  \********************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    init() {
        $("#btnSearch").on("click", () => this.search());
        this.setDefaultTimeRange();
    }
    setDefaultTimeRange() {
        const end = new Date();
        const start = new Date(end.getTime() - 60 * 60 * 1000);
        $("#startTime").val(this.toDateTimeLocalValue(start));
        $("#endTime").val(this.toDateTimeLocalValue(end));
    }
    async search() {
        var _a;
        try {
            const request = this.getRequest();
            if (request.endpointUrl === "") {
                alert("EndpointUrl을 입력해 주세요.");
                return;
            }
            if (request.nodeId === "") {
                alert("NodeId를 입력해 주세요.");
                return;
            }
            this.setStatus("조회 중...");
            $("#lblCount").text("0");
            $("#historyBody").html(`<tr><td colspan="4">조회 중입니다.</td></tr>`);
            const response = await fetch("/api/opc-collector/history/read", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                throw new Error((_a = result.message) !== null && _a !== void 0 ? _a : "History 조회 실패");
            }
            this.render(result.values);
            this.setStatus(result.message);
        }
        catch (e) {
            console.error(e);
            this.setStatus("조회 실패");
            $("#historyBody").html(`<tr><td colspan="4">조회 실패</td></tr>`);
            alert(e instanceof Error ? e.message : "History 조회 중 오류가 발생했습니다.");
        }
    }
    getRequest() {
        return {
            endpointUrl: this.getText("endpointUrl"),
            nodeId: this.getText("nodeId"),
            useSecurity: this.getChecked("useSecurity"),
            useAnonymous: this.getChecked("useAnonymous"),
            userName: this.getText("userName"),
            password: this.getText("password"),
            startTime: this.getText("startTime"),
            endTime: this.getText("endTime"),
            readMode: this.getText("readMode"),
            returnBounds: this.getChecked("returnBounds"),
            readModified: this.getChecked("readModified"),
            numValuesPerNode: this.getNumber("numValuesPerNode"),
            timestampsToReturn: this.getText("timestampsToReturn"),
            releaseContinuationPoints: this.getChecked("releaseContinuationPoints"),
            maxContinuationReads: this.getNumber("maxContinuationReads")
        };
    }
    render(values) {
        $("#lblCount").text(String(values.length));
        if (values.length === 0) {
            $("#historyBody").html(`<tr><td colspan="4">조회된 데이터가 없습니다.</td></tr>`);
            return;
        }
        const html = values.map(x => {
            var _a, _b;
            return `
            <tr>
                <td>${this.escapeHtml(this.formatDate(x.sourceTimestamp))}</td>
                <td>${this.escapeHtml(this.formatDate(x.serverTimestamp))}</td>
                <td>${this.escapeHtml((_a = x.value) !== null && _a !== void 0 ? _a : "")}</td>
                <td>${this.escapeHtml((_b = x.statusCode) !== null && _b !== void 0 ? _b : "")}</td>
            </tr>
        `;
        });
        $("#historyBody").html(html.join(""));
    }
    getText(id) {
        var _a;
        return String((_a = $(`#${id}`).val()) !== null && _a !== void 0 ? _a : "").trim();
    }
    getNumber(id) {
        const value = Number($(`#${id}`).val());
        if (Number.isNaN(value)) {
            return 0;
        }
        return value;
    }
    getChecked(id) {
        return Boolean($(`#${id}`).prop("checked"));
    }
    setStatus(message) {
        $("#lblStatus").text(message);
    }
    toDateTimeLocalValue(date) {
        const yyyy = date.getFullYear();
        const mm = String(date.getMonth() + 1).padStart(2, "0");
        const dd = String(date.getDate()).padStart(2, "0");
        const hh = String(date.getHours()).padStart(2, "0");
        const mi = String(date.getMinutes()).padStart(2, "0");
        return `${yyyy}-${mm}-${dd}T${hh}:${mi}`;
    }
    formatDate(value) {
        if (value == null || value === "") {
            return "";
        }
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
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
/*!**************************************************************!*\
  !*** ./WebFlex.UI/Scripts/.generated/views__opc__opc3000.ts ***!
  \**************************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_opc_opc3000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/opc/opc3000 */ "./WebFlex.UI/Scripts/src/ts/views/opc/opc3000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./WebFlex.UI/Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_opc_opc3000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=opc3000.js.map