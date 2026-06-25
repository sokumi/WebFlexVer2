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

/***/ "./Scripts/src/ts/views/opc/opc4000.ts"
/*!*********************************************!*\
  !*** ./Scripts/src/ts/views/opc/opc4000.ts ***!
  \*********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.tables = [];
        this.selectedFullName = "";
    }
    init() {
        $("#btnSearch").on("click", () => this.search());
        $("#btnApply").on("click", () => this.apply());
        this.search();
    }
    async search() {
        var _a;
        try {
            this.setStatus("조회 중...");
            const response = await fetch("/api/timescale-options/tables");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            this.tables = await response.json();
            this.renderTables();
            if (this.tables.length > 0) {
                const firstHypertable = (_a = this.tables.find(x => x.isHypertable)) !== null && _a !== void 0 ? _a : this.tables[0];
                this.selectTable(firstHypertable.fullName);
            }
            this.setStatus("조회 완료");
        }
        catch (e) {
            console.error(e);
            this.setStatus("조회 실패");
            $("#tableBody").html(`<tr><td colspan="9">조회 실패</td></tr>`);
            alert(e instanceof Error ? e.message : "TimescaleDB 옵션 조회 중 오류가 발생했습니다.");
        }
    }
    async apply() {
        var _a;
        try {
            const request = this.getApplyRequest();
            if (request.tableName === "") {
                alert("적용할 테이블을 선택해 주세요.");
                return;
            }
            const selected = this.tables.find(x => x.fullName === this.selectedFullName);
            if (selected == null || !selected.isHypertable) {
                alert("Hypertable에만 TimescaleDB 옵션을 적용할 수 있습니다.");
                return;
            }
            if (request.retentionEnabled && this.isEmpty(request.retentionDropAfter)) {
                alert("Retention 사용 시 RetentionDropAfter 값을 입력해 주세요.");
                return;
            }
            if (request.compressionEnabled && this.isEmpty(request.compressionOrderBy)) {
                request.compressionOrderBy = "time DESC";
            }
            const confirmMessage = [
                "TimescaleDB 옵션을 적용할까요?",
                "",
                `대상: ${request.schemaName}.${request.tableName}`,
                "",
                "주의:",
                "- Retention 설정은 오래된 데이터를 자동 삭제할 수 있습니다.",
                "- Chunk interval 변경은 새 chunk부터 적용됩니다.",
                "- Compression 설정은 TimescaleDB 버전에 따라 실패할 수 있습니다."
            ].join("\n");
            if (!confirm(confirmMessage)) {
                return;
            }
            this.setStatus("적용 중...");
            this.setLog("");
            const response = await fetch("/api/timescale-options/apply", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                throw new Error((_a = result.message) !== null && _a !== void 0 ? _a : "적용 실패");
            }
            this.setLog(result.logs.join("\n"));
            this.setStatus(result.message);
            alert(result.message);
            await this.search();
        }
        catch (e) {
            console.error(e);
            this.setStatus("적용 실패");
            this.setLog(e instanceof Error ? e.message : "TimescaleDB 옵션 적용 중 오류가 발생했습니다.");
            alert(e instanceof Error ? e.message : "TimescaleDB 옵션 적용 중 오류가 발생했습니다.");
        }
    }
    renderTables() {
        if (this.tables.length === 0) {
            $("#tableBody").html(`<tr><td colspan="9">조회된 테이블이 없습니다.</td></tr>`);
            return;
        }
        const html = this.tables.map(x => {
            var _a, _b, _c, _d, _e;
            return `
            <tr data-full-name="${this.escapeHtml(x.fullName)}">
                <td>
                    <button type="button" class="btnSelectTable btn-basic" data-full-name="${this.escapeHtml(x.fullName)}">선택</button>
                </td>
                <td>${this.escapeHtml(x.fullName)}</td>
                <td>${x.isHypertable ? "Y" : "N"}</td>
                <td>${this.escapeHtml((_a = x.timeColumnName) !== null && _a !== void 0 ? _a : "")}</td>
                <td>${this.escapeHtml((_b = x.chunkTimeInterval) !== null && _b !== void 0 ? _b : "")}</td>
                <td>${(_c = x.chunkCount) !== null && _c !== void 0 ? _c : ""}</td>
                <td>${this.escapeHtml((_d = x.totalSize) !== null && _d !== void 0 ? _d : "")}</td>
                <td>${x.retentionEnabled ? this.escapeHtml((_e = x.retentionDropAfter) !== null && _e !== void 0 ? _e : "Y") : "N"}</td>
                <td>${x.compressionEnabled ? "Y" : "N"}</td>
            </tr>
        `;
        });
        $("#tableBody").html(html.join(""));
        $(".btnSelectTable").on("click", (e) => {
            var _a;
            const fullName = String((_a = $(e.currentTarget).data("full-name")) !== null && _a !== void 0 ? _a : "");
            this.selectTable(fullName);
        });
    }
    selectTable(fullName) {
        var _a, _b, _c, _d, _e, _f, _g, _h, _j, _k, _l, _m;
        const table = this.tables.find(x => x.fullName === fullName);
        if (table == null) {
            return;
        }
        this.selectedFullName = fullName;
        $("#tableBody tr").removeClass("selected-row");
        $(`#tableBody tr[data-full-name="${this.escapeSelector(fullName)}"]`).addClass("selected-row");
        $("#schemaName").val(table.schemaName);
        $("#tableName").val(table.tableName);
        $("#isHypertable").prop("checked", table.isHypertable);
        $("#timeColumnName").val((_a = table.timeColumnName) !== null && _a !== void 0 ? _a : "");
        $("#currentChunkTimeInterval").val((_b = table.chunkTimeInterval) !== null && _b !== void 0 ? _b : "");
        $("#totalSize").val((_c = table.totalSize) !== null && _c !== void 0 ? _c : "");
        $("#tableSize").val((_d = table.tableSize) !== null && _d !== void 0 ? _d : "");
        $("#indexSize").val((_e = table.indexSize) !== null && _e !== void 0 ? _e : "");
        $("#chunkTimeInterval").val((_f = table.chunkTimeInterval) !== null && _f !== void 0 ? _f : "");
        $("#retentionEnabled").prop("checked", table.retentionEnabled);
        $("#retentionDropAfter").val((_g = table.retentionDropAfter) !== null && _g !== void 0 ? _g : "");
        $("#retentionScheduleInterval").val((_h = table.retentionScheduleInterval) !== null && _h !== void 0 ? _h : "");
        $("#compressionEnabled").prop("checked", table.compressionEnabled);
        $("#compressionAfter").val((_j = table.compressionAfter) !== null && _j !== void 0 ? _j : "");
        $("#compressionScheduleInterval").val((_k = table.compressionScheduleInterval) !== null && _k !== void 0 ? _k : "");
        $("#compressionSegmentBy").val((_l = table.compressionSegmentBy) !== null && _l !== void 0 ? _l : "");
        $("#compressionOrderBy").val((_m = table.compressionOrderBy) !== null && _m !== void 0 ? _m : "time DESC");
        if (!table.isHypertable) {
            this.setStatus("선택한 테이블은 hypertable이 아닙니다. 설정 적용 대상이 아닙니다.");
        }
        else {
            this.setStatus(`${table.fullName} 선택됨`);
        }
    }
    getApplyRequest() {
        return {
            schemaName: this.getText("schemaName"),
            tableName: this.getText("tableName"),
            chunkTimeInterval: this.getNullableText("chunkTimeInterval"),
            retentionEnabled: this.getChecked("retentionEnabled"),
            retentionDropAfter: this.getNullableText("retentionDropAfter"),
            retentionScheduleInterval: this.getNullableText("retentionScheduleInterval"),
            compressionEnabled: this.getChecked("compressionEnabled"),
            compressionAfter: this.getNullableText("compressionAfter"),
            compressionScheduleInterval: this.getNullableText("compressionScheduleInterval"),
            compressionSegmentBy: this.getNullableText("compressionSegmentBy"),
            compressionOrderBy: this.getNullableText("compressionOrderBy")
        };
    }
    getText(id) {
        var _a;
        return String((_a = $(`#${id}`).val()) !== null && _a !== void 0 ? _a : "").trim();
    }
    getNullableText(id) {
        const value = this.getText(id);
        return value === ""
            ? null
            : value;
    }
    getChecked(id) {
        return Boolean($(`#${id}`).prop("checked"));
    }
    isEmpty(value) {
        return value == null || value.trim() === "";
    }
    setStatus(message) {
        $("#lblStatus").text(message);
    }
    setLog(message) {
        $("#txtLog").val(message);
    }
    escapeHtml(value) {
        return value
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
    escapeSelector(value) {
        return value.replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
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
  !*** ./Scripts/.generated/views__opc__opc4000.ts ***!
  \***************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_opc_opc4000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/opc/opc4000 */ "./Scripts/src/ts/views/opc/opc4000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_opc_opc4000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=opc4000.js.map