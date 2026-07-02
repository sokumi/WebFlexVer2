import $ from "jquery";

import { api, escapeHtml } from "../../framework/common";
import { notify } from "../../framework/notify";

export default class Page {
    devices: any[] = [];
    deviceSummaries: any[] = [];
    deviceStatuses = new Map<any, any>();
    renderedDeviceIds: any[] = [];
    isLogAutoRefresh = false;
    isLogCollapsed = true;
    refreshTimerId: any = null;
    isRefreshing = false;
    isRestartingAllSubscription = false;

    init() {
        $("#deviceCardHost").on("click", "[data-subscription-button]", event => {
            const $button = $(event.currentTarget);

            if ($button.prop("disabled")) {
                return;
            }

            const deviceId = String($button.attr("data-device-id") ?? "");
            const action = String($button.attr("data-subscription-action") ?? "");

            void this.postDeviceSubscription(deviceId, action);
        });

        $("#btnRefreshStatus").on("click", () => {
            void this.refresh();
        });

        $("#btnRestartAllSubscription").on("click", () => {
            void this.restartAllSubscription();
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

        if (rows.length === 0) {
            this.renderDeviceEmpty();
            return;
        }

        $("#deviceCardHost .wf-opc-empty-card").remove();

        const nextDeviceIds = rows
            .map((row: any) => String(row.device.id ?? row.status?.deviceId ?? ""))
            .filter((x: any) => x.length > 0);

        $("#deviceCardHost [data-device-card]").each((_, el) => {
            const deviceId = String($(el).attr("data-device-id") ?? "");

            if (!nextDeviceIds.includes(deviceId)) {
                $(el).remove();
            }
        });

        for (const row of rows) {
            const deviceId = String(row.device.id ?? row.status?.deviceId ?? "");

            if (deviceId.length === 0) {
                continue;
            }

            const $card = this.ensureDeviceCard(deviceId);
            this.updateDeviceCard($card, row);
        }

        this.renderedDeviceIds = nextDeviceIds;
    }

    ensureDeviceCard(deviceId: any) {
        let $card = this.findDeviceCard(deviceId);

        if ($card.length > 0) {
            return $card;
        }

        const template = document.getElementById("deviceCardTemplate") as any;

        if (template == null || template.content == null || template.content.firstElementChild == null) {
            return $();
        }

        const newCard = template.content.firstElementChild.cloneNode(true);
        const $newCard = $(newCard);

        $newCard.attr("data-device-id", deviceId);
        $("#deviceCardHost").append($newCard);

        return $newCard;
    }

    updateDeviceCard($card: any, row: any) {
        if ($card.length === 0) {
            return;
        }

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
        const totalSnapshotRows = status?.totalSnapshotRows ?? 0;

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
        const action = isRunning ? "stop" : "start";
        const actionText = isRunning ? "구독중지" : "구독시작";
        const actionIcon = isRunning ? "pause-circle" : "play-circle";

        $card
            .removeClass("is-running is-stopped")
            .addClass(cardStateClass)
            .attr("data-device-id", deviceId);

        const $statusBadge = $card.find("[data-field='statusBadge']");
        $statusBadge
            .removeClass("is-running is-stopped")
            .addClass(cardStateClass);

        this.setCardText($card, "deviceName", deviceName);
        this.setCardText($card, "deviceCode", deviceCode);
        this.setCardText($card, "statusText", statusText);
        this.setCardText($card, "deviceType", deviceType);
        this.setCardText($card, "endpoint", endpoint);
        this.setCardText($card, "subscribedCount", this.formatNumber(subscribedCount));
        this.setCardText($card, "tagCount", this.formatNumber(tagCount));
        this.setCardText($card, "currentValueCount", this.formatNumber(currentValueCount));
        this.setCardText($card, "totalSnapshotRows", this.formatNumber(totalSnapshotRows));
        this.setCardText($card, "totalInserted", this.formatNumber(totalInserted));
        this.setCardText($card, "percent", `${percent}%`);
        this.setCardText($card, "updatedTime", this.getCurrentTimeText());
        this.setCardText($card, "actionText", actionText);
        this.setCardText($card, "dbStatusText", totalInserted > 0 ? "DB저장중" : "DB대기");

        $card.find("[data-field='progressBar']").css("width", `${percent}%`);

        const $button = $card.find("[data-subscription-button]");
        $button
            .attr("data-device-id", deviceId)
            .attr("data-subscription-action", action)
            .prop("disabled", !isEnabled);

        $card.find("[data-field='actionIcon']").attr("data-lucide", actionIcon);
    }

    setCardText($card: any, field: any, value: any) {
        const $el = $card.find(`[data-field='${field}']`);
        const text = value == null || value === "" ? "-" : String(value);

        if ($el.text() === text) {
            return;
        }

        $el.text(text);
    }

    findDeviceCard(deviceId: any) {
        return $("#deviceCardHost [data-device-card]").filter((_, el) =>
            String($(el).attr("data-device-id") ?? "") === String(deviceId ?? "")
        );
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
        this.renderedDeviceIds = [];
        this.deviceStatuses.clear();
        $("#deviceCardHost").html(this.createEmptyHtml("조회된 디바이스가 없습니다."));
        this.refreshIcons();
    }

    renderDeviceError(message: any) {
        $("#lblDeviceSummaryText").text("0개 디바이스");
        $("#lblStoppedCount").text("상태 조회 실패");
        this.renderedDeviceIds = [];
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

    async restartAllSubscription() {
        if (this.isRestartingAllSubscription) {
            return;
        }

        if (!confirm("전체 디바이스 구독을 모두 끊고 다시 재구독하시겠습니까?")) {
            return;
        }

        this.isRestartingAllSubscription = true;

        const $button = $("#btnRestartAllSubscription");
        $button.prop("disabled", true);
        $button.html(`<i data-lucide="loader-circle"></i> 재구독 중`);

        try {
            await this.postCollectorAction("/api/opc-collector/subscription/stop");
            await this.postCollectorAction("/api/opc-collector/subscription/start");

            notify.success("전체 디바이스 구독 자동 재구독 요청 완료");

            await this.loadStatus();
            await this.loadDeviceSummary();
            await this.loadDeviceCards();
        } catch (e) {
            console.error(e);
            notify.error(e instanceof Error ? e.message : "전체 구독 자동 재구독 중 오류가 발생했습니다.");
        } finally {
            this.isRestartingAllSubscription = false;

            $button.prop("disabled", false);
            $button.html(`<i data-lucide="rotate-cw"></i> 전체 구독 자동 재구독`);

            this.refreshIcons();
        }
    }

    async postCollectorAction(url: any) {
        const response = await fetch(url, {
            method: "POST"
        });

        if (!response.ok) {
            throw new Error(await response.text());
        }

        return await response.json();
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
