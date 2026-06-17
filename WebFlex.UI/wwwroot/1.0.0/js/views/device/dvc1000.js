/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/*!************************************************!*\
  !*** ./Scripts/src/ts/views/device/dvc1000.ts ***!
  \************************************************/

function btnSearch_onClick() {
    console.log("조회 클릭");
}
function grid1_onClick() {
    console.log("grid1 클릭");
}
document.addEventListener("DOMContentLoaded", () => {
    const btnSearch = document.getElementById("btnSearch");
    const grid1 = document.getElementById("grid1");
    btnSearch === null || btnSearch === void 0 ? void 0 : btnSearch.addEventListener("click", btnSearch_onClick);
    grid1 === null || grid1 === void 0 ? void 0 : grid1.addEventListener("click", grid1_onClick);
});

/******/ })()
;
//# sourceMappingURL=dvc1000.js.map