type OpcCollectorOptions = {
    enableAutoReload: boolean;
    enableSnapshotSave: boolean;
    enableTimescaleHistorySave: boolean;
    enableCurrentValueSave: boolean;

    reloadIntervalSeconds: number;
    saveIntervalMilliseconds: number;
    maxBatchSize: number;
    writerLogIntervalSeconds: number;

    defaultPublishingIntervalMs: number;
    defaultSamplingIntervalMs: number;
    defaultQueueSize: number;

    subscriptionKeepAliveCount: number;
    subscriptionLifetimeCount: number;
    maxNotificationsPerPublish: number;
    subscriptionPriority: number;
    discardOldest: boolean;

    autoAcceptUntrustedCertificates: boolean;
    rejectSHA1SignedCertificates: boolean;
    minimumCertificateKeySize: number;
    suppressNonceValidationErrors: boolean;
    certificateStoreRootPath: string;

    operationTimeoutMilliseconds: number;
    defaultSessionTimeoutMilliseconds: number;
    minSubscriptionLifetimeMilliseconds: number;

    maxStringLength: number;
    maxByteStringLength: number;
    maxArrayLength: number;
    maxMessageSize: number;
    maxBufferSize: number;
    channelLifetime: number;
    securityTokenLifetime: number;

    disableHiResClock: boolean;

    defaultUseSecurity: boolean;
    defaultUseAnonymous: boolean;
    defaultSecurityPolicy: string;
    defaultSecurityMode: string;
};

type ApiResponse<T> = {
    success: boolean;
    message?: string;
    data?: T;
};

export default class Page {
    init(): void {
        $("#btnLoad").on("click", () => this.load());
        $("#btnSave").on("click", () => this.save());
        $("#btnRestartDevices").on("click", () => this.restartDevices());

        this.load();
    }

    private async load(): Promise<void> {
        try {
            this.setStatus("불러오는 중...");

            const response = await fetch("/api/opc-collector/options");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json() as OpcCollectorOptions;

            this.setForm(data);
            this.setStatus("불러오기 완료");
        } catch (e) {
            console.error(e);
            this.setStatus("불러오기 실패");
            alert("옵션을 불러오는 중 오류가 발생했습니다.");
        }
    }

    private async save(): Promise<void> {
        try {
            if (!confirm("OPC Collector 옵션을 저장할까요? 일부 옵션은 재구독 후 적용됩니다.")) {
                return;
            }

            this.setStatus("저장 중...");

            const request = this.getForm();

            const response = await fetch("/api/opc-collector/options", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const result = await response.json() as ApiResponse<OpcCollectorOptions>;

            if (result.data != null) {
                this.setForm(result.data);
            }

            this.setStatus(result.message ?? "저장 완료");
            alert(result.message ?? "저장되었습니다.");
        } catch (e) {
            console.error(e);
            this.setStatus("저장 실패");
            alert("옵션 저장 중 오류가 발생했습니다.");
        }
    }

    private async restartDevices(): Promise<void> {
        try {
            if (!confirm("전체 디바이스를 재구독할까요?")) {
                return;
            }

            this.setStatus("전체 재구독 요청 중...");

            const response = await fetch("/api/opc-collector/devices/restart", {
                method: "POST"
            });

            if (!response.ok) {
                throw new Error(await response.text());
            }

            this.setStatus("전체 재구독 요청 완료");
        } catch (e) {
            console.error(e);
            this.setStatus("전체 재구독 요청 실패");
            alert("전체 재구독 요청 중 오류가 발생했습니다.");
        }
    }

    private setForm(data: OpcCollectorOptions): void {
        this.setChecked("enableAutoReload", data.enableAutoReload);
        this.setChecked("enableSnapshotSave", data.enableSnapshotSave);
        this.setChecked("enableTimescaleHistorySave", data.enableTimescaleHistorySave);
        this.setChecked("enableCurrentValueSave", data.enableCurrentValueSave);

        this.setNumber("reloadIntervalSeconds", data.reloadIntervalSeconds);
        this.setNumber("saveIntervalMilliseconds", data.saveIntervalMilliseconds);
        this.setNumber("maxBatchSize", data.maxBatchSize);
        this.setNumber("writerLogIntervalSeconds", data.writerLogIntervalSeconds);

        this.setNumber("defaultPublishingIntervalMs", data.defaultPublishingIntervalMs);
        this.setNumber("defaultSamplingIntervalMs", data.defaultSamplingIntervalMs);
        this.setNumber("defaultQueueSize", data.defaultQueueSize);

        this.setNumber("subscriptionKeepAliveCount", data.subscriptionKeepAliveCount);
        this.setNumber("subscriptionLifetimeCount", data.subscriptionLifetimeCount);
        this.setNumber("maxNotificationsPerPublish", data.maxNotificationsPerPublish);
        this.setNumber("subscriptionPriority", data.subscriptionPriority);
        this.setChecked("discardOldest", data.discardOldest);

        this.setChecked("autoAcceptUntrustedCertificates", data.autoAcceptUntrustedCertificates);
        this.setChecked("rejectSHA1SignedCertificates", data.rejectSHA1SignedCertificates);
        this.setNumber("minimumCertificateKeySize", data.minimumCertificateKeySize);
        this.setChecked("suppressNonceValidationErrors", data.suppressNonceValidationErrors);
        this.setText("certificateStoreRootPath", data.certificateStoreRootPath);

        this.setNumber("operationTimeoutMilliseconds", data.operationTimeoutMilliseconds);
        this.setNumber("defaultSessionTimeoutMilliseconds", data.defaultSessionTimeoutMilliseconds);
        this.setNumber("minSubscriptionLifetimeMilliseconds", data.minSubscriptionLifetimeMilliseconds);

        this.setNumber("maxStringLength", data.maxStringLength);
        this.setNumber("maxByteStringLength", data.maxByteStringLength);
        this.setNumber("maxArrayLength", data.maxArrayLength);
        this.setNumber("maxMessageSize", data.maxMessageSize);
        this.setNumber("maxBufferSize", data.maxBufferSize);
        this.setNumber("channelLifetime", data.channelLifetime);
        this.setNumber("securityTokenLifetime", data.securityTokenLifetime);

        this.setChecked("disableHiResClock", data.disableHiResClock);

        this.setChecked("defaultUseSecurity", data.defaultUseSecurity);
        this.setChecked("defaultUseAnonymous", data.defaultUseAnonymous);
        this.setText("defaultSecurityPolicy", data.defaultSecurityPolicy);
        this.setText("defaultSecurityMode", data.defaultSecurityMode);
    }

    private getForm(): OpcCollectorOptions {
        return {
            enableAutoReload: this.getChecked("enableAutoReload"),
            enableSnapshotSave: this.getChecked("enableSnapshotSave"),
            enableTimescaleHistorySave: this.getChecked("enableTimescaleHistorySave"),
            enableCurrentValueSave: this.getChecked("enableCurrentValueSave"),

            reloadIntervalSeconds: this.getNumber("reloadIntervalSeconds"),
            saveIntervalMilliseconds: this.getNumber("saveIntervalMilliseconds"),
            maxBatchSize: this.getNumber("maxBatchSize"),
            writerLogIntervalSeconds: this.getNumber("writerLogIntervalSeconds"),

            defaultPublishingIntervalMs: this.getNumber("defaultPublishingIntervalMs"),
            defaultSamplingIntervalMs: this.getNumber("defaultSamplingIntervalMs"),
            defaultQueueSize: this.getNumber("defaultQueueSize"),

            subscriptionKeepAliveCount: this.getNumber("subscriptionKeepAliveCount"),
            subscriptionLifetimeCount: this.getNumber("subscriptionLifetimeCount"),
            maxNotificationsPerPublish: this.getNumber("maxNotificationsPerPublish"),
            subscriptionPriority: this.getNumber("subscriptionPriority"),
            discardOldest: this.getChecked("discardOldest"),

            autoAcceptUntrustedCertificates: this.getChecked("autoAcceptUntrustedCertificates"),
            rejectSHA1SignedCertificates: this.getChecked("rejectSHA1SignedCertificates"),
            minimumCertificateKeySize: this.getNumber("minimumCertificateKeySize"),
            suppressNonceValidationErrors: this.getChecked("suppressNonceValidationErrors"),
            certificateStoreRootPath: this.getText("certificateStoreRootPath"),

            operationTimeoutMilliseconds: this.getNumber("operationTimeoutMilliseconds"),
            defaultSessionTimeoutMilliseconds: this.getNumber("defaultSessionTimeoutMilliseconds"),
            minSubscriptionLifetimeMilliseconds: this.getNumber("minSubscriptionLifetimeMilliseconds"),

            maxStringLength: this.getNumber("maxStringLength"),
            maxByteStringLength: this.getNumber("maxByteStringLength"),
            maxArrayLength: this.getNumber("maxArrayLength"),
            maxMessageSize: this.getNumber("maxMessageSize"),
            maxBufferSize: this.getNumber("maxBufferSize"),
            channelLifetime: this.getNumber("channelLifetime"),
            securityTokenLifetime: this.getNumber("securityTokenLifetime"),

            disableHiResClock: this.getChecked("disableHiResClock"),

            defaultUseSecurity: this.getChecked("defaultUseSecurity"),
            defaultUseAnonymous: this.getChecked("defaultUseAnonymous"),
            defaultSecurityPolicy: this.getText("defaultSecurityPolicy"),
            defaultSecurityMode: this.getText("defaultSecurityMode")
        };
    }

    private getNumber(id: string): number {
        const value = Number($(`#${id}`).val());

        if (Number.isNaN(value)) {
            return 0;
        }

        return value;
    }

    private setNumber(id: string, value: number): void {
        $(`#${id}`).val(value);
    }

    private getText(id: string): string {
        return String($(`#${id}`).val() ?? "");
    }

    private setText(id: string, value: string): void {
        $(`#${id}`).val(value ?? "");
    }

    private getChecked(id: string): boolean {
        return Boolean($(`#${id}`).prop("checked"));
    }

    private setChecked(id: string, value: boolean): void {
        $(`#${id}`).prop("checked", value);
    }

    private setStatus(message: string): void {
        $("#lblStatus").text(message);
    }
}