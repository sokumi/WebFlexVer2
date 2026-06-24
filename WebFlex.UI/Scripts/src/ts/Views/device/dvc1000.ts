import { api } from "../../framework/common";

export default class {
    rows: any[] = [];
    selectedId = 0;

    init(): void {
        $("#btnNew").on("click", this.btnNew_onClick);
        $("#btnSave").on("click", this.btnSave_onClick);
        $("#btnDelete").on("click", this.btnDelete_onClick);

        $("#btnSearch").on("click", this.btnSearch_onClick);

        this.load();
    }

    btnNew_onClick = (): void => {
        this.clearForm();
    };

    btnSearch_onClick = (): void => {
        this.load();
    };

    btnSave_onClick = async (): Promise<void> => {
        const data = this.getFormData();

        try {
            const res = await api.post({
                url: "/device/manage/insert",
                data
            });

            if (!res.success) {
                alert(res.message ?? "저장에 실패했습니다.");
                return;
            }

            alert(res.message ?? "저장되었습니다.");
            await this.load();
            this.clearForm();
        } catch (e) {
            alert(e instanceof Error ? e.message : "저장 중 오류가 발생했습니다.");
        }
    };

    btnDelete_onClick = async (): Promise<void> => {
        if (!this.selectedId) {
            alert("삭제할 디바이스를 선택하세요.");
            return;
        }

        if (!confirm("선택한 디바이스를 삭제하시겠습니까?")) return;

        try {
            const res = await api.post({
                url: "/device/manage/delete",
                data: this.selectedId
            });

            if (!res.success) {
                alert(res.message ?? "삭제에 실패했습니다.");
                return;
            }

            alert(res.message ?? "삭제되었습니다.");
            await this.load();
            this.clearForm();
        } catch (e) {
            alert(e instanceof Error ? e.message : "삭제 중 오류가 발생했습니다.");
        }
    };

    async load(): Promise<void> {
        try {
            const res = await api.get({ url: "/device/manage/list" });

            this.rows = res.data ?? [];
            this.renderGrid();
        } catch (e) {
            alert(e instanceof Error ? e.message : "조회 중 오류가 발생했습니다.");
        }
    }

    renderGrid(): void {
        const $body = $("#grid1Body");
        $body.empty();

        for (const row of this.rows) {
            const $tr = $(`
                <tr>
                    <td>${row.id}</td>
                    <td>${row.deviceName}</td>
                    <td>${row.deviceType}</td>
                    <td>${row.deviceAddress}</td>
                    <td>${row.port}</td>
                    <td>${row.isCollectEnabled ? "Y" : "N"}</td>
                    <td>${row.isEnabled ? "Y" : "N"}</td>
                </tr>
            `);

            $tr.on("click", () => this.grid1_onClick(row));
            $body.append($tr);
        }
    }

    grid1_onClick = (row: any): void => {
        this.selectedId = row.id;

        $("#hidId").val(row.id);
        $("#txtDeviceCode").val(row.deviceCode);
        $("#txtDeviceName").val(row.deviceName);
        $("#selDeviceType").val(row.deviceType);
        $("#txtDeviceAddress").val(row.deviceAddress);
        $("#txtPort").val(row.port);
        $("#txtEndpointUrl").val(row.endpointUrl);
        $("#chkCollect").prop("checked", row.isCollectEnabled);
        $("#chkEnabled").prop("checked", row.isEnabled);
        $("#chkUseSecurity").prop("checked", row.useSecurity);
        $("#chkUseAnonymous").prop("checked", row.useAnonymous);
        $("#txtUserName").val(row.userName ?? "");
        $("#txtPassword").val(row.password ?? "");
        $("#txtPublishingIntervalMs").val(row.publishingIntervalMs);
        $("#txtSamplingIntervalMs").val(row.samplingIntervalMs);
        $("#txtSortOrder").val(row.sortOrder);
        $("#txtDescription").val(row.description ?? "");
    };

    clearForm(): void {
        this.selectedId = 0;

        $("#hidId").val("");
        $("#txtDeviceCode").val("");
        $("#txtDeviceName").val("");
        $("#selDeviceType").val("OPCUA");
        $("#txtDeviceAddress").val("");
        $("#txtPort").val(4840);
        $("#txtEndpointUrl").val("");
        $("#chkCollect").prop("checked", true);
        $("#chkEnabled").prop("checked", true);
        $("#chkUseSecurity").prop("checked", false);
        $("#chkUseAnonymous").prop("checked", true);
        $("#txtUserName").val("");
        $("#txtPassword").val("");
        $("#txtPublishingIntervalMs").val(1000);
        $("#txtSamplingIntervalMs").val(1000);
        $("#txtSortOrder").val(0);
        $("#txtDescription").val("");
    }

    getFormData() {
        return {
            id: this.selectedId || null,
            deviceName: String($("#txtDeviceName").val() ?? ""),
            deviceType: String($("#selDeviceType").val() ?? "OPCUA"),
            deviceAddress: String($("#txtDeviceAddress").val() ?? ""),
            port: Number($("#txtPort").val() ?? 0),
            endpointUrl: String($("#txtEndpointUrl").val() ?? ""),
            isCollectEnabled: $("#chkCollect").prop("checked") === true,
            isEnabled: $("#chkEnabled").prop("checked") === true,
            useSecurity: $("#chkUseSecurity").prop("checked") === true,
            useAnonymous: $("#chkUseAnonymous").prop("checked") === true,
            userName: String($("#txtUserName").val() ?? ""),
            password: String($("#txtPassword").val() ?? ""),
            publishingIntervalMs: Number($("#txtPublishingIntervalMs").val() ?? 1000),
            samplingIntervalMs: Number($("#txtSamplingIntervalMs").val() ?? 1000),
            sortOrder: Number($("#txtSortOrder").val() ?? 0),
            description: String($("#txtDescription").val() ?? "")
        };
    }
}