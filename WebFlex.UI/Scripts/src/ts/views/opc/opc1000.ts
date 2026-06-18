export default class Page {
    private timerId: number | null = null;

    public init(): void {
        $("#btnRestartDevice").on("click", () => this.restartDevice());
        $("#btnRestartAllDevices").on("click", () => this.post("/api/opc-collector/RestartAllDevices"));
        $("#btnStopSubscription").on("click", () => this.post("/api/opc-collector/StopSubscription"));
        $("#btnStartSubscription").on("click", () => this.post("/api/opc-collector/StartSubscription"));
        $("#btnStopDbSave").on("click", () => this.post("/api/opc-collector/StopDbSave"));
        $("#btnStartDbSave").on("click", () => this.post("/api/opc-collector/StartDbSave"));
        $("#btnRestartProcess").on("click", () => this.restartProcess());
        $("#btnRefresh").on("click", () => this.refresh());

        this.refresh();

        this.timerId = window.setInterval(() => {
            this.refresh();
        }, 3000);
    }

    private async restartDevice(): Promise<void> {
        const deviceId = Number($("#txtDeviceId").val());

        if (!deviceId || deviceId <= 0) {
            alert("DeviceId를 입력해줘.");
            return;
        }

        await this.post(`/api/opc-collector/RestartDevice?deviceId=${deviceId}`);
    }

    private async restartProcess(): Promise<void> {
        if (!confirm("OPC Collector 전체를 재가동할까요?")) {
            return;
        }

        await this.post("/api/opc-collector/RestartProcess");
    }

    private async post(url: string): Promise<void> {
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

    private async refresh(): Promise<void> {
        await Promise.all([
            this.loadStatus(),
            this.loadLogs()
        ]);
    }

    private async loadStatus(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/Status");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json();

            $("#lblDeviceCount").text(data.deviceCount ?? "-");
            $("#lblSubscribedCount").text(data.subscribedCount ?? "-");
            $("#lblQueueCount").text(data.queueCount ?? "-");
            $("#lblTotalEnqueued").text(data.totalEnqueued ?? "-");
            $("#lblTotalInserted").text(data.totalInserted ?? "-");
            $("#lblSubscriptionStopped").text(data.subscriptionStopped ? "중지" : "동작");
            $("#lblDbSaveStopped").text(data.dbSaveStopped ? "중지" : "동작");
        } catch (e) {
            console.error(e);
            $("#lblDeviceCount").text("연결 실패");
        }
    }

    private async loadLogs(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/Logs");

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

    private escapeHtml(value: string): string {
        return value
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
}