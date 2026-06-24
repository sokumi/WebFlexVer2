type ApiResponse<T> = {
    success: boolean;
    message?: string;
    data?: T;
};

type DeviceRow = {
    id: string;
    deviceCode: string;
    deviceName: string;
    deviceType: string;
};

type DeviceSummaryRow = {
    deviceId: string;
    deviceName: string;
    subscriptionStatus: string;
    todayInsertedCount: number;
};

export default class Page {
    devices: DeviceRow[] = [];
    selectedDeviceId = "";
    isLogAutoRefresh = false;
    isSelectedDeviceAutoRefresh = false;

    init(): void {
        $("#selDevice").on("change", this.selDevice_onChange);

        $("#btnStopSubscription").on("click", () => this.post("/api/opc-collector/subscription/stop"));
        $("#btnStartSubscription").on("click", () => this.post("/api/opc-collector/subscription/start"));
        $("#btnRefresh").on("click", () => this.refresh());

        $("#btnLoadDeviceStatus").on("click", () => this.startSelectedDeviceAutoRefresh());
        $("#btnStopDeviceSubscription").on("click", () => this.postSelectedDevice("stop"));
        $("#btnStartDeviceSubscription").on("click", () => this.postSelectedDevice("start"));

        $("#btnClearLogs").on("click", () => this.clearLogs());
        $("#btnLoadLogs").on("click", () => this.startLogAutoRefresh());

        this.refresh();
        this.loadDevices();

        window.setInterval(() => {
            this.refresh();
        }, 3000);
    }

    async loadDevices(): Promise<void> {
        try {
            const res = await this.get<ApiResponse<DeviceRow[]>>("/device/manage/list");

            this.devices = res.data ?? [];

            const $selDevice = $("#selDevice");
            $selDevice.empty();
            $selDevice.append(`<option value="">디바이스 선택</option>`);

            for (const device of this.devices) {
                $selDevice.append(
                    `<option value="${this.escapeHtml(device.id)}">${this.escapeHtml(device.deviceName)} (${this.escapeHtml(device.deviceType)})</option>`
                );
            }
        } catch (e) {
            alert(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }

    async get<T>(url: string): Promise<T> {
        return await $.ajax({
            url,
            method: "GET",
            dataType: "json"
        }) as T;
    }

    selDevice_onChange = (): void => {
        this.selectedDeviceId = String($("#selDevice").val() ?? "");
    };

    async postSelectedDevice(action: "stop" | "start"): Promise<void> {
        const deviceId = this.selectedDeviceId;

        if (!deviceId) {
            alert("디바이스를 선택하세요.");
            return;
        }

        const encodedDeviceId = encodeURIComponent(deviceId);

        await this.post(`/api/opc-collector/device/${encodedDeviceId}/subscription/${action}`);
        await this.loadSelectedDeviceStatus(false);
    }

    async post(url: string): Promise<void> {
        try {
            const response = await fetch(url, {
                method: "POST"
            });

            if (!response.ok) {
                throw new Error(await response.text());
            }

            await this.refresh();
        } catch (e) {
            console.error(e);
            alert("요청 처리 중 오류가 발생했습니다.");
        }
    }

    async refresh(): Promise<void> {
        await this.loadStatus();
        await this.loadDeviceSummary();

        if (this.isSelectedDeviceAutoRefresh) {
            await this.loadSelectedDeviceStatus(false);
        }

        if (this.isLogAutoRefresh) {
            await this.loadLogs();
        }
    }

    async loadDeviceSummary(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/device-summary");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const rows = await response.json() as DeviceSummaryRow[];

            const html = rows
                .map(x => `
                    <tr>
                        <td>${this.escapeHtml(x.deviceName ?? "-")}</td>
                        <td>${this.escapeHtml(x.subscriptionStatus ?? "-")}</td>
                    </tr>
                `)
                .join("");

            $("#deviceSummaryBody").html(
                html || `<tr><td colspan="3">조회된 디바이스가 없습니다.</td></tr>`
            );
        } catch (e) {
            console.error(e);
            $("#deviceSummaryBody").html(
                `<tr><td colspan="3">조회 실패</td></tr>`
            );
        }
    }

    async startSelectedDeviceAutoRefresh(): Promise<void> {
        const deviceId = this.selectedDeviceId;

        if (!deviceId) {
            alert("디바이스를 선택하세요.");
            return;
        }

        this.isSelectedDeviceAutoRefresh = true;
        await this.loadSelectedDeviceStatus(false);
    }

    async startLogAutoRefresh(): Promise<void> {
        this.isLogAutoRefresh = true;
        await this.loadLogs();
    }

    clearLogs(): void {
        $("#logBox").empty();
    }

    async loadStatus(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/status");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json();

            this.setTextWithFlash("#lblDeviceCount", data.deviceCount);
            this.setTextWithFlash("#lblSubscribedCount", data.subscribedCount);
            this.setTextWithFlash("#lblTotalSnapshotRows", data.totalSnapshotRows);
            this.setTextWithFlash("#lblTotalInserted", data.totalInserted);
            this.setTextWithFlash("#lblSubscriptionStopped", data.subscriptionStopped ? "중지" : "동작");
        } catch (e) {
            console.error(e);
            $("#lblDeviceCount").text("연결 실패");
        }
    }

    async loadSelectedDeviceStatus(showAlert: boolean = true): Promise<void> {
        const deviceId = this.selectedDeviceId;

        if (!deviceId) {
            if (showAlert) {
                alert("디바이스를 선택하세요.");
            }
            return;
        }

        try {
            const encodedDeviceId = encodeURIComponent(deviceId);
            const response = await fetch(`/api/opc-collector/device/${encodedDeviceId}/status`);

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json();
            const runtime = data.runtimeStatus ?? {};

            this.setTextWithFlash("#lblSelectedDeviceId", data.deviceId);
            this.setTextWithFlash("#lblSelectedDeviceName", data.deviceName || "-");
            this.setTextWithFlash("#lblSelectedTagCount", data.tagCount);
            this.setTextWithFlash("#lblSelectedSubscribedCount", runtime.subscribedCount);
            this.setTextWithFlash("#lblSelectedCurrentValueCount", runtime.currentValueCount);
            this.setTextWithFlash("#lblSelectedTotalSnapshotRows", data.totalSnapshotRows);
            this.setTextWithFlash("#lblSelectedTotalInserted", data.totalInserted);
            this.setTextWithFlash("#lblSelectedSubscriptionStopped", runtime.subscriptionStopped ? "중지" : "동작");
        } catch (e) {
            console.error(e);
            $("#lblSelectedDeviceName").text("조회 실패");
        }
    }

    async loadLogs(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/logs");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const logs = await response.json();

            const html = logs
                .map((x: any) => {
                    const time = this.escapeHtml(x.time ?? "");
                    const level = this.escapeHtml(x.level ?? "");
                    const message = this.escapeHtml(x.message ?? "");

                    return `<div class="opc-log-row">
                        <span class="opc-log-time">${time}</span>
                        <span class="opc-log-level">${level}</span>
                        <span class="opc-log-message">${message}</span>
                    </div>`;
                })
                .join("");

            $("#logBox").html(html || "<div class='opc-log-empty'>로그가 없습니다.</div>");
        } catch (e) {
            console.error(e);
            $("#logBox").html("<div class='opc-log-empty'>로그 조회 실패</div>");
        }
    }

    escapeHtml(value: string): string {
        return value
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    setTextWithFlash(selector: string, value: any): void {
        const $el = $(selector);
        const newText = value == null || value === "" ? "-" : String(value);
        const oldText = $el.text();

        if (oldText === newText) {
            return;
        }

        $el.text(newText);

        $el.removeClass("value-flash");

        void ($el[0] as HTMLElement).offsetWidth;

        $el.addClass("value-flash");

        window.setTimeout(() => {
            $el.removeClass("value-flash");
        }, 700);
    }
}