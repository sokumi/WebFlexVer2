export default class {
    init(): void {
        wf.onClick("btnSearch", this.btnSearch_onClick);

        console.log("DVC1000 loaded.");
    }

    btnSearch_onClick = (): void => {
        wf.toast("조회 클릭");
    };
}