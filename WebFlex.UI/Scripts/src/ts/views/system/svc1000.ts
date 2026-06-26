import { notify } from "../../framework/notify";

type ServiceStatusRow = {
    serviceName: string;
    displayName: string;
    status: string;
    exists: boolean;
    exePath: string;
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

    public init(): void {
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

        $("#btnDeployZip").on("click", () => {
            void this.deployZip();
        });

        $("#btnToggleLog").on("click", () => {
            this.toggleLog();
        });

        this.setLogCollapsed(true);
        void this.loadStatus();

        window.setInterval(() => {
            if (this.isAutoRefresh) {
                void this.loadStatus();
            }
        }, 3000);
    }

     async loadStatus(showToast: boolean = false): Promise<void> {
        try {
            const response = await fetch("/system/service/status");
            const text = await response.text();

            if (!response.ok) {
                throw new Error(text);
            }

            const data = JSON.parse(text) as ServiceStatusRow & { error?: string };

            this.renderStatus(data);
            this.setButtonState(data);
            this.writeResult(data);

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

        // PID/메모리는 이후 기능 추가 예정이라 우선 비워둔다.
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
            const response = await fetch(url, {
                method: "POST"
            });

            const text = await response.text();

            if (!response.ok) {
                throw new Error(text);
            }

            const data = JSON.parse(text) as ServiceCommandResult;

            this.writeResult(data);

            if (!data.success) {
                notify.error(data.message ?? "요청 처리에 실패했습니다.");
            } else {
                notify.success(data.message ?? successMessage ?? "요청 처리 완료");
            }

            await this.loadStatus();
        } catch (e) {
            console.error(e);
            notify.error("요청 처리 중 오류가 발생했습니다.");
            this.writeError(e);
        }
    }

     async installService(): Promise<void> {
        const ok = confirm("WebFlex OPC Collector 서비스를 등록할까요?");

        if (!ok) {
            return;
        }

        await this.post("/system/service/install", "서비스 등록이 완료되었습니다.");
    }

     async restartService(): Promise<void> {
        const ok = confirm("WebFlex OPC Collector 서비스를 재시작할까요?");

        if (!ok) {
            return;
        }

        await this.post("/system/service/restart", "서비스 재시작 요청이 완료되었습니다.");
    }

     async uninstallService(): Promise<void> {
        const ok = confirm("WebFlex OPC Collector 서비스를 삭제할까요?");

        if (!ok) {
            return;
        }

        await this.post("/system/service/uninstall", "서비스 삭제가 완료되었습니다.");
    }

     async deployZip(): Promise<void> {
        const input = document.getElementById("collectorZipFile") as HTMLInputElement | null;

        if (!input || !input.files || input.files.length === 0) {
            notify.warning("업로드할 ZIP 파일을 선택하세요.");
            return;
        }

        const file = input.files[0];

        if (!file.name.toLowerCase().endsWith(".zip")) {
            notify.warning("ZIP 파일만 업로드할 수 있습니다.");
            return;
        }

        const ok = confirm("서비스를 중지하고 ZIP 파일을 배포한 뒤 다시 시작할까요?");

        if (!ok) {
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        try {
            this.setUploading(true);

            const response = await fetch("/system/service/deploy-zip", {
                method: "POST",
                body: formData
            });

            const text = await response.text();

            if (!response.ok) {
                throw new Error(text);
            }

            const data = JSON.parse(text) as ServiceCommandResult;

            this.writeResult(data);

            if (!data.success) {
                notify.error(data.message ?? "배포에 실패했습니다.");
            } else {
                notify.success(data.message ?? "ZIP 배포가 완료되었습니다.");
            }

            await this.loadStatus();
        } catch (e) {
            console.error(e);
            notify.error("ZIP 배포 중 오류가 발생했습니다.");
            this.writeError(e);
        } finally {
            this.setUploading(false);
        }
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

     setUploading(isUploading: boolean): void {
        $("#btnDeployZip").prop("disabled", isUploading);

        if (isUploading) {
            $("#btnDeployZip").text("배포 중...");
        } else {
            $("#btnDeployZip").text("ZIP 업로드 후 배포/재시작");
        }
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
        if (text.trim().length === 0) {
            this.logCount = 0;
        } else {
            this.logCount = text.split(/\r?\n/).filter(x => x.trim().length > 0).length;
        }

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