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

/***/ "./WebFlex.UI/Scripts/src/ts/views/opc/opc1020.ts"
/*!********************************************************!*\
  !*** ./WebFlex.UI/Scripts/src/ts/views/opc/opc1020.ts ***!
  \********************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    init() {
        $("#btnLoad").on("click", () => this.load());
        $("#btnSave").on("click", () => this.save());
        this.load();
    }
    async load() {
        try {
            this.setStatus("불러오는 중...");
            const response = await fetch("/api/opc-collect-options");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            this.setForm(data);
            this.setStatus("불러오기 완료");
        }
        catch (e) {
            console.error(e);
            this.setStatus("불러오기 실패");
            alert("옵션을 불러오는 중 오류가 발생했습니다.");
        }
    }
    async save() {
        var _a, _b;
        try {
            if (!confirm("OPC Collector 옵션을 저장할까요? 저장 후 Windows Service 제어 화면에서 서비스를 재시작하면 적용됩니다.")) {
                return;
            }
            this.setStatus("저장 중...");
            const request = this.getForm();
            const response = await fetch("/api/opc-collect-options", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const result = await response.json();
            if (result.data != null) {
                this.setForm(result.data);
            }
            this.setStatus((_a = result.message) !== null && _a !== void 0 ? _a : "저장 완료");
            alert((_b = result.message) !== null && _b !== void 0 ? _b : "저장되었습니다.");
        }
        catch (e) {
            console.error(e);
            this.setStatus("저장 실패");
            alert("옵션 저장 중 오류가 발생했습니다.");
        }
    }
    setForm(data) {
        this.setChecked("enableAutoReload", data.enableAutoReload);
        this.setChecked("enableSnapshotSave", data.enableSnapshotSave);
        this.setChecked("enableTimescaleHistorySave", data.enableTimescaleHistorySave);
        this.setChecked("enableCurrentValueSave", data.enableCurrentValueSave);
        this.setNumber("reloadIntervalSeconds", data.reloadIntervalSeconds);
        this.setNumber("saveIntervalMilliseconds", data.saveIntervalMilliseconds);
        this.setNumber("maxBatchSize", data.maxBatchSize);
        this.setNumber("writerLogIntervalSeconds", data.writerLogIntervalSeconds);
        this.setNumber("defaultPublishingIntervalMs", data.defaultPublishingIntervalMs);
        this.setNumber("defaultSamplingIntervalMs", data.defaultSamplingIntervalMs);
        this.setNumber("defaultQueueSize", data.defaultQueueSize);
        this.setNumber("subscriptionKeepAliveCount", data.subscriptionKeepAliveCount);
        this.setNumber("subscriptionLifetimeCount", data.subscriptionLifetimeCount);
        this.setNumber("maxNotificationsPerPublish", data.maxNotificationsPerPublish);
        this.setNumber("subscriptionPriority", data.subscriptionPriority);
        this.setChecked("discardOldest", data.discardOldest);
        this.setChecked("autoAcceptUntrustedCertificates", data.autoAcceptUntrustedCertificates);
        this.setChecked("rejectSHA1SignedCertificates", data.rejectSHA1SignedCertificates);
        this.setNumber("minimumCertificateKeySize", data.minimumCertificateKeySize);
        this.setChecked("suppressNonceValidationErrors", data.suppressNonceValidationErrors);
        this.setText("certificateStoreRootPath", data.certificateStoreRootPath);
        this.setNumber("operationTimeoutMilliseconds", data.operationTimeoutMilliseconds);
        this.setNumber("defaultSessionTimeoutMilliseconds", data.defaultSessionTimeoutMilliseconds);
        this.setNumber("minSubscriptionLifetimeMilliseconds", data.minSubscriptionLifetimeMilliseconds);
        this.setNumber("maxStringLength", data.maxStringLength);
        this.setNumber("maxByteStringLength", data.maxByteStringLength);
        this.setNumber("maxArrayLength", data.maxArrayLength);
        this.setNumber("maxMessageSize", data.maxMessageSize);
        this.setNumber("maxBufferSize", data.maxBufferSize);
        this.setNumber("channelLifetime", data.channelLifetime);
        this.setNumber("securityTokenLifetime", data.securityTokenLifetime);
        this.setChecked("disableHiResClock", data.disableHiResClock);
        this.setChecked("defaultUseSecurity", data.defaultUseSecurity);
        this.setChecked("defaultUseAnonymous", data.defaultUseAnonymous);
        this.setText("defaultSecurityPolicy", data.defaultSecurityPolicy);
        this.setText("defaultSecurityMode", data.defaultSecurityMode);
    }
    getForm() {
        return {
            enableAutoReload: this.getChecked("enableAutoReload"),
            enableSnapshotSave: this.getChecked("enableSnapshotSave"),
            enableTimescaleHistorySave: this.getChecked("enableTimescaleHistorySave"),
            enableCurrentValueSave: this.getChecked("enableCurrentValueSave"),
            reloadIntervalSeconds: this.getNumber("reloadIntervalSeconds"),
            saveIntervalMilliseconds: this.getNumber("saveIntervalMilliseconds"),
            maxBatchSize: this.getNumber("maxBatchSize"),
            writerLogIntervalSeconds: this.getNumber("writerLogIntervalSeconds"),
            defaultPublishingIntervalMs: this.getNumber("defaultPublishingIntervalMs"),
            defaultSamplingIntervalMs: this.getNumber("defaultSamplingIntervalMs"),
            defaultQueueSize: this.getNumber("defaultQueueSize"),
            subscriptionKeepAliveCount: this.getNumber("subscriptionKeepAliveCount"),
            subscriptionLifetimeCount: this.getNumber("subscriptionLifetimeCount"),
            maxNotificationsPerPublish: this.getNumber("maxNotificationsPerPublish"),
            subscriptionPriority: this.getNumber("subscriptionPriority"),
            discardOldest: this.getChecked("discardOldest"),
            autoAcceptUntrustedCertificates: this.getChecked("autoAcceptUntrustedCertificates"),
            rejectSHA1SignedCertificates: this.getChecked("rejectSHA1SignedCertificates"),
            minimumCertificateKeySize: this.getNumber("minimumCertificateKeySize"),
            suppressNonceValidationErrors: this.getChecked("suppressNonceValidationErrors"),
            certificateStoreRootPath: this.getText("certificateStoreRootPath"),
            operationTimeoutMilliseconds: this.getNumber("operationTimeoutMilliseconds"),
            defaultSessionTimeoutMilliseconds: this.getNumber("defaultSessionTimeoutMilliseconds"),
            minSubscriptionLifetimeMilliseconds: this.getNumber("minSubscriptionLifetimeMilliseconds"),
            maxStringLength: this.getNumber("maxStringLength"),
            maxByteStringLength: this.getNumber("maxByteStringLength"),
            maxArrayLength: this.getNumber("maxArrayLength"),
            maxMessageSize: this.getNumber("maxMessageSize"),
            maxBufferSize: this.getNumber("maxBufferSize"),
            channelLifetime: this.getNumber("channelLifetime"),
            securityTokenLifetime: this.getNumber("securityTokenLifetime"),
            disableHiResClock: this.getChecked("disableHiResClock"),
            defaultUseSecurity: this.getChecked("defaultUseSecurity"),
            defaultUseAnonymous: this.getChecked("defaultUseAnonymous"),
            defaultSecurityPolicy: this.getText("defaultSecurityPolicy"),
            defaultSecurityMode: this.getText("defaultSecurityMode")
        };
    }
    getNumber(id) {
        const value = Number($(`#${id}`).val());
        if (Number.isNaN(value)) {
            return 0;
        }
        return value;
    }
    setNumber(id, value) {
        $(`#${id}`).val(value);
    }
    getText(id) {
        var _a;
        return String((_a = $(`#${id}`).val()) !== null && _a !== void 0 ? _a : "");
    }
    setText(id, value) {
        $(`#${id}`).val(value !== null && value !== void 0 ? value : "");
    }
    getChecked(id) {
        return Boolean($(`#${id}`).prop("checked"));
    }
    setChecked(id, value) {
        $(`#${id}`).prop("checked", value);
    }
    setStatus(message) {
        $("#lblStatus").text(message);
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
  !*** ./WebFlex.UI/Scripts/.generated/views__opc__opc1020.ts ***!
  \**************************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_opc_opc1020__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/opc/opc1020 */ "./WebFlex.UI/Scripts/src/ts/views/opc/opc1020.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./WebFlex.UI/Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_opc_opc1020__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=opc1020.js.map