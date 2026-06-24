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

/***/ "./Scripts/src/ts/views/system/svc1000.ts"
/*!************************************************!*\
  !*** ./Scripts/src/ts/views/system/svc1000.ts ***!
  \************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.isAutoRefresh = false;
    }
    init() {
        $("#btnRefresh").on("click", () => this.loadStatus());
        $("#btnInstall").on("click", () => this.installService());
        $("#btnStart").on("click", () => this.post("/system/service/start"));
        $("#btnStop").on("click", () => this.post("/system/service/stop"));
        $("#btnRestart").on("click", () => this.restartService());
        $("#btnUninstall").on("click", () => this.uninstallService());
        $("#btnDeployZip").on("click", () => this.deployZip());
        this.loadStatus();
        window.setInterval(() => {
            if (this.isAutoRefresh) {
                this.loadStatus();
            }
        }, 3000);
    }
    async loadStatus() {
        try {
            const response = await fetch("/system/service/status");
            const text = await response.text();
            if (!response.ok) {
                throw new Error(text);
            }
            const data = JSON.parse(text);
            this.setTextWithFlash("#serviceName", data.serviceName);
            this.setTextWithFlash("#displayName", data.displayName);
            this.setTextWithFlash("#serviceStatus", data.status);
            this.setTextWithFlash("#exePath", data.exePath);
            this.setButtonState(data);
            this.writeResult(data);
            if (data.status === "Error" && data.error) {
                console.error(data.error);
            }
        }
        catch (e) {
            console.error(e);
            this.setTextWithFlash("#serviceStatus", "조회 실패");
            this.writeError(e);
        }
    }
    async post(url) {
        var _a;
        try {
            const response = await fetch(url, {
                method: "POST"
            });
            const text = await response.text();
            if (!response.ok) {
                throw new Error(text);
            }
            const data = JSON.parse(text);
            this.writeResult(data);
            if (!data.success) {
                alert((_a = data.message) !== null && _a !== void 0 ? _a : "요청 처리에 실패했습니다.");
            }
            await this.loadStatus();
        }
        catch (e) {
            console.error(e);
            alert("요청 처리 중 오류가 발생했습니다.");
            this.writeError(e);
        }
    }
    async installService() {
        const ok = confirm("WebFlex OPC Collector 서비스를 등록할까요?");
        if (!ok) {
            return;
        }
        await this.post("/system/service/install");
    }
    async restartService() {
        const ok = confirm("WebFlex OPC Collector 서비스를 재시작할까요?");
        if (!ok) {
            return;
        }
        await this.post("/system/service/restart");
    }
    async uninstallService() {
        const ok = confirm("WebFlex OPC Collector 서비스를 삭제할까요?");
        if (!ok) {
            return;
        }
        await this.post("/system/service/uninstall");
    }
    async post(url) {
        var _a;
        try {
            const response = await fetch(url, {
                method: "POST"
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            this.writeResult(data);
            if (!data.success) {
                alert((_a = data.message) !== null && _a !== void 0 ? _a : "요청 처리에 실패했습니다.");
            }
            await this.loadStatus();
        }
        catch (e) {
            console.error(e);
            alert("요청 처리 중 오류가 발생했습니다.");
            this.writeError(e);
        }
    }
    async deployZip() {
        var _a;
        const input = document.getElementById("collectorZipFile");
        if (!input || !input.files || input.files.length === 0) {
            alert("업로드할 ZIP 파일을 선택하세요.");
            return;
        }
        const file = input.files[0];
        if (!file.name.toLowerCase().endsWith(".zip")) {
            alert("ZIP 파일만 업로드할 수 있습니다.");
            return;
        }
        const ok = confirm("서비스를 중지하고 ZIP 파일을 배포한 뒤 다시 시작할까요?");
        if (!ok) {
            return;
        }
        const formData = new FormData();
        formData.append("file", file);
        try {
            this.setUploading(true);
            const response = await fetch("/system/service/deploy-zip", {
                method: "POST",
                body: formData
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            this.writeResult(data);
            if (!data.success) {
                alert((_a = data.message) !== null && _a !== void 0 ? _a : "배포에 실패했습니다.");
            }
            await this.loadStatus();
        }
        catch (e) {
            console.error(e);
            alert("ZIP 배포 중 오류가 발생했습니다.");
            this.writeError(e);
        }
        finally {
            this.setUploading(false);
        }
    }
    setButtonState(data) {
        var _a;
        const exists = data.exists === true;
        const status = (_a = data.status) !== null && _a !== void 0 ? _a : "";
        const isRunning = status === "Running";
        const isStopped = status === "Stopped";
        const isNotInstalled = !exists || status === "NotInstalled";
        $("#btnInstall").prop("disabled", !isNotInstalled);
        $("#btnStart").prop("disabled", !exists || isRunning);
        $("#btnStop").prop("disabled", !exists || isStopped || isNotInstalled);
        $("#btnRestart").prop("disabled", !exists || isNotInstalled);
        $("#btnUninstall").prop("disabled", !exists || isRunning);
    }
    setUploading(isUploading) {
        $("#btnDeployZip").prop("disabled", isUploading);
        if (isUploading) {
            $("#btnDeployZip").text("배포 중...");
        }
        else {
            $("#btnDeployZip").text("ZIP 업로드 후 배포/재시작");
        }
    }
    writeResult(data) {
        $("#resultBox").text(JSON.stringify(data, null, 2));
    }
    writeError(error) {
        if (error instanceof Error) {
            $("#resultBox").text(error.message);
            return;
        }
        $("#resultBox").text(String(error));
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
/*!******************************************************!*\
  !*** ./Scripts/.generated/views__system__svc1000.ts ***!
  \******************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_system_svc1000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/system/svc1000 */ "./Scripts/src/ts/views/system/svc1000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_system_svc1000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=svc1000.js.map