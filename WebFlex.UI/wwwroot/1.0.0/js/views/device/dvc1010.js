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

/***/ "./Scripts/src/ts/views/device/dvc1010.ts"
/*!************************************************!*\
  !*** ./Scripts/src/ts/views/device/dvc1010.ts ***!
  \************************************************/
(__unused_webpack_module, __webpack_exports__, __webpack_require__) {

__webpack_require__.r(__webpack_exports__);
/* harmony export */ __webpack_require__.d(__webpack_exports__, {
/* harmony export */   "default": () => (__WEBPACK_DEFAULT_EXPORT__)
/* harmony export */ });
/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (class {
    constructor() {
        this.devices = [];
        this.nodes = [];
        this.treeNodes = [];
        this.selectedNodes = [];
        this.selectedDeviceId = 0;
        this.selDevice_onChange = () => {
            var _a;
            this.selectedDeviceId = Number((_a = $("#selDevice").val()) !== null && _a !== void 0 ? _a : 0);
            this.nodes = [];
            this.treeNodes = [];
            this.selectedNodes = [];
            this.renderNodes();
            this.renderSelectedNodes();
            if (this.selectedDeviceId > 0) {
                this.loadTags();
            }
        };
        this.btnBrowse_onClick = async () => {
            var _a, _b;
            if (!this.selectedDeviceId) {
                alert("디바이스를 선택하세요.");
                return;
            }
            try {
                const res = await this.get(`/device/browse?deviceId=${this.selectedDeviceId}`);
                if (!res.success) {
                    alert((_a = res.message) !== null && _a !== void 0 ? _a : "노드 조회에 실패했습니다.");
                    return;
                }
                this.nodes = (_b = res.data) !== null && _b !== void 0 ? _b : [];
                this.treeNodes = this.buildTree(this.nodes);
                this.selectedNodes = [];
                this.renderNodes();
                this.renderSelectedNodes();
            }
            catch (e) {
                alert(e instanceof Error ? e.message : "노드 조회 중 오류가 발생했습니다.");
            }
        };
        this.btnSelectAll_onClick = () => {
            this.selectedNodes = this.nodes.filter(x => x.nodeClass === "Variable");
            this.renderNodes();
            this.renderSelectedNodes();
        };
        this.btnClearSelect_onClick = () => {
            this.selectedNodes = [];
            this.renderNodes();
            this.renderSelectedNodes();
        };
        this.btnSave_onClick = async () => {
            var _a, _b;
            if (!this.selectedDeviceId) {
                alert("디바이스를 선택하세요.");
                return;
            }
            const nodes = this.selectedNodes
                .filter(x => x.nodeClass === "Variable")
                .map(x => ({
                nodeId: x.nodeId,
                displayName: x.displayName,
                dataType: x.dataType
            }));
            if (nodes.length === 0) {
                alert("Variable 노드만 태그로 저장할 수 있습니다.");
                return;
            }
            if (!confirm(`${nodes.length}개의 태그를 저장하시겠습니까?`)) {
                return;
            }
            try {
                const res = await this.post("/device/tag-save", {
                    deviceId: this.selectedDeviceId,
                    nodes
                });
                if (!res.success) {
                    alert((_a = res.message) !== null && _a !== void 0 ? _a : "태그 저장에 실패했습니다.");
                    return;
                }
                alert((_b = res.message) !== null && _b !== void 0 ? _b : "태그가 저장되었습니다.");
                this.selectedNodes = [];
                this.renderNodes();
                this.renderSelectedNodes();
                await this.loadTags();
            }
            catch (e) {
                alert(e instanceof Error ? e.message : "태그 저장 중 오류가 발생했습니다.");
            }
        };
        this.btnSearch_onClick = () => {
            this.loadTags();
        };
    }
    init() {
        console.log("DVC1010 INIT");
        $("#btnBrowse").on("click", this.btnBrowse_onClick);
        $("#btnSelectAll").on("click", this.btnSelectAll_onClick);
        $("#btnClearSelect").on("click", this.btnClearSelect_onClick);
        $("#btnSave").on("click", this.btnSave_onClick);
        $("#btnSearch").on("click", this.btnSearch_onClick);
        $("#selDevice").on("change", this.selDevice_onChange);
        this.loadDevices();
    }
    async loadDevices() {
        var _a;
        try {
            const res = await this.get("/device/list");
            this.devices = (_a = res.data) !== null && _a !== void 0 ? _a : [];
            const $selDevice = $("#selDevice");
            $selDevice.empty();
            $selDevice.append(`<option value="">디바이스 선택</option>`);
            for (const device of this.devices) {
                $selDevice.append(`<option value="${device.id}">${device.deviceName} (${device.deviceType})</option>`);
            }
        }
        catch (e) {
            alert(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }
    async loadTags() {
        var _a;
        if (!this.selectedDeviceId) {
            return;
        }
        try {
            const res = await this.get(`/device/tag-list?deviceId=${this.selectedDeviceId}`);
            this.renderTags((_a = res.data) !== null && _a !== void 0 ? _a : []);
        }
        catch (e) {
            alert(e instanceof Error ? e.message : "태그 조회 중 오류가 발생했습니다.");
        }
    }
    buildTree(nodes) {
        var _a;
        const map = new Map();
        for (const node of nodes) {
            map.set(node.nodeId, {
                ...node,
                children: []
            });
        }
        const roots = [];
        for (const node of map.values()) {
            if (node.parentNodeId && map.has(node.parentNodeId)) {
                (_a = map.get(node.parentNodeId)) === null || _a === void 0 ? void 0 : _a.children.push(node);
            }
            else {
                roots.push(node);
            }
        }
        return roots;
    }
    renderNodes() {
        const $area = $("#nodeList");
        $area.empty();
        for (const node of this.treeNodes) {
            $area.append(this.createNodeElement(node, 0));
        }
    }
    createNodeElement(node, depth) {
        const isVariable = node.nodeClass === "Variable";
        const checked = this.selectedNodes.some(x => x.nodeId === node.nodeId);
        const padding = depth * 18;
        const $wrapper = $(`<div class="tree-node"></div>`);
        const $row = $(`
            <div class="${isVariable ? "tree-node-row node-variable" : "tree-node-row node-object"}" style="padding-left:${padding}px">
            </div>
        `);
        if (isVariable) {
            const $label = $(`
                <label>
                    <input type="checkbox" ${checked ? "checked" : ""} />
                    <span>${node.displayName}</span>
                    <small>${node.nodeId}</small>
                </label>
            `);
            $label.find("input").on("change", (e) => {
                const checked = $(e.currentTarget).prop("checked") === true;
                this.toggleNode(node, checked);
            });
            $row.append($label);
        }
        else {
            $row.append(`
                <div class="tree-group">
                    <span>▾ ${node.displayName}</span>
                    <small>${node.nodeId}</small>
                </div>
            `);
        }
        $wrapper.append($row);
        for (const child of node.children) {
            $wrapper.append(this.createNodeElement(child, depth + 1));
        }
        return $wrapper;
    }
    toggleNode(node, checked) {
        if (checked) {
            if (!this.selectedNodes.some(x => x.nodeId === node.nodeId)) {
                this.selectedNodes.push(node);
            }
        }
        else {
            this.selectedNodes = this.selectedNodes.filter(x => x.nodeId !== node.nodeId);
        }
        this.renderSelectedNodes();
    }
    renderSelectedNodes() {
        var _a;
        const $body = $("#selectedNodeBody");
        $body.empty();
        for (const node of this.selectedNodes) {
            const $tr = $(`
                <tr>
                    <td>${node.nodeId}</td>
                    <td>${node.displayName}</td>
                    <td>${(_a = node.dataType) !== null && _a !== void 0 ? _a : ""}</td>
                </tr>
            `);
            $body.append($tr);
        }
    }
    renderTags(tags) {
        const $body = $("#tagBody");
        $body.empty();
        for (const tag of tags) {
            const $tr = $(`
                <tr>
                    <td>${tag.tagCode}</td>
                    <td>${tag.displayName}</td>
                    <td>${tag.nodeId}</td>
                    <td>${tag.isCollectEnabled ? "Y" : "N"}</td>
                    <td>${tag.saveToDatabase ? "Y" : "N"}</td>
                </tr>
            `);
            $body.append($tr);
        }
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
  !*** ./Scripts/.generated/views__device__dvc1010.ts ***!
  \******************************************************/
__webpack_require__.r(__webpack_exports__);
/* harmony import */ var _src_ts_views_device_dvc1010__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./../src/ts/views/device/dvc1010 */ "./Scripts/src/ts/views/device/dvc1010.ts");
/* harmony import */ var _src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ../src/ts/framework/page */ "./Scripts/src/ts/framework/page.ts");


(0,_src_ts_framework_page__WEBPACK_IMPORTED_MODULE_1__.runPage)(_src_ts_views_device_dvc1010__WEBPACK_IMPORTED_MODULE_0__["default"]);

})();

/******/ })()
;
//# sourceMappingURL=dvc1010.js.map