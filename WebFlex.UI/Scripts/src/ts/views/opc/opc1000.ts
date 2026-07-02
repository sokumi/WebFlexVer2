import $ from "jquery";

import { api, escapeHtml } from "../../framework/common";
import { notify } from "../../framework/notify";

export default class Page {
    devices: any[] = [];
    deviceSummaries: any[] = [];
    deviceStatuses = new Map<any, any>();
    isLogAutoRefresh = false;
    isLogCollapsed = true;
    refreshTimerId: any = null;
    isRefreshing = false;

    init() {
        $("#deviceCardHost").on("click", "[data-subscription-action]", event => {
            const $button = $(event.currentTarget);
            const deviceId = String($button.attr("data-device-id") ?? "");
            const action = String($button.attr("data-subscription-action") ?? "");

            void this.postDeviceSubscription(deviceId, action);
        });

        $("#btnRefreshStatus").on("click", () => {
            void this.refresh();
        });

        $("#btnClearLogs").on("click", () => this.clearLogs());

        $("#btnLoadLogs").on("click", () => {
            void this.startLogAutoRefresh();
        });

        $("#btnToggleLogs").on("click", () => this.toggleLogs());

        this.setLogCollapsed(true);
        void this.refresh();

        this.refreshTimerId = window.setInterval(() => {
            void this.refresh();
        }, 3000);

        window.addEventListener("beforeunload", () => {
            if (this.refreshTimerId != null) {
                window.clearInterval(this.refreshTimerId);
            }
        });
    }

    async refresh() {
        if (this.isRefreshing) {
            return;
        }

        this.isRefreshing = true;

        try {
            await this.loadDevices();
            await this.loadStatus();
            await this.loadDeviceSummary();
            await this.loadDeviceCards();

            if (this.isLogAutoRefresh) {
                await this.loadLogs();
            }
        } finally {
            this.isRefreshing = false;
        }
    }

    async loadDevices() {
        try {
            const result = await api.get({
                url: "/device/manage/list"
            });

            if (!result.success) {
                this.devices = [];
                this.renderDeviceError(result.message ?? "디바이스 조회 실패");
                return;
            }

            this.devices = (result.data ?? []).map((x: any) => this.normalizeDeviceRow(x));
        } catch (e) {
            console.error(e);
            this.devices = [];
            this.renderDeviceError("디바이스 조회 실패");
        }
    }

    async loadStatus() {
        try {
            const data = await this.getJson("/api/opc-collector/status");

            const version = data.collectorVersion ?? data.version ?? "-";
            const totalDeviceCount = this.devices.length > 0
                ? this.devices.length
                : data.deviceCount ?? 0;

            this.setTextWithFlash("#lblDeviceCount", `${this.formatNumber(totalDeviceCount)}/${this.formatNumber(totalDeviceCount)}`);
            this.setTextWithFlash("#lblSubscribedCount", this.formatNumber(data.subscribedCount));
            this.setTextWithFlash("#lblTotalSnapshotRows", this.formatNumber(data.totalSnapshotRows));
            this.setTextWithFlash("#lblTotalInserted", this.formatNumber(data.totalInserted));
            this.setTextWithFlash("#lblCollectorVersion", version);

            $("#lblSnapshotTime").text(`마지막 ${this.getCurrentTimeText()}`);
            $("#lblCollectorDbStatus").text(data.dbStatus ?? "DB 상태 -");

            this.updateStoppedDeviceCount();
        } catch (e) {
            console.error(e);

            this.setTextWithFlash("#lblDeviceCount", this.devices.length > 0 ? `${this.devices.length}/${this.devices.length}` : "조회 실패");
            this.setTextWithFlash("#lblSubscribedCount", "-");
            this.setTextWithFlash("#lblTotalSnapshotRows", "-");
            this.setTextWithFlash("#lblTotalInserted", "-");
            this.setTextWithFlash("#lblCollectorVersion", "-");

            $("#lblStoppedCount").text("상태 조회 실패");
            $("#lblCollectorDbStatus").text("DB 상태 -");
        }
    }

    async loadDeviceSummary() {
        try {
            this.deviceSummaries = await this.getJson("/api/opc-collector/device-summary");
        } catch (e) {
            console.error(e);
            this.deviceSummaries = [];
        }
    }

    async loadDeviceCards() {
        if (this.devices.length === 0) {
            this.renderDeviceEmpty();
            return;
        }

        await Promise.all(
            this.devices.map(async (device: any) => {
                const deviceId = device.id ?? "";

                if (deviceId.length === 0) {
                    return;
                }

                try {
                    const status = await this.getJson(`/api/opc-collector/device/${encodeURIComponent(deviceId)}/status`);
                    this.deviceStatuses.set(deviceId, status);
                } catch (e) {
                    console.error(e);

                    this.deviceStatuses.set(deviceId, {
                        deviceId,
                        deviceName: device.deviceName ?? "-",
                        tagCount: device.tagCount ?? 0,
                        runtimeStatus: {
                            subscribedCount: 0,
                            currentValueCount: 0,
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

    renderDeviceCards() {
        const rows = this.devices.map((device: any) => {
            const deviceId = device.id ?? "";
            const summary = this.deviceSummaries.find((x: any) => x.deviceId === deviceId) ?? null;
            const status = this.deviceStatuses.get(deviceId) ?? null;

            return {
                device,
                summary,
                status
            };
        });

        $("#lblDeviceSummaryText").text(`${rows.length.toLocaleString()}개 디바이스`);

        const html = rows
            .map((row: any) => this.createDeviceCardHtml(row))
            .join("");

        $("#deviceCardHost").html(html || this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }

    createDeviceCardHtml(row: any) {
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

        const subscriptionStopped = this.isSubscriptionStopped(row);
        const isRunning = !subscriptionStopped && subscribedCount > 0;
        const isEnabled = device.isEnabled !== false && device.isCollectEnabled !== false;

        const statusText = !isEnabled
            ? "수집비활성"
            : isRunning
                ? "연결됨"
                : "구독중지";

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
                            <h4>${escapeHtml(deviceName)}</h4>
                            <small>${escapeHtml(deviceCode)}</small>
                        </div>
                    </div>

                    <span class="wf-opc-device-status ${cardStateClass}">
                        <span class="wf-status-dot"></span>
                        ${escapeHtml(statusText)}
                    </span>
                </div>

                <div class="wf-opc-device-type">${escapeHtml(deviceType)}</div>

                <div class="wf-opc-endpoint">
                    <i data-lucide="link"></i>
                    <span>${escapeHtml(endpoint)}</span>
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
                        <span>Snapshot</span>
                        <strong>${this.formatNumber(status?.totalSnapshotRows)}</strong>
                    </div>
                    <div>
                        <span>DB Insert</span>
                        <strong>${this.formatNumber(totalInserted)}</strong>
                    </div>
                </div>

                <div class="wf-opc-progress">
                    <span style="width:${percent}%"></span>
                </div>

                <div class="wf-opc-device-footer">
                    <div class="wf-opc-device-time">
                        <span>
                            <i data-lucide="timer"></i>
                            ${percent}%
                        </span>
                        <span>
                            <i data-lucide="clock"></i>
                            ${this.getCurrentTimeText()}
                        </span>
                    </div>

                    <div class="wf-opc-device-actions">
                        <button type="button"
                                class="btn btn-outline-primary btn-sm"
                                data-device-id="${escapeHtml(deviceId)}"
                                data-subscription-action="${isRunning ? "stop" : "start"}"
                                ${!isEnabled ? "disabled" : ""}>
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

    isSubscriptionStopped(row: any) {
        const runtime = row.status?.runtimeStatus ?? {};
        const summaryStatus = row.summary?.subscriptionStatus ?? "";

        return runtime.subscriptionStopped === true ||
            summaryStatus === "Stopped" ||
            summaryStatus === "SubscriptionStopped" ||
            summaryStatus === "중지";
    }

    updateStoppedDeviceCount() {
        const totalCount = this.devices.length;

        if (totalCount === 0) {
            $("#lblStoppedCount").text("0개 구독중지");
            return;
        }

        const stoppedCount = this.devices.filter((device: any) => {
            const deviceId = device.id ?? "";
            const summary = this.deviceSummaries.find((x: any) => x.deviceId === deviceId) ?? null;
            const status = this.deviceStatuses.get(deviceId) ?? null;

            return this.isSubscriptionStopped({
                device,
                summary,
                status
            });
        }).length;

        $("#lblStoppedCount").text(`${stoppedCount.toLocaleString()}개 구독중지`);
    }

    renderDeviceEmpty() {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("0개 구독중지");
        $("#deviceCardHost").html(this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }

    renderDeviceError(message: any) {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("상태 조회 실패");
        $("#deviceCardHost").html(this.createEmptyHtml(message));
        this.refreshIcons();
    }

    createEmptyHtml(message: any) {
        return `
            <article class="wf-opc-empty-card">
                <i data-lucide="server-off"></i>
                <strong>${escapeHtml(message)}</strong>
                <span>Collector 상태 또는 디바이스 설정을 확인해 주세요.</span>
            </article>
        `;
    }

    async postDeviceSubscription(deviceId: any, action: any) {
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

            notify.success(action === "start" ? "디바이스 구독 시작 요청 완료" : "디바이스 구독 중지 요청 완료");

            await this.loadStatus();
            await this.loadDeviceSummary();
            await this.loadDeviceCards();
        } catch (e) {
            console.error(e);
            notify.error(e instanceof Error ? e.message : "요청 처리 중 오류가 발생했습니다.");
        }
    }

    async startLogAutoRefresh() {
        this.isLogAutoRefresh = true;
        this.setLogCollapsed(false);
        await this.loadLogs();
    }

    clearLogs() {
        $("#logBox").empty();
        $("#lblLogCount").text("0개 항목");
    }

    async loadLogs() {
        try {
            const logs = await this.getJson("/api/opc-collector/logs?count=100");

            const html = logs
                .map((x: any) => {
                    const time = escapeHtml(x.time ?? "");
                    const level = escapeHtml(x.level ?? "");
                    const message = escapeHtml(x.message ?? "");

                    return `
                        <div class="wf-opc-log-row">
                            <span class="wf-opc-log-time">${time}</span>
                            <span class="wf-opc-log-level">${level}</span>
                            <span class="wf-opc-log-message">${message}</span>
                        </div>
                    `;
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

    toggleLogs() {
        this.setLogCollapsed(!this.isLogCollapsed);
    }

    setLogCollapsed(isCollapsed: any) {
        this.isLogCollapsed = isCollapsed;

        $("#opcLogCard").toggleClass("is-collapsed", isCollapsed);
        $("#lblLogToggleText").text(isCollapsed ? "펼치기" : "접기");
        $("#btnToggleLogs").attr("aria-expanded", String(!isCollapsed));

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }

    getEndpointText(device: any) {
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

    getCurrentTimeText() {
        const now = new Date();

        return [
            String(now.getHours()).padStart(2, "0"),
            String(now.getMinutes()).padStart(2, "0"),
            String(now.getSeconds()).padStart(2, "0")
        ].join(":");
    }

    formatNumber(value: any) {
        if (value == null || value === "") {
            return "-";
        }

        const numberValue = Number(value);

        if (Number.isNaN(numberValue)) {
            return String(value);
        }

        return numberValue.toLocaleString();
    }

    async getJson(url: any) {
        const response = await fetch(url);

        if (!response.ok) {
            throw new Error(await response.text());
        }

        return await response.json();
    }

    refreshIcons() {
        window.setTimeout(() => {
            const lucide = (window as any).lucide;

            if (lucide?.createIcons != null) {
                lucide.createIcons();
            }
        }, 0);
    }

    setTextWithFlash(selector: any, value: any) {
        const $el = $(selector);
        const newText = value == null || value === "" ? "-" : String(value);
        const oldText = $el.text();

        if (oldText === newText) {
            return;
        }

        $el.text(newText);

        const element = $el[0] as any;

        if (element != null) {
            void element.offsetWidth;
        }
    }

    normalizeDeviceRow(row: any) {
        return {
            ...row,
            id: this.readValue(row, "id", "ID", "deviceId", "DEVICE_ID") ?? "",
            deviceCode: this.readValue(row, "deviceCode", "id", "ID") ?? "",
            deviceName: this.readValue(row, "deviceName", "DEVICE_NAME") ?? "",
            deviceType: this.readValue(row, "deviceType", "DEVICE_TYPE") ?? "",
            endpointUrl: this.readValue(row, "endpointUrl", "ENDPOINT_URL") ?? "",
            deviceAddress: this.readValue(row, "deviceAddress", "DEVICE_ADDRESS") ?? "",
            port: this.readValue(row, "port", "PORT"),
            tagCount: this.readNumber(row, "tagCount", "TAG_COUNT") ?? 0,
            isCollectEnabled: this.readBool(row, true, "isCollectEnabled", "IS_COLLECTENABLED"),
            isEnabled: this.readBool(row, true, "isEnabled", "IsEnabled")
        };
    }

    readValue(row: any, ...names: any[]) {
        if (row == null) {
            return null;
        }

        for (const name of names) {
            if (Object.prototype.hasOwnProperty.call(row, name)) {
                return row[name];
            }
        }

        const normalizedNames = names.map((x: any) => this.normalizeFieldName(x));

        for (const key of Object.keys(row)) {
            if (normalizedNames.includes(this.normalizeFieldName(key))) {
                return row[key];
            }
        }

        return null;
    }

    readBool(row: any, defaultValue: any, ...names: any[]) {
        const value = this.readValue(row, ...names);

        if (value == null) {
            return defaultValue;
        }

        if (typeof value === "boolean") {
            return value;
        }

        const text = String(value).trim().toLowerCase();

        if (text === "true" || text === "1" || text === "y" || text === "yes") {
            return true;
        }

        if (text === "false" || text === "0" || text === "n" || text === "no") {
            return false;
        }

        return defaultValue;
    }

    readNumber(row: any, ...names: any[]) {
        const value = this.readValue(row, ...names);

        if (value == null || value === "") {
            return null;
        }

        const numberValue = Number(value);

        return Number.isFinite(numberValue)
            ? numberValue
            : null;
    }

    normalizeFieldName(value: any) {
        return String(value ?? "")
            .replace(/_/g, "")
            .replace(/-/g, "")
            .toLowerCase();
    }
}
