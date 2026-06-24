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

export default class Page {
    isAutoRefresh = false;

    init(): void {
        $("#btnRefresh").on("click", () => this.loadStatus());

        $("#btnInstall").on("click", () => this.installService());
        $("#btnStart").on("click", () => this.post("/system/service/start"));
        $("#btnStop").on("click", () => this.post("/system/service/stop"));
        $("#btnRestart").on("click", () => this.restartService());
        $("#btnUninstall").on("click", () => this.uninstallService());

        $("#btnDeployZip").on("click", () => this.deployZip());

        this.loadStatus();

        window.setInterval(() => {
            if (this.isAutoRefresh) {
                this.loadStatus();
            }
        }, 3000);
    }

    async loadStatus(): Promise<void> {
        try {
            const response = await fetch("/system/service/status");
            const text = await response.text();

            if (!response.ok) {
                throw new Error(text);
            }

            const data = JSON.parse(text) as ServiceStatusRow & { error?: string };

            this.setTextWithFlash("#serviceName", data.serviceName);
            this.setTextWithFlash("#displayName", data.displayName);
            this.setTextWithFlash("#serviceStatus", data.status);
            this.setTextWithFlash("#exePath", data.exePath);

            this.setButtonState(data);
            this.writeResult(data);

            if (data.status === "Error" && data.error) {
                console.error(data.error);
            }
        } catch (e) {
            console.error(e);
            this.setTextWithFlash("#serviceStatus", "조회 실패");
            this.writeError(e);
        }
    }

    async post(url: string): Promise<void> {
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
                alert(data.message ?? "요청 처리에 실패했습니다.");
            }

            await this.loadStatus();
        } catch (e) {
            console.error(e);
            alert("요청 처리 중 오류가 발생했습니다.");
            this.writeError(e);
        }
    }

    async installService(): Promise<void> {
        const ok = confirm("WebFlex OPC Collector 서비스를 등록할까요?");

        if (!ok) {
            return;
        }

        await this.post("/system/service/install");
    }

    async restartService(): Promise<void> {
        const ok = confirm("WebFlex OPC Collector 서비스를 재시작할까요?");

        if (!ok) {
            return;
        }

        await this.post("/system/service/restart");
    }

    async uninstallService(): Promise<void> {
        const ok = confirm("WebFlex OPC Collector 서비스를 삭제할까요?");

        if (!ok) {
            return;
        }

        await this.post("/system/service/uninstall");
    }

    async deployZip(): Promise<void> {
        const input = document.getElementById("collectorZipFile") as HTMLInputElement | null;

        if (!input || !input.files || input.files.length === 0) {
            alert("업로드할 ZIP 파일을 선택하세요.");
            return;
        }

        const file = input.files[0];

        if (!file.name.toLowerCase().endsWith(".zip")) {
            alert("ZIP 파일만 업로드할 수 있습니다.");
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
                alert(data.message ?? "배포에 실패했습니다.");
            }

            await this.loadStatus();
        } catch (e) {
            console.error(e);
            alert("ZIP 배포 중 오류가 발생했습니다.");
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

    writeResult(data: unknown): void {
        $("#resultBox").text(JSON.stringify(data, null, 2));
    }

    writeError(error: unknown): void {
        if (error instanceof Error) {
            $("#resultBox").text(error.message);
            return;
        }

        $("#resultBox").text(String(error));
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