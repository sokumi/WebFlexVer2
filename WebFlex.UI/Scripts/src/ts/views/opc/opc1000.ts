import { notify } from "../../framework/notify";
import type { ApiResponse } from "../../dtos/apiResponse";
import type { DeviceDto, DeviceSummaryDto } from "../../dtos/deviceDto";

type DeviceRow = DeviceDto & {
    endpointUrl?: string | null;
    deviceAddress?: string | null;
    port?: number | string | null;
    tagCount?: number | null;
};

type DeviceSummaryRow = DeviceSummaryDto;

type CollectorStatusDto = {
    deviceCount?: number;
    subscribedCount?: number;
    totalSnapshotRows?: number;
    totalInserted?: number;
    subscriptionStopped?: boolean;
    version?: string;
    collectorVersion?: string;
    dbStatus?: string;
};

type DeviceRuntimeStatusDto = {
    subscribedCount?: number;
    currentValueCount?: number;
    subscriptionStopped?: boolean;
};

type DeviceStatusDto = {
    deviceId?: string;
    deviceName?: string;
    tagCount?: number;
    totalSnapshotRows?: number;
    totalInserted?: number;
    runtimeStatus?: DeviceRuntimeStatusDto;
};

type LogRow = {
    time?: string;
    level?: string;
    message?: string;
};

type DeviceCardViewModel = {
    device: DeviceRow;
    summary: DeviceSummaryRow | null;
    status: DeviceStatusDto | null;
};

export default class Page {
    private devices: DeviceRow[] = [];
    private deviceSummaries: DeviceSummaryRow[] = [];
    private deviceStatuses = new Map<string, DeviceStatusDto>();
    private isLogAutoRefresh = false;
    private isLogCollapsed = true;

    public init(): void {
        $("#deviceCardHost").on("click", "[data-subscription-action]", event => {
            const $button = $(event.currentTarget);
            const deviceId = String($button.attr("data-device-id") ?? "");
            const action = String($button.attr("data-subscription-action") ?? "") as "start" | "stop";

            void this.postDeviceSubscription(deviceId, action);
        });

        $("#btnClearLogs").on("click", () => this.clearLogs());

        $("#btnLoadLogs").on("click", () => {
            void this.startLogAutoRefresh();
        });

        $("#btnToggleLogs").on("click", () => this.toggleLogs());

        this.setLogCollapsed(true);
        void this.refresh();

        window.setInterval(() => {
            void this.refresh();
        }, 3000);
    }

    private async refresh(): Promise<void> {
        await this.loadDevices();
        await this.loadStatus();
        await this.loadDeviceSummary();
        await this.loadDeviceCards();

        if (this.isLogAutoRefresh) {
            await this.loadLogs();
        }
    }

    private async loadDevices(): Promise<void> {
        try {
            const res = await this.get<ApiResponse<DeviceRow[]>>("/device/manage/list");
            this.devices = (res.data ?? []).slice(0, 2);
        } catch (e) {
            console.error(e);
            this.devices = [];
            this.renderDeviceError("디바이스 조회 실패");
        }
    }

    private async loadStatus(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/status");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json() as CollectorStatusDto;
            const version = data.collectorVersion ?? data.version ?? "v2.4.1.20260618";
            const totalDeviceCount = this.devices.length > 0
                ? this.devices.length
                : data.deviceCount ?? 0;

            this.setTextWithFlash("#lblDeviceCount", `${this.formatNumber(totalDeviceCount)}/${this.formatNumber(totalDeviceCount)}`);
            this.setTextWithFlash("#lblSubscribedCount", this.formatNumber(data.subscribedCount));
            this.setTextWithFlash("#lblTotalSnapshotRows", this.formatNumber(data.totalSnapshotRows));
            this.setTextWithFlash("#lblTotalInserted", this.formatNumber(data.totalInserted));
            this.setTextWithFlash("#lblCollectorVersion", version);

            $("#lblSnapshotTime").text(`마지막 ${this.getCurrentTimeText()}`);
            $("#lblCollectorDbStatus").text(data.dbStatus ?? "DB 정상");

            this.updateStoppedDeviceCount();
        } catch (e) {
            console.error(e);
            this.setTextWithFlash("#lblDeviceCount", "조회 실패");
            this.setTextWithFlash("#lblSubscribedCount", "-");
            this.setTextWithFlash("#lblTotalSnapshotRows", "-");
            this.setTextWithFlash("#lblTotalInserted", "-");
            this.setTextWithFlash("#lblCollectorVersion", "-");
            $("#lblStoppedCount").text("상태 조회 실패");
            $("#lblCollectorDbStatus").text("DB 상태 -");
        }
    }

    private async loadDeviceSummary(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/device-summary");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            this.deviceSummaries = await response.json() as DeviceSummaryRow[];
        } catch (e) {
            console.error(e);
            this.deviceSummaries = [];
        }
    }

    private async loadDeviceCards(): Promise<void> {
        if (this.devices.length === 0) {
            this.renderDeviceEmpty();
            return;
        }

        await Promise.all(
            this.devices.map(async device => {
                const deviceId = device.id ?? "";

                if (deviceId.length === 0) {
                    return;
                }

                try {
                    const response = await fetch(`/api/opc-collector/device/${encodeURIComponent(deviceId)}/status`);

                    if (!response.ok) {
                        throw new Error(await response.text());
                    }

                    const status = await response.json() as DeviceStatusDto;
                    this.deviceStatuses.set(deviceId, status);
                } catch (e) {
                    console.error(e);
                    this.deviceStatuses.set(deviceId, {
                        deviceId,
                        deviceName: device.deviceName ?? "-",
                        runtimeStatus: {
                            subscriptionStopped: true
                        }
                    });
                }
            })
        );

        this.renderDeviceCards();
        this.updateStoppedDeviceCount();
        this.refreshIcons();
    }

    private renderDeviceCards(): void {
        const rows: DeviceCardViewModel[] = this.devices.map(device => {
            const deviceId = device.id ?? "";
            const summary = this.deviceSummaries.find(x => x.deviceId === deviceId) ?? null;
            const status = this.deviceStatuses.get(deviceId) ?? null;

            return {
                device,
                summary,
                status
            };
        });

        $("#lblDeviceSummaryText").text(`${rows.length.toLocaleString()}개 디바이스`);

        const html = rows
            .map(row => this.createDeviceCardHtml(row))
            .join("");

        $("#deviceCardHost").html(html || this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }

    private createDeviceCardHtml(row: DeviceCardViewModel): string {
        const device = row.device;
        const status = row.status;
        const runtime = status?.runtimeStatus ?? {};
        const deviceId = device.id ?? status?.deviceId ?? "";
        const deviceName = status?.deviceName ?? device.deviceName ?? "-";
        const deviceCode = device.deviceCode ?? deviceId;
        const deviceType = device.deviceType ?? "OPCUA";
        const endpoint = this.getEndpointText(device);
        const tagCount = status?.tagCount ?? device.tagCount ?? 0;
        const subscribedCount = runtime.subscribedCount ?? 0;
        const currentValueCount = runtime.currentValueCount ?? 0;
        const totalInserted = status?.totalInserted ?? 0;
        const subscriptionStopped = runtime.subscriptionStopped === true
            || row.summary?.subscriptionStatus === "Stopped"
            || row.summary?.subscriptionStatus === "SubscriptionStopped"
            || row.summary?.subscriptionStatus === "중지";
        const isRunning = !subscriptionStopped && subscribedCount > 0;
        const statusText = isRunning ? "연결됨" : "구독중지";
        const cardStateClass = isRunning ? "is-running" : "is-stopped";
        const percent = tagCount > 0
            ? Math.min(100, Math.round((subscribedCount / tagCount) * 100))
            : 0;

        return `
            <article class="wf-opc-device-card ${cardStateClass}">
                <div class="wf-opc-device-head">
                    <div class="wf-opc-device-title">
                        <span class="wf-opc-device-icon">
                            <i data-lucide="cpu"></i>
                        </span>
                        <div>
                            <h4>${this.escapeHtml(deviceName)}</h4>
                            <small>${this.escapeHtml(deviceCode)}</small>
                        </div>
                    </div>

                    <span class="wf-opc-device-status ${cardStateClass}">
                        <span class="wf-status-dot"></span>
                        ${this.escapeHtml(statusText)}
                    </span>
                </div>

                <div class="wf-opc-device-type">${this.escapeHtml(deviceType)}</div>

                <div class="wf-opc-endpoint">
                    <i data-lucide="link"></i>
                    <span>${this.escapeHtml(endpoint)}</span>
                </div>

                <div class="wf-opc-device-metrics">
                    <div>
                        <span>구독 태그</span>
                        <strong>${this.formatNumber(subscribedCount)} <small>/ ${this.formatNumber(tagCount)}</small></strong>
                    </div>
                    <div>
                        <span>현재값</span>
                        <strong>${this.formatNumber(currentValueCount)} <small>rows</small></strong>
                    </div>
                    <div>
                        <span>CPU</span>
                        <strong>-</strong>
                    </div>
                    <div>
                        <span>메모리</span>
                        <strong>-</strong>
                    </div>
                </div>

                <div class="wf-opc-progress">
                    <span style="width:${percent}%"></span>
                </div>

                <div class="wf-opc-device-footer">
                    <div class="wf-opc-device-time">
                        <span>
                            <i data-lucide="timer"></i>
                            -
                        </span>
                        <span>
                            <i data-lucide="clock"></i>
                            ${this.getCurrentTimeText()}
                        </span>
                    </div>

                    <div class="wf-opc-device-actions">
                        <button type="button"
                                class="btn btn-outline-primary btn-sm"
                                data-device-id="${this.escapeHtml(deviceId)}"
                                data-subscription-action="${isRunning ? "stop" : "start"}">
                            <i data-lucide="${isRunning ? "pause-circle" : "play-circle"}"></i>
                            ${isRunning ? "구독중지" : "구독시작"}
                        </button>

                        <span class="wf-opc-db-badge">
                            <i data-lucide="save"></i>
                            ${totalInserted > 0 ? "DB저장중" : "DB대기"}
                        </span>
                    </div>
                </div>
            </article>
        `;
    }

    private updateStoppedDeviceCount(): void {
        const totalCount = this.devices.length;

        if (totalCount === 0) {
            $("#lblStoppedCount").text("0개 구독중지");
            return;
        }

        const stoppedCount = this.devices.filter(device => {
            const deviceId = device.id ?? "";
            const summary = this.deviceSummaries.find(x => x.deviceId === deviceId) ?? null;
            const status = this.deviceStatuses.get(deviceId) ?? null;
            const runtime = status?.runtimeStatus ?? {};

            return runtime.subscriptionStopped === true
                || summary?.subscriptionStatus === "Stopped"
                || summary?.subscriptionStatus === "SubscriptionStopped"
                || summary?.subscriptionStatus === "중지";
        }).length;

        if (stoppedCount <= 0) {
            $("#lblStoppedCount").text("0개 구독중지");
            return;
        }

        $("#lblStoppedCount").text(`${stoppedCount.toLocaleString()}개 구독중지`);
    }

    private renderDeviceEmpty(): void {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("0개 구독중지");
        $("#deviceCardHost").html(this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }

    private renderDeviceError(message: string): void {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("상태 조회 실패");
        $("#deviceCardHost").html(this.createEmptyHtml(message));
        this.refreshIcons();
    }

    private createEmptyHtml(message: string): string {
        return `
            <article class="wf-opc-empty-card">
                <i data-lucide="server-off"></i>
                <strong>${this.escapeHtml(message)}</strong>
                <span>Collector 상태 또는 디바이스 설정을 확인해 주세요.</span>
            </article>
        `;
    }

    private async postDeviceSubscription(deviceId: string, action: "start" | "stop"): Promise<void> {
        if (deviceId.length === 0) {
            notify.warning("디바이스 정보가 없습니다.");
            return;
        }

        try {
            const response = await fetch(`/api/opc-collector/device/${encodeURIComponent(deviceId)}/subscription/${action}`, {
                method: "POST"
            });

            if (!response.ok) {
                throw new Error(await response.text());
            }

            notify.success(action === "start" ? "디바이스 구독 재시작 요청 완료" : "디바이스 구독 중지 요청 완료");

            await this.loadStatus();
            await this.loadDeviceSummary();
            await this.loadDeviceCards();
        } catch (e) {
            console.error(e);
            notify.error("요청 처리 중 오류가 발생했습니다.");
        }
    }

    private async startLogAutoRefresh(): Promise<void> {
        this.isLogAutoRefresh = true;
        this.setLogCollapsed(false);
        await this.loadLogs();
    }

    private clearLogs(): void {
        $("#logBox").empty();
        $("#lblLogCount").text("0개 항목");
    }

    private async loadLogs(): Promise<void> {
        try {
            const response = await fetch("/api/opc-collector/logs");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const logs = await response.json() as LogRow[];

            const html = logs
                .map(x => {
                    const time = this.escapeHtml(x.time ?? "");
                    const level = this.escapeHtml(x.level ?? "");
                    const message = this.escapeHtml(x.message ?? "");

                    return `<div class="wf-opc-log-row">
                        <span class="wf-opc-log-time">${time}</span>
                        <span class="wf-opc-log-level">${level}</span>
                        <span class="wf-opc-log-message">${message}</span>
                    </div>`;
                })
                .join("");

            $("#logBox").html(html || "<div class='wf-opc-log-empty'>로그가 없습니다.</div>");
            $("#lblLogCount").text(`${logs.length.toLocaleString()}개 항목`);
        } catch (e) {
            console.error(e);
            $("#logBox").html("<div class='wf-opc-log-empty'>로그 조회 실패</div>");
            $("#lblLogCount").text("0개 항목");
        }
    }

    private toggleLogs(): void {
        this.setLogCollapsed(!this.isLogCollapsed);
    }

    private setLogCollapsed(isCollapsed: boolean): void {
        this.isLogCollapsed = isCollapsed;

        $("#opcLogCard").toggleClass("is-collapsed", isCollapsed);
        $("#lblLogToggleText").text(isCollapsed ? "펼치기" : "접기");
        $("#btnToggleLogs").attr("aria-expanded", String(!isCollapsed));

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }

    private getEndpointText(device: DeviceRow): string {
        if (device.endpointUrl != null && device.endpointUrl.length > 0) {
            return device.endpointUrl.replace(/^opc\.tcp:\/\//i, "");
        }

        const address = device.deviceAddress ?? "";
        const port = device.port == null ? "" : String(device.port);

        if (address.length > 0 && port.length > 0) {
            return `${address}:${port}`;
        }

        return "-";
    }

    private getCurrentTimeText(): string {
        const now = new Date();

        return [
            String(now.getHours()).padStart(2, "0"),
            String(now.getMinutes()).padStart(2, "0"),
            String(now.getSeconds()).padStart(2, "0")
        ].join(":");
    }

    private formatNumber(value: number | string | null | undefined): string {
        if (value == null || value === "") {
            return "-";
        }

        const numberValue = Number(value);

        if (Number.isNaN(numberValue)) {
            return String(value);
        }

        return numberValue.toLocaleString();
    }

    private async get<T>(url: string): Promise<T> {
        return await $.ajax({
            url,
            method: "GET",
            dataType: "json"
        }) as T;
    }

    private refreshIcons(): void {
        window.setTimeout(() => {
            const lucide = (window as any).lucide;

            if (lucide?.createIcons != null) {
                lucide.createIcons();
            }
        }, 0);
    }

    private escapeHtml(value: string | number | null | undefined): string {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    private setTextWithFlash(selector: string, value: unknown): void {
        const $el = $(selector);
        const newText = value == null || value === "" ? "-" : String(value);
        const oldText = $el.text();

        if (oldText === newText) {
            return;
        }

        $el.text(newText);
        $el.removeClass("value-flash");

        const element = $el[0] as HTMLElement | undefined;

        if (element != null) {
            void element.offsetWidth;
        }

        $el.addClass("value-flash");

        window.setTimeout(() => {
            $el.removeClass("value-flash");
        }, 700);
    }
}