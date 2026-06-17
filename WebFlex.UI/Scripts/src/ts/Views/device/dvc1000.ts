function btnSearch_onClick(): void {
    console.log("조회 클릭");
}

function grid1_onClick(): void {
    console.log("grid1 클릭");
}

document.addEventListener("DOMContentLoaded", () => {
    const btnSearch = document.getElementById("btnSearch");
    const grid1 = document.getElementById("grid1");

    btnSearch?.addEventListener("click", btnSearch_onClick);
    grid1?.addEventListener("click", grid1_onClick);
});