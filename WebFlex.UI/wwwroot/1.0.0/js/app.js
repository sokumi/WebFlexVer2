/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./Scripts/src/ts/framework/common.ts"
/*!********************************************!*\
  !*** ./Scripts/src/ts/framework/common.ts ***!
  \********************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   common: () => (/* binding */ common)
/* harmony export */ });
async function request(options) {
    var _a;
    const method = (_a = options.method) !== null && _a !== void 0 ? _a : "GET";
    const fetchOptions = {
        method,
        headers: {}
    };
    if (options.data instanceof FormData) {
        fetchOptions.body = options.data;
    }
    else if (options.data) {
        fetchOptions.headers = {
            "Content-Type": "application/json"
        };
        fetchOptions.body = JSON.stringify(options.data);
    }
    const response = await fetch(options.url, fetchOptions);
    if (!response.ok) {
        const message = await response.text();
        throw new Error(message || "요청 처리 중 오류가 발생했습니다.");
    }
    const contentType = response.headers.get("content-type");
    if (contentType === null || contentType === void 0 ? void 0 : contentType.includes("application/json")) {
        return await response.json();
    }
    return await response.text();
}
function getEl(id) {
    return document.getElementById(id);
}
function onClick(id, handler) {
    var _a;
    (_a = getEl(id)) === null || _a === void 0 ? void 0 : _a.addEventListener("click", handler);
}
function toFormData(data) {
    const formData = new FormData();
    for (const key in data) {
        const value = data[key];
        if (value === null || value === undefined) {
            continue;
        }
        formData.append(key, String(value));
    }
    return formData;
}
function alert(message) {
    window.alert(message);
}
function toast(message) {
    console.log(message);
}
const common = {
    request,
    getEl,
    onClick,
    toFormData,
    alert,
    toast
};


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
/*!*******************************!*\
  !*** ./Scripts/src/ts/app.ts ***!
  \*******************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _framework_common__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./framework/common */ "./Scripts/src/ts/framework/common.ts");

window.wf = _framework_common__WEBPACK_IMPORTED_MODULE_0__.common;
window.viewModel = null;

})();

/******/ })()
;
//# sourceMappingURL=app.js.map