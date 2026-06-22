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

/***/ "./Scripts/src/ts/views/opc/opc1030.ts"
/*!*********************************************!*\
  !*** ./Scripts/src/ts/views/opc/opc1030.ts ***!
  \*********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (/* binding */ Page)
/* harmony export */ });
class Page {
    constructor() {
        this.optionNames = [
            "applicationName",
            "applicationUri",
            "productUri",
            "applicationType",
            "disableHiResClock",
            "applicationCertificateStoreType",
            "applicationCertificateStorePath",
            "applicationCertificateSubjectName",
            "applicationCertificateThumbprint",
            "trustedPeerCertificatesStoreType",
            "trustedPeerCertificatesStorePath",
            "trustedIssuerCertificatesStoreType",
            "trustedIssuerCertificatesStorePath",
            "rejectedCertificateStoreType",
            "rejectedCertificateStorePath",
            "autoAcceptUntrustedCertificates",
            "rejectSHA1SignedCertificates",
            "rejectUnknownRevocationStatus",
            "minimumCertificateKeySize",
            "addAppCertToTrustedStore",
            "suppressNonceValidationErrors",
            "sendCertificateChain",
            "operationTimeout",
            "maxStringLength",
            "maxByteStringLength",
            "maxArrayLength",
            "maxMessageSize",
            "maxBufferSize",
            "channelLifetime",
            "securityTokenLifetime",
            "defaultSessionTimeout",
            "minSubscriptionLifetime",
            "wellKnownDiscoveryUrls",
            "discoveryServers",
            "endpointCacheFilePath",
            "endpointUrl",
            "useSecurity",
            "securityPolicyUri",
            "messageSecurityMode",
            "transportProfileUri",
            "endpointSelectionTimeout",
            "identityType",
            "userName",
            "password",
            "certificateUserStoreType",
            "certificateUserStorePath",
            "certificateUserSubjectName",
            "sessionName",
            "sessionTimeout",
            "updateBeforeConnect",
            "checkDomain",
            "preferredLocales",
            "publishingInterval",
            "lifetimeCount",
            "keepAliveCount",
            "maxNotificationsPerPublish",
            "publishingEnabled",
            "priority",
            "attributeId",
            "monitoringMode",
            "samplingInterval",
            "queueSize",
            "discardOldest",
            "deadbandType",
            "deadbandValue",
            "dataChangeTrigger",
            "browseNodeClassMask",
            "browseResultMask",
            "browseMaxReferencesToReturn",
            "readMaxAge",
            "readTimestampsToReturn",
            "enableSessionKeepAlive",
            "keepAliveInterval",
            "reconnectPeriod",
            "maxReconnectAttempts",
            "historyReadMode",
            "historyReturnBounds",
            "historyReadModified",
            "historyNumValuesPerNode",
            "historyTimestampsToReturn",
            "historyReleaseContinuationPoints",
            "historyMaxContinuationReads",
            "historyDefaultRangeMinutes"
        ];
    }
    init() {
        $("#btnSave").on("click", () => this.save());
        $("#btnRestartService").on("click", () => this.restartService());
        this.load();
    }
    async load() {
        try {
            this.setStatus("옵션 조회 중...");
            const response = await fetch("/api/opc-client-options");
            if (!response.ok) {
                throw new Error(await response.text());
            }
            const data = await response.json();
            this.setForm(data.options);
            this.applyConfiguredStyle(data);
            this.setStatus(data.hasSavedOptions
                ? "DB 저장 옵션을 불러왔습니다."
                : "DB 저장 옵션이 없어 기본 옵션을 표시합니다.");
        }
        catch (e) {
            console.error(e);
            this.setStatus("옵션 조회 실패");
            alert("OPC Client 옵션 조회 중 오류가 발생했습니다.");
        }
    }
    async save() {
        try {
            if (!confirm("OPC Client 옵션을 DB에 저장할까요? 저장 후 서비스 재시작 시 적용됩니다.")) {
                return;
            }
            this.setStatus("저장 중...");
            const request = {
                options: this.getForm(),
                configuredOptionNames: this.optionNames.map(x => this.toPascalCase(x)),
                usedOptionNames: [],
                hasSavedOptions: true
            };
            const response = await fetch("/api/opc-client-options", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            this.setStatus("저장 완료. 서비스 재시작 후 적용됩니다.");
            alert("저장되었습니다. 서비스 재시작 후 적용됩니다.");
            await this.load();
        }
        catch (e) {
            console.error(e);
            this.setStatus("저장 실패");
            alert("OPC Client 옵션 저장 중 오류가 발생했습니다.");
        }
    }
    async restartService() {
        try {
            if (!confirm("OPC Collector 서비스를 재시작할까요? 저장된 OPC Client 옵션은 재시작 후 적용됩니다.")) {
                return;
            }
            this.setStatus("서비스 재시작 요청 중...");
            const response = await fetch("/api/opc-collector/restart-process", {
                method: "POST"
            });
            if (!response.ok) {
                throw new Error(await response.text());
            }
            this.setStatus("서비스 재시작 요청 완료");
            alert("서비스 재시작 요청이 완료되었습니다.");
        }
        catch (e) {
            console.error(e);
            this.setStatus("서비스 재시작 요청 실패");
            alert("서비스 재시작 요청 중 오류가 발생했습니다.");
        }
    }
    setForm(data) {
        for (const name of this.optionNames) {
            const element = $(`#${name}`);
            if (element.length === 0) {
                continue;
            }
            const value = data[name];
            if (element.attr("type") === "checkbox") {
                element.prop("checked", Boolean(value));
                continue;
            }
            element.val(value);
        }
    }
    getForm() {
        var _a;
        const result = {};
        for (const name of this.optionNames) {
            const element = $(`#${name}`);
            if (element.length === 0) {
                continue;
            }
            if (element.attr("type") === "checkbox") {
                result[name] = Boolean(element.prop("checked"));
                continue;
            }
            if (element.attr("type") === "number") {
                const numberValue = Number(element.val());
                result[name] = Number.isNaN(numberValue)
                    ? 0
                    : numberValue;
                continue;
            }
            result[name] = String((_a = element.val()) !== null && _a !== void 0 ? _a : "");
        }
        return result;
    }
    applyConfiguredStyle(data) {
        var _a, _b;
        $(".opc-option-used").removeClass("opc-option-used");
        const names = new Set();
        for (const name of (_a = data.usedOptionNames) !== null && _a !== void 0 ? _a : []) {
            names.add(this.toCamelCase(name));
        }
        for (const name of (_b = data.configuredOptionNames) !== null && _b !== void 0 ? _b : []) {
            names.add(this.toCamelCase(name));
        }
        for (const name of names) {
            const element = $(`#${name}`);
            if (element.length === 0) {
                continue;
            }
            element.closest("tr").find("th").addClass("opc-option-used");
        }
    }
    toPascalCase(value) {
        if (value.length === 0) {
            return value;
        }
        return value.charAt(0).toUpperCase() + value.slice(1);
    }
    toCamelCase(value) {
        if (value.length === 0) {
            return value;
        }
        return value.charAt(0).toLowerCase() + value.slice(1);
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
/*!***************************************************!*\
  !*** ./Scripts/.generated/views__opc__opc1030.ts ***!
  \***************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_opc_opc1030__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/opc/opc1030 */ "./Scripts/src/ts/views/opc/opc1030.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_opc_opc1030__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=opc1030.js.map