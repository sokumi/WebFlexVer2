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
        this.storageKey = "webflex.opc1030.clientOptions";
    }
    init() {
        $("#btnLoadDefaults").on("click", () => this.loadDefaults());
        $("#btnLoadSaved").on("click", () => this.loadSaved());
        $("#btnSaveLocal").on("click", () => this.saveLocal());
        $("#btnClearLocal").on("click", () => this.clearLocal());
        this.loadSavedOrDefaults();
    }
    loadSavedOrDefaults() {
        const saved = this.getSaved();
        if (saved != null) {
            this.setForm(saved);
            this.setStatus("저장된 임시 옵션을 불러왔습니다.");
            return;
        }
        this.setForm(this.getDefaultOptions());
        this.setStatus("기본값을 불러왔습니다.");
    }
    loadDefaults() {
        this.setForm(this.getDefaultOptions());
        this.setStatus("기본값을 불러왔습니다.");
    }
    loadSaved() {
        const saved = this.getSaved();
        if (saved == null) {
            alert("임시 저장된 옵션이 없습니다.");
            return;
        }
        this.setForm(saved);
        this.setStatus("저장된 임시 옵션을 불러왔습니다.");
    }
    saveLocal() {
        const data = this.getForm();
        localStorage.setItem(this.storageKey, JSON.stringify(data));
        this.setStatus("브라우저 localStorage에 임시 저장했습니다. Collector에는 적용되지 않았습니다.");
        alert("임시 저장되었습니다. 아직 OPC Collector 서비스에는 적용되지 않습니다.");
    }
    clearLocal() {
        if (!confirm("임시 저장된 OPC Client 옵션을 삭제할까요?")) {
            return;
        }
        localStorage.removeItem(this.storageKey);
        this.setStatus("임시 저장값을 삭제했습니다.");
    }
    getSaved() {
        const raw = localStorage.getItem(this.storageKey);
        if (raw == null || raw === "") {
            return null;
        }
        try {
            return JSON.parse(raw);
        }
        catch {
            return null;
        }
    }
    getDefaultOptions() {
        return {
            applicationName: "WebFlexOpcCollector",
            applicationUri: "urn:localhost:WebFlexOpcCollector",
            productUri: "WebFlexOpcCollector",
            applicationType: "Client",
            disableHiResClock: true,
            applicationCertificateStoreType: "Directory",
            applicationCertificateStorePath: "pki/own",
            applicationCertificateSubjectName: "WebFlexOpcCollector",
            applicationCertificateThumbprint: "",
            trustedPeerCertificatesStoreType: "Directory",
            trustedPeerCertificatesStorePath: "pki/trusted",
            trustedIssuerCertificatesStoreType: "Directory",
            trustedIssuerCertificatesStorePath: "pki/issuer",
            rejectedCertificateStoreType: "Directory",
            rejectedCertificateStorePath: "pki/rejected",
            autoAcceptUntrustedCertificates: true,
            rejectSHA1SignedCertificates: false,
            rejectUnknownRevocationStatus: false,
            minimumCertificateKeySize: 1024,
            addAppCertToTrustedStore: false,
            suppressNonceValidationErrors: true,
            sendCertificateChain: false,
            operationTimeout: 600000,
            maxStringLength: 2147483647,
            maxByteStringLength: 2147483647,
            maxArrayLength: 65535,
            maxMessageSize: 419430400,
            maxBufferSize: 65535,
            channelLifetime: -1,
            securityTokenLifetime: -1,
            defaultSessionTimeout: 60000,
            minSubscriptionLifetime: 10000,
            wellKnownDiscoveryUrls: "",
            discoveryServers: "",
            endpointCacheFilePath: "",
            endpointUrl: "opc.tcp://127.0.0.1:49320",
            useSecurity: false,
            securityPolicyUri: "",
            messageSecurityMode: "",
            transportProfileUri: "",
            endpointSelectionTimeout: 15000,
            identityType: "Anonymous",
            userName: "",
            password: "",
            certificateUserStoreType: "Directory",
            certificateUserStorePath: "pki/user",
            certificateUserSubjectName: "",
            sessionName: "WebFlexOpcCollector",
            sessionTimeout: 60000,
            updateBeforeConnect: false,
            checkDomain: false,
            preferredLocales: "ko-KR,en-US",
            publishingInterval: 1000,
            lifetimeCount: 0,
            keepAliveCount: 0,
            maxNotificationsPerPublish: 0,
            publishingEnabled: true,
            priority: 100,
            attributeId: "Value",
            monitoringMode: "Reporting",
            samplingInterval: 1000,
            queueSize: 1,
            discardOldest: true,
            deadbandType: "None",
            deadbandValue: 0,
            dataChangeTrigger: "StatusValue",
            browseNodeClassMask: "Object,Variable",
            browseResultMask: "All",
            browseMaxReferencesToReturn: 0,
            readMaxAge: 0,
            readTimestampsToReturn: "Both",
            enableSessionKeepAlive: true,
            keepAliveInterval: 5000,
            reconnectPeriod: 10000,
            maxReconnectAttempts: -1
        };
    }
    setForm(data) {
        this.setText("applicationName", data.applicationName);
        this.setText("applicationUri", data.applicationUri);
        this.setText("productUri", data.productUri);
        this.setText("applicationType", data.applicationType);
        this.setChecked("disableHiResClock", data.disableHiResClock);
        this.setText("applicationCertificateStoreType", data.applicationCertificateStoreType);
        this.setText("applicationCertificateStorePath", data.applicationCertificateStorePath);
        this.setText("applicationCertificateSubjectName", data.applicationCertificateSubjectName);
        this.setText("applicationCertificateThumbprint", data.applicationCertificateThumbprint);
        this.setText("trustedPeerCertificatesStoreType", data.trustedPeerCertificatesStoreType);
        this.setText("trustedPeerCertificatesStorePath", data.trustedPeerCertificatesStorePath);
        this.setText("trustedIssuerCertificatesStoreType", data.trustedIssuerCertificatesStoreType);
        this.setText("trustedIssuerCertificatesStorePath", data.trustedIssuerCertificatesStorePath);
        this.setText("rejectedCertificateStoreType", data.rejectedCertificateStoreType);
        this.setText("rejectedCertificateStorePath", data.rejectedCertificateStorePath);
        this.setChecked("autoAcceptUntrustedCertificates", data.autoAcceptUntrustedCertificates);
        this.setChecked("rejectSHA1SignedCertificates", data.rejectSHA1SignedCertificates);
        this.setChecked("rejectUnknownRevocationStatus", data.rejectUnknownRevocationStatus);
        this.setNumber("minimumCertificateKeySize", data.minimumCertificateKeySize);
        this.setChecked("addAppCertToTrustedStore", data.addAppCertToTrustedStore);
        this.setChecked("suppressNonceValidationErrors", data.suppressNonceValidationErrors);
        this.setChecked("sendCertificateChain", data.sendCertificateChain);
        this.setNumber("operationTimeout", data.operationTimeout);
        this.setNumber("maxStringLength", data.maxStringLength);
        this.setNumber("maxByteStringLength", data.maxByteStringLength);
        this.setNumber("maxArrayLength", data.maxArrayLength);
        this.setNumber("maxMessageSize", data.maxMessageSize);
        this.setNumber("maxBufferSize", data.maxBufferSize);
        this.setNumber("channelLifetime", data.channelLifetime);
        this.setNumber("securityTokenLifetime", data.securityTokenLifetime);
        this.setNumber("defaultSessionTimeout", data.defaultSessionTimeout);
        this.setNumber("minSubscriptionLifetime", data.minSubscriptionLifetime);
        this.setText("wellKnownDiscoveryUrls", data.wellKnownDiscoveryUrls);
        this.setText("discoveryServers", data.discoveryServers);
        this.setText("endpointCacheFilePath", data.endpointCacheFilePath);
        this.setText("endpointUrl", data.endpointUrl);
        this.setChecked("useSecurity", data.useSecurity);
        this.setText("securityPolicyUri", data.securityPolicyUri);
        this.setText("messageSecurityMode", data.messageSecurityMode);
        this.setText("transportProfileUri", data.transportProfileUri);
        this.setNumber("endpointSelectionTimeout", data.endpointSelectionTimeout);
        this.setText("identityType", data.identityType);
        this.setText("userName", data.userName);
        this.setText("password", data.password);
        this.setText("certificateUserStoreType", data.certificateUserStoreType);
        this.setText("certificateUserStorePath", data.certificateUserStorePath);
        this.setText("certificateUserSubjectName", data.certificateUserSubjectName);
        this.setText("sessionName", data.sessionName);
        this.setNumber("sessionTimeout", data.sessionTimeout);
        this.setChecked("updateBeforeConnect", data.updateBeforeConnect);
        this.setChecked("checkDomain", data.checkDomain);
        this.setText("preferredLocales", data.preferredLocales);
        this.setNumber("publishingInterval", data.publishingInterval);
        this.setNumber("lifetimeCount", data.lifetimeCount);
        this.setNumber("keepAliveCount", data.keepAliveCount);
        this.setNumber("maxNotificationsPerPublish", data.maxNotificationsPerPublish);
        this.setChecked("publishingEnabled", data.publishingEnabled);
        this.setNumber("priority", data.priority);
        this.setText("attributeId", data.attributeId);
        this.setText("monitoringMode", data.monitoringMode);
        this.setNumber("samplingInterval", data.samplingInterval);
        this.setNumber("queueSize", data.queueSize);
        this.setChecked("discardOldest", data.discardOldest);
        this.setText("deadbandType", data.deadbandType);
        this.setNumber("deadbandValue", data.deadbandValue);
        this.setText("dataChangeTrigger", data.dataChangeTrigger);
        this.setText("browseNodeClassMask", data.browseNodeClassMask);
        this.setText("browseResultMask", data.browseResultMask);
        this.setNumber("browseMaxReferencesToReturn", data.browseMaxReferencesToReturn);
        this.setNumber("readMaxAge", data.readMaxAge);
        this.setText("readTimestampsToReturn", data.readTimestampsToReturn);
        this.setChecked("enableSessionKeepAlive", data.enableSessionKeepAlive);
        this.setNumber("keepAliveInterval", data.keepAliveInterval);
        this.setNumber("reconnectPeriod", data.reconnectPeriod);
        this.setNumber("maxReconnectAttempts", data.maxReconnectAttempts);
    }
    getForm() {
        return {
            applicationName: this.getText("applicationName"),
            applicationUri: this.getText("applicationUri"),
            productUri: this.getText("productUri"),
            applicationType: this.getText("applicationType"),
            disableHiResClock: this.getChecked("disableHiResClock"),
            applicationCertificateStoreType: this.getText("applicationCertificateStoreType"),
            applicationCertificateStorePath: this.getText("applicationCertificateStorePath"),
            applicationCertificateSubjectName: this.getText("applicationCertificateSubjectName"),
            applicationCertificateThumbprint: this.getText("applicationCertificateThumbprint"),
            trustedPeerCertificatesStoreType: this.getText("trustedPeerCertificatesStoreType"),
            trustedPeerCertificatesStorePath: this.getText("trustedPeerCertificatesStorePath"),
            trustedIssuerCertificatesStoreType: this.getText("trustedIssuerCertificatesStoreType"),
            trustedIssuerCertificatesStorePath: this.getText("trustedIssuerCertificatesStorePath"),
            rejectedCertificateStoreType: this.getText("rejectedCertificateStoreType"),
            rejectedCertificateStorePath: this.getText("rejectedCertificateStorePath"),
            autoAcceptUntrustedCertificates: this.getChecked("autoAcceptUntrustedCertificates"),
            rejectSHA1SignedCertificates: this.getChecked("rejectSHA1SignedCertificates"),
            rejectUnknownRevocationStatus: this.getChecked("rejectUnknownRevocationStatus"),
            minimumCertificateKeySize: this.getNumber("minimumCertificateKeySize"),
            addAppCertToTrustedStore: this.getChecked("addAppCertToTrustedStore"),
            suppressNonceValidationErrors: this.getChecked("suppressNonceValidationErrors"),
            sendCertificateChain: this.getChecked("sendCertificateChain"),
            operationTimeout: this.getNumber("operationTimeout"),
            maxStringLength: this.getNumber("maxStringLength"),
            maxByteStringLength: this.getNumber("maxByteStringLength"),
            maxArrayLength: this.getNumber("maxArrayLength"),
            maxMessageSize: this.getNumber("maxMessageSize"),
            maxBufferSize: this.getNumber("maxBufferSize"),
            channelLifetime: this.getNumber("channelLifetime"),
            securityTokenLifetime: this.getNumber("securityTokenLifetime"),
            defaultSessionTimeout: this.getNumber("defaultSessionTimeout"),
            minSubscriptionLifetime: this.getNumber("minSubscriptionLifetime"),
            wellKnownDiscoveryUrls: this.getText("wellKnownDiscoveryUrls"),
            discoveryServers: this.getText("discoveryServers"),
            endpointCacheFilePath: this.getText("endpointCacheFilePath"),
            endpointUrl: this.getText("endpointUrl"),
            useSecurity: this.getChecked("useSecurity"),
            securityPolicyUri: this.getText("securityPolicyUri"),
            messageSecurityMode: this.getText("messageSecurityMode"),
            transportProfileUri: this.getText("transportProfileUri"),
            endpointSelectionTimeout: this.getNumber("endpointSelectionTimeout"),
            identityType: this.getText("identityType"),
            userName: this.getText("userName"),
            password: this.getText("password"),
            certificateUserStoreType: this.getText("certificateUserStoreType"),
            certificateUserStorePath: this.getText("certificateUserStorePath"),
            certificateUserSubjectName: this.getText("certificateUserSubjectName"),
            sessionName: this.getText("sessionName"),
            sessionTimeout: this.getNumber("sessionTimeout"),
            updateBeforeConnect: this.getChecked("updateBeforeConnect"),
            checkDomain: this.getChecked("checkDomain"),
            preferredLocales: this.getText("preferredLocales"),
            publishingInterval: this.getNumber("publishingInterval"),
            lifetimeCount: this.getNumber("lifetimeCount"),
            keepAliveCount: this.getNumber("keepAliveCount"),
            maxNotificationsPerPublish: this.getNumber("maxNotificationsPerPublish"),
            publishingEnabled: this.getChecked("publishingEnabled"),
            priority: this.getNumber("priority"),
            attributeId: this.getText("attributeId"),
            monitoringMode: this.getText("monitoringMode"),
            samplingInterval: this.getNumber("samplingInterval"),
            queueSize: this.getNumber("queueSize"),
            discardOldest: this.getChecked("discardOldest"),
            deadbandType: this.getText("deadbandType"),
            deadbandValue: this.getNumber("deadbandValue"),
            dataChangeTrigger: this.getText("dataChangeTrigger"),
            browseNodeClassMask: this.getText("browseNodeClassMask"),
            browseResultMask: this.getText("browseResultMask"),
            browseMaxReferencesToReturn: this.getNumber("browseMaxReferencesToReturn"),
            readMaxAge: this.getNumber("readMaxAge"),
            readTimestampsToReturn: this.getText("readTimestampsToReturn"),
            enableSessionKeepAlive: this.getChecked("enableSessionKeepAlive"),
            keepAliveInterval: this.getNumber("keepAliveInterval"),
            reconnectPeriod: this.getNumber("reconnectPeriod"),
            maxReconnectAttempts: this.getNumber("maxReconnectAttempts")
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