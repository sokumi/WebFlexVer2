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

/***/ "./Scripts/src/ts/views/device/dvc1000.ts"
/*!************************************************!*\
  !*** ./Scripts/src/ts/views/device/dvc1000.ts ***!
  \************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (__WEBPACK_DEFAULT_EXPORT__)
/* harmony export */ });
/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (class {
    constructor() {
        this.rows = [];
        this.selectedId = 0;
        this.btnNew_onClick = () => {
            this.clearForm();
        };
        this.btnSearch_onClick = () => {
            this.load();
        };
        this.btnSave_onClick = async () => {
            var _a, _b;
            const data = this.getFormData();
            try {
                const res = await this.post("/device/save", data);
                if (!res.success) {
                    alert((_a = res.message) !== null && _a !== void 0 ? _a : "저장에 실패했습니다.");
                    return;
                }
                alert((_b = res.message) !== null && _b !== void 0 ? _b : "저장되었습니다.");
                await this.load();
                this.clearForm();
            }
            catch (e) {
                alert(e instanceof Error ? e.message : "저장 중 오류가 발생했습니다.");
            }
        };
        this.btnDelete_onClick = async () => {
            var _a, _b;
            if (!this.selectedId) {
                alert("삭제할 디바이스를 선택하세요.");
                return;
            }
            if (!confirm("선택한 디바이스를 삭제하시겠습니까?")) {
                return;
            }
            try {
                const res = await this.post("/device/delete", this.selectedId);
                if (!res.success) {
                    alert((_a = res.message) !== null && _a !== void 0 ? _a : "삭제에 실패했습니다.");
                    return;
                }
                alert((_b = res.message) !== null && _b !== void 0 ? _b : "삭제되었습니다.");
                await this.load();
                this.clearForm();
            }
            catch (e) {
                alert(e instanceof Error ? e.message : "삭제 중 오류가 발생했습니다.");
            }
        };
        this.grid1_onClick = (row) => {
            var _a, _b, _c;
            this.selectedId = row.id;
            $("#hidId").val(row.id);
            $("#txtDeviceCode").val(row.deviceCode);
            $("#txtDeviceName").val(row.deviceName);
            $("#selDeviceType").val(row.deviceType);
            $("#txtDeviceAddress").val(row.deviceAddress);
            $("#txtPort").val(row.port);
            $("#txtEndpointUrl").val(row.endpointUrl);
            $("#chkCollect").prop("checked", row.isCollectEnabled);
            $("#chkEnabled").prop("checked", row.isEnabled);
            $("#chkUseSecurity").prop("checked", row.useSecurity);
            $("#chkUseAnonymous").prop("checked", row.useAnonymous);
            $("#txtUserName").val((_a = row.userName) !== null && _a !== void 0 ? _a : "");
            $("#txtPassword").val((_b = row.password) !== null && _b !== void 0 ? _b : "");
            $("#txtPublishingIntervalMs").val(row.publishingIntervalMs);
            $("#txtSamplingIntervalMs").val(row.samplingIntervalMs);
            $("#txtQueueSize").val(row.queueSize);
            $("#txtSortOrder").val(row.sortOrder);
            $("#txtDescription").val((_c = row.description) !== null && _c !== void 0 ? _c : "");
        };
    }
    init() {
        $("#btnNew").on("click", this.btnNew_onClick);
        $("#btnSave").on("click", this.btnSave_onClick);
        $("#btnDelete").on("click", this.btnDelete_onClick);
        $("#btnSearch").on("click", this.btnSearch_onClick);
        this.load();
    }
    async load() {
        var _a;
        try {
            const res = await this.get("/device/list");
            this.rows = (_a = res.data) !== null && _a !== void 0 ? _a : [];
            this.renderGrid();
        }
        catch (e) {
            alert(e instanceof Error ? e.message : "조회 중 오류가 발생했습니다.");
        }
    }
    renderGrid() {
        const $body = $("#grid1Body");
        $body.empty();
        for (const row of this.rows) {
            const $tr = $(`
                <tr>
                    <td>${row.deviceCode}</td>
                    <td>${row.deviceName}</td>
                    <td>${row.deviceType}</td>
                    <td>${row.deviceAddress}</td>
                    <td>${row.port}</td>
                    <td>${row.isCollectEnabled ? "Y" : "N"}</td>
                    <td>${row.isEnabled ? "Y" : "N"}</td>
                </tr>
            `);
            $tr.on("click", () => this.grid1_onClick(row));
            $body.append($tr);
        }
    }
    clearForm() {
        this.selectedId = 0;
        $("#hidId").val("");
        $("#txtDeviceCode").val("");
        $("#txtDeviceName").val("");
        $("#selDeviceType").val("OPCUA");
        $("#txtDeviceAddress").val("");
        $("#txtPort").val(4840);
        $("#txtEndpointUrl").val("");
        $("#chkCollect").prop("checked", true);
        $("#chkEnabled").prop("checked", true);
        $("#chkUseSecurity").prop("checked", false);
        $("#chkUseAnonymous").prop("checked", true);
        $("#txtUserName").val("");
        $("#txtPassword").val("");
        $("#txtPublishingIntervalMs").val(1000);
        $("#txtSamplingIntervalMs").val(1000);
        $("#txtQueueSize").val(100);
        $("#txtSortOrder").val(0);
        $("#txtDescription").val("");
    }
    getFormData() {
        var _a, _b, _c, _d, _e, _f, _g, _h, _j, _k, _l, _m;
        return {
            id: this.selectedId || null,
            deviceName: String((_a = $("#txtDeviceName").val()) !== null && _a !== void 0 ? _a : ""),
            deviceType: String((_b = $("#selDeviceType").val()) !== null && _b !== void 0 ? _b : "OPCUA"),
            deviceAddress: String((_c = $("#txtDeviceAddress").val()) !== null && _c !== void 0 ? _c : ""),
            port: Number((_d = $("#txtPort").val()) !== null && _d !== void 0 ? _d : 0),
            endpointUrl: String((_e = $("#txtEndpointUrl").val()) !== null && _e !== void 0 ? _e : ""),
            isCollectEnabled: $("#chkCollect").prop("checked") === true,
            isEnabled: $("#chkEnabled").prop("checked") === true,
            useSecurity: $("#chkUseSecurity").prop("checked") === true,
            useAnonymous: $("#chkUseAnonymous").prop("checked") === true,
            userName: String((_f = $("#txtUserName").val()) !== null && _f !== void 0 ? _f : ""),
            password: String((_g = $("#txtPassword").val()) !== null && _g !== void 0 ? _g : ""),
            publishingIntervalMs: Number((_h = $("#txtPublishingIntervalMs").val()) !== null && _h !== void 0 ? _h : 1000),
            samplingIntervalMs: Number((_j = $("#txtSamplingIntervalMs").val()) !== null && _j !== void 0 ? _j : 1000),
            queueSize: Number((_k = $("#txtQueueSize").val()) !== null && _k !== void 0 ? _k : 100),
            sortOrder: Number((_l = $("#txtSortOrder").val()) !== null && _l !== void 0 ? _l : 0),
            description: String((_m = $("#txtDescription").val()) !== null && _m !== void 0 ? _m : "")
        };
    }
    async get(url) {
        return await $.ajax({
            url,
            method: "GET",
            dataType: "json"
        });
    }
    async post(url, data) {
        return await $.ajax({
            url,
            method: "POST",
            data: JSON.stringify(data),
            contentType: "application/json; charset=utf-8",
            dataType: "json"
        });
    }
});
__webpack_require__.dn(__WEBPACK_DEFAULT_EXPORT__);


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
/******/ 	/* webpack/runtime/set anonymous default export name */
/******/ 	(() => {
/******/ 		// set .name for anonymous default exports per ES spec
/******/ 		__webpack_require__.dn = (x) => {
/******/ 			(Object.getOwnPropertyDescriptor(x, "name") || {}).writable || Object.defineProperty(x, "name", { value: "default", configurable: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
var __webpack_exports__ = {};
// This entry needs to be wrapped in an IIFE because it needs to be isolated against other modules in the chunk.
(() => {
/*!******************************************************!*\
  !*** ./Scripts/.generated/views__device__dvc1000.ts ***!
  \******************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_device_dvc1000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/device/dvc1000 */ "./Scripts/src/ts/views/device/dvc1000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_device_dvc1000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=dvc1000.js.map