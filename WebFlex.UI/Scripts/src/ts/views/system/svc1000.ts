import $ from "jquery";

import { api } from "../../framework/common";
import { notify } from "../../framework/notify";

type ServiceStatusRow = {
    serviceName: string;
    displayName: string;
    status: string;
    exists: boolean;
    exePath: string;
    error?: string | null;
};

type ServiceCommandResult = {
    success: boolean;
    message?: string;
    output?: string;
    error?: string;
};

type ServiceVisualStatus = "running" | "stopped" | "not-installed" | "error" | "unknown";

export default class Page {
    isAutoRefresh = true;
    isLogCollapsed = true;
    logCount = 0;
    refreshTimerId: number | null = null;

    init(): void {
        $("#btnRefresh").on("click", () => {
            void this.loadStatus(true);
        });

        $("#btnInstall").on("click", () => {
            void this.installService();
        });

        $("#btnStart").on("click", () => {
            void this.post("/system/service/start", "서비스 시작 요청이 완료되었습니다.");
        });

        $("#btnStop").on("click", () => {
            void this.post("/system/service/stop", "서비스 중지 요청이 완료되었습니다.");
        });

        $("#btnRestart").on("click", () => {
            void this.restartService();
        });

        $("#btnUninstall").on("click", () => {
            void this.uninstallService();
        });

        $("#btnToggleLog").on("click", () => {
            this.toggleLog();
        });

        this.setLogCollapsed(true);
        void this.loadStatus();

        this.refreshTimerId = window.setInterval(() => {
            if (this.isAutoRefresh) {
                void this.loadStatus();
            }
        }, 3000);

        window.addEventListener("beforeunload", () => {
            if (this.refreshTimerId != null) {
                window.clearInterval(this.refreshTimerId);
            }
        });
    }

    async loadStatus(showToast: boolean = false): Promise<void> {
        try {
            const result = await api.get({
                url: "/system/service/status"
            });

            if (!result.success || result.data == null) {
                throw new Error(result.message ?? "서비스 상태 조회에 실패했습니다.");
            }

            const data = result.data as ServiceStatusRow;

            this.renderStatus(data);
            this.setButtonState(data);
            this.writeResult(result);

            if (showToast) {
                notify.success("서비스 상태를 조회했습니다.");
            }

            if (data.status === "Error" && data.error) {
                console.error(data.error);
            }
        } catch (e) {
            console.error(e);
            this.renderErrorStatus();
            this.writeError(e);

            if (showToast) {
                notify.error("서비스 상태 조회에 실패했습니다.");
            }
        }
    }

    renderStatus(data: ServiceStatusRow): void {
        const displayName = data.displayName || "WebFlex Collector Service";
        const serviceName = data.serviceName || "WebFlexCollector";
        const exePath = data.exePath || "-";
        const visualStatus = this.getVisualStatus(data);

        this.setTextWithFlash("#displayName", displayName);
        this.setTextWithFlash("#serviceName", serviceName);
        this.setTextWithFlash("#exePath", exePath);

        this.setTextWithFlash("#servicePid", "-");
        this.setTextWithFlash("#serviceMemory", "-");
        this.setTextWithFlash("#serviceStartType", "자동");

        $("#serviceStatus").text(this.getStatusText(data));

        $("#serviceHeroCard")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass(`is-${visualStatus}`);

        $("#serviceStatusBadge")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass(`is-${visualStatus}`);
    }

    renderErrorStatus(): void {
        $("#serviceStatus").text("조회 실패");

        $("#serviceHeroCard")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass("is-error");

        $("#serviceStatusBadge")
            .removeClass("is-running is-stopped is-not-installed is-error is-unknown")
            .addClass("is-error");
    }

    async post(url: string, successMessage?: string): Promise<void> {
        try {
            const result = await api.post({
                url,
                data: {}
            });

            this.writeResult(result);

            if (!result.success) {
                notify.error(result.message ?? "요청 처리에 실패했습니다.");
            } else {
                notify.success(result.message ?? successMessage ?? "요청 처리 완료");
            }

            await this.loadStatus();
        } catch (e) {
            console.error(e);
            notify.error("요청 처리 중 오류가 발생했습니다.");
            this.writeError(e);
        }
    }

    async installService(): Promise<void> {
        if (!confirm("WebFlex OPC Collector 서비스를 등록할까요?")) {
            return;
        }

        await this.post("/system/service/install", "서비스 등록이 완료되었습니다.");
    }

    async restartService(): Promise<void> {
        if (!confirm("WebFlex OPC Collector 서비스를 재시작할까요?")) {
            return;
        }

        await this.post("/system/service/restart", "서비스 재시작 요청이 완료되었습니다.");
    }

    async uninstallService(): Promise<void> {
        if (!confirm("WebFlex OPC Collector 서비스를 삭제할까요?")) {
            return;
        }

        await this.post("/system/service/uninstall", "서비스 삭제가 완료되었습니다.");
    }

    setButtonState(data: ServiceStatusRow): void {
        const exists = data.exists === true;
        const status = data.status ?? "";

        const isRunning = status === "Running";
        const isStopped = status === "Stopped";
        const isNotInstalled = !exists || status === "NotInstalled";

        $("#btnInstall").prop("disabled", !isNotInstalled);
        $("#btnStart").prop("disabled", !exists || isRunning);
        $("#btnStop").prop("disabled", !exists || isStopped || isNotInstalled);
        $("#btnRestart").prop("disabled", !exists || isNotInstalled);
        $("#btnUninstall").prop("disabled", !exists || isRunning);
    }

    toggleLog(): void {
        this.setLogCollapsed(!this.isLogCollapsed);
    }

    setLogCollapsed(isCollapsed: boolean): void {
        this.isLogCollapsed = isCollapsed;

        $("#serviceLogCard").toggleClass("is-collapsed", isCollapsed);
        $("#serviceLogToggleText").text(isCollapsed ? "펼치기" : "접기");
        $("#btnToggleLog").attr("aria-expanded", String(!isCollapsed));

        window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
    }

    writeResult(data: unknown): void {
        const text = JSON.stringify(data, null, 2);
        $("#resultBox").text(text);
        this.updateLogCount(text);
    }

    writeError(error: unknown): void {
        const text = error instanceof Error
            ? error.message
            : String(error);

        $("#resultBox").text(text);
        this.updateLogCount(text);
    }

    updateLogCount(text: string): void {
        this.logCount = text.trim().length === 0
            ? 0
            : text.split(/\r?\n/).filter(x => x.trim().length > 0).length;

        $("#serviceLogCount").text(`${this.logCount.toLocaleString()}개 항목`);
    }

    getVisualStatus(data: ServiceStatusRow): ServiceVisualStatus {
        if (data.status === "Running") {
            return "running";
        }

        if (data.status === "Stopped") {
            return "stopped";
        }

        if (!data.exists || data.status === "NotInstalled") {
            return "not-installed";
        }

        if (data.status === "Error") {
            return "error";
        }

        return "unknown";
    }

    getStatusText(data: ServiceStatusRow): string {
        if (!data.exists || data.status === "NotInstalled") {
            return "미등록";
        }

        if (data.status === "Running") {
            return "실행 중";
        }

        if (data.status === "Stopped") {
            return "중지됨";
        }

        if (data.status === "Error") {
            return "오류";
        }

        return data.status || "알 수 없음";
    }

    setTextWithFlash(selector: string, value: unknown): void {
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