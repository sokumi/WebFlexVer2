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

/***/ "./Scripts/src/ts/views/system/svc1000.ts"
/*!************************************************!*\
  !*** ./Scripts/src/ts/views/system/svc1000.ts ***!
  \************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
/* harmony import */ var _framework_notify__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ../../framework/notify */ "./Scripts/src/ts/framework/notify.ts");

class Page {
    constructor() {
        this.isAutoRefresh = true;
        this.isLogCollapsed = true;
        this.logCount = 0;
    }
    init() {
        $("#btnRefresh").on("click", () => {
            void this.loadStatus(true);
        });
        $("#btnInstall").on("click", () => {
            void this.installService();
        });
        $("#btnStart").on("click", () => {
            void this.post("/system/service/start", "서비스 시작 요청이 완료되었습니다.");
        });
        $("#btnStop").on("click", () => {
            void this.post("/system/service/stop", "서비스 중지 요청이 완료되었습니다.");
        });
        $("#btnRestart").on("click", () => {
            void this.restartService();
        });
        $("#btnUninstall").on("click", () => {
            void this.uninstallService();
        });
        $("#btnDeployZip").on("click", () => {
            void this.deployZip();
        });
        $("#btnToggleLog").on("click", () => {
            this.toggleLog();
        });
        this.setLogCollapsed(true);
        void this.loadStatus();
        window.setInterval(() => {
            if (this.isAutoRefresh) {
                void this.loadStatus();
            }
        }, 3000);
    }
    async loadStatus(showToast = false) {
        try {
            const response = await fetch("/system/service/status");
            const text = await response.text();
            if (!response.ok) {
                throw new Error(text);
            }
            const data = JSON.parse(text);
            this.renderStatus(data);
            this.setButtonState(data);
            this.writeResult(data);
            if (showToast) {
                _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.success("서비스 상태를 조회했습니다.");
            }
            if (data.status === "Error" && data.error) {
                console.error(data.error);
            }
        }
        catch (e) {
            console.error(e);
            this.renderErrorStatus();
            this.writeError(e);
            if (showToast) {
                _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.error("서비스 상태 조회에 실패했습니다.");
            }
        }
    }
    renderStatus(data) {
        const displayName = data.displayName || "WebFlex Collector Service";
        const serviceName = data.serviceName || "WebFlexCollector";
        const exePath = data.exePath || "-";
        const visualStatus = this.getVisualStatus(data);
        this.setTextWithFlash("#displayName", displayName);
        this.setTextWithFlash("#serviceName", serviceName);
        this.setTextWithFlash("#exePath", exePath);
        // PID/메모리는 이후 기능 추가 예정이라 우선 비워둔다.
        this.setTextWithFlash("#servicePid", "-");
        this.setTextWithFlash("#serviceMemory", "-");
        this.setTextWithFlash("#serviceStartType", "자동");
        $("#serviceStatus").text(this.getStatusText(data));
        $("#serviceHeroCard")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass(`is-${visualStatus}`);
        $("#serviceStatusBadge")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass(`is-${visualStatus}`);
    }
    renderErrorStatus() {
        $("#serviceStatus").text("조회 실패");
        $("#serviceHeroCard")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass("is-error");
        $("#serviceStatusBadge")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass("is-error");
    }
    async post(url, successMessage) {
        var _a, _b, _c;
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
                _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.error((_a = data.message) !== null && _a !== void 0 ? _a : "요청 처리에 실패했습니다.");
            }
            else {
                _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.success((_c = (_b = data.message) !== null && _b !== void 0 ? _b : successMessage) !== null && _c !== void 0 ? _c : "요청 처리 완료");
            }
            await this.loadStatus();
        }
        catch (e) {
            console.error(e);
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.error("요청 처리 중 오류가 발생했습니다.");
            this.writeError(e);
        }
    }
    async installService() {
        const ok = confirm("WebFlex OPC Collector 서비스를 등록할까요?");
        if (!ok) {
            return;
        }
        await this.post("/system/service/install", "서비스 등록이 완료되었습니다.");
    }
    async restartService() {
        const ok = confirm("WebFlex OPC Collector 서비스를 재시작할까요?");
        if (!ok) {
            return;
        }
        await this.post("/system/service/restart", "서비스 재시작 요청이 완료되었습니다.");
    }
    async uninstallService() {
        const ok = confirm("WebFlex OPC Collector 서비스를 삭제할까요?");
        if (!ok) {
            return;
        }
        await this.post("/system/service/uninstall", "서비스 삭제가 완료되었습니다.");
    }
    async deployZip() {
        var _a, _b;
        const input = document.getElementById("collectorZipFile");
        if (!input || !input.files || input.files.length === 0) {
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.warning("업로드할 ZIP 파일을 선택하세요.");
            return;
        }
        const file = input.files[0];
        if (!file.name.toLowerCase().endsWith(".zip")) {
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.warning("ZIP 파일만 업로드할 수 있습니다.");
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
            const text = await response.text();
            if (!response.ok) {
                throw new Error(text);
            }
            const data = JSON.parse(text);
            this.writeResult(data);
            if (!data.success) {
                _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.error((_a = data.message) !== null && _a !== void 0 ? _a : "배포에 실패했습니다.");
            }
            else {
                _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.success((_b = data.message) !== null && _b !== void 0 ? _b : "ZIP 배포가 완료되었습니다.");
            }
            await this.loadStatus();
        }
        catch (e) {
            console.error(e);
            _framework_notify__WEBPACK_IMPORTED_MODULE_0__.notify.error("ZIP 배포 중 오류가 발생했습니다.");
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
    toggleLog() {
        this.setLogCollapsed(!this.isLogCollapsed);
    }
    setLogCollapsed(isCollapsed) {
        this.isLogCollapsed = isCollapsed;
        $("#serviceLogCard").toggleClass("is-collapsed", isCollapsed);
        $("#serviceLogToggleText").text(isCollapsed ? "펼치기" : "접기");
        $("#btnToggleLog").attr("aria-expanded", String(!isCollapsed));
        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }
    writeResult(data) {
        const text = JSON.stringify(data, null, 2);
        $("#resultBox").text(text);
        this.updateLogCount(text);
    }
    writeError(error) {
        const text = error instanceof Error
            ? error.message
            : String(error);
        $("#resultBox").text(text);
        this.updateLogCount(text);
    }
    updateLogCount(text) {
        if (text.trim().length === 0) {
            this.logCount = 0;
        }
        else {
            this.logCount = text.split(/\r?\n/).filter(x => x.trim().length > 0).length;
        }
        $("#serviceLogCount").text(`${this.logCount.toLocaleString()}개 항목`);
    }
    getVisualStatus(data) {
        if (data.status === "Running") {
            return "running";
        }
        if (data.status === "Stopped") {
            return "stopped";
        }
        if (!data.exists || data.status === "NotInstalled") {
            return "not-installed";
        }
        if (data.status === "Error") {
            return "error";
        }
        return "unknown";
    }
    getStatusText(data) {
        if (!data.exists || data.status === "NotInstalled") {
            return "미등록";
        }
        if (data.status === "Running") {
            return "실행 중";
        }
        if (data.status === "Stopped") {
            return "중지됨";
        }
        if (data.status === "Error") {
            return "오류";
        }
        return data.status || "알 수 없음";
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
        const element = $el[0];
        if (element != null) {
            void element.offsetWidth;
        }
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
// This entry needs to be wrapped in an IIFE because it needs to be isolated against other entry modules.
(() => {
var __webpack_exports__ = {};
/*!******************************************************!*\
  !*** ./Scripts/.generated/views__system__svc1000.ts ***!
  \******************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_system_svc1000__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/system/svc1000 */ "./Scripts/src/ts/views/system/svc1000.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_system_svc1000__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

// This entry needs to be wrapped in an IIFE because it needs to be isolated against other entry modules.
(() => {
/*!**************************************************!*\
  !*** ./Scripts/src/css/views/system/svc1000.css ***!
  \**************************************************/
__webpack_require__.r(__webpack_exports__);
// extracted by mini-css-extract-plugin

})();

/******/ })()
;
//# sourceMappingURL=svc1000.js.map