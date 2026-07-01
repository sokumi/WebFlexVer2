type OpcClientUiOptions = Record<string, string | number | boolean>;

type OpcClientOptionView = {
    options: OpcClientUiOptions;
    configuredOptionNames: string[];
    usedOptionNames: string[];
    hasSavedOptions: boolean;
};

export default class Page {
    private optionNames: string[] = [
        "applicationName",
        "applicationUri",
        "productUri",
        "applicationType",
        "disableHiResClock",

        "applicationCertificateStoreType",
        "applicationCertificateStorePath",
        "applicationCertificateSubjectName",
        "applicationCertificateThumbprint",

        "trustedPeerCertificatesStoreType",
        "trustedPeerCertificatesStorePath",
        "trustedIssuerCertificatesStoreType",
        "trustedIssuerCertificatesStorePath",
        "rejectedCertificateStoreType",
        "rejectedCertificateStorePath",

        "autoAcceptUntrustedCertificates",
        "rejectSHA1SignedCertificates",
        "rejectUnknownRevocationStatus",
        "minimumCertificateKeySize",
        "addAppCertToTrustedStore",
        "suppressNonceValidationErrors",
        "sendCertificateChain",

        "operationTimeout",
        "maxStringLength",
        "maxByteStringLength",
        "maxArrayLength",
        "maxMessageSize",
        "maxBufferSize",
        "channelLifetime",
        "securityTokenLifetime",

        "defaultSessionTimeout",
        "minSubscriptionLifetime",
        "wellKnownDiscoveryUrls",
        "discoveryServers",
        "endpointCacheFilePath",

        "endpointUrl",
        "useSecurity",
        "securityPolicyUri",
        "messageSecurityMode",
        "transportProfileUri",
        "endpointSelectionTimeout",

        "identityType",
        "userName",
        "password",
        "certificateUserStoreType",
        "certificateUserStorePath",
        "certificateUserSubjectName",

        "sessionName",
        "sessionTimeout",
        "updateBeforeConnect",
        "checkDomain",
        "preferredLocales",

        "publishingInterval",
        "lifetimeCount",
        "keepAliveCount",
        "maxNotificationsPerPublish",
        "publishingEnabled",
        "priority",

        "attributeId",
        "monitoringMode",
        "samplingInterval",
        "queueSize",
        "discardOldest",
        "deadbandType",
        "deadbandValue",
        "dataChangeTrigger",

        "browseNodeClassMask",
        "browseResultMask",
        "browseMaxReferencesToReturn",
        "readMaxAge",
        "readTimestampsToReturn",

        "enableSessionKeepAlive",
        "keepAliveInterval",
        "reconnectPeriod",
        "maxReconnectAttempts",

        "historyReadMode",
        "historyReturnBounds",
        "historyReadModified",
        "historyNumValuesPerNode",
        "historyTimestampsToReturn",
        "historyReleaseContinuationPoints",
        "historyMaxContinuationReads",
        "historyDefaultRangeMinutes"
    ];

    init(): void {
        $("#btnSave").on("click", () => this.save());

        this.load();
    }

    private async load(): Promise<void> {
        try {
            this.setStatus("옵션 조회 중...");

            const response = await fetch("/api/opc-client-options");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const data = await response.json() as OpcClientOptionView;

            this.setForm(data.options);
            this.applyConfiguredStyle(data);

            this.setStatus(data.hasSavedOptions
                ? "DB 저장 옵션을 불러왔습니다."
                : "DB 저장 옵션이 없어 기본 옵션을 표시합니다.");
        } catch (e) {
            console.error(e);
            this.setStatus("옵션 조회 실패");
            alert("OPC Client 옵션 조회 중 오류가 발생했습니다.");
        }
    }

    private async save(): Promise<void> {
        try {
            if (!confirm("OPC Client 옵션을 DB에 저장할까요? 저장 후 Windows Service 제어 화면에서 서비스를 재시작하면 적용됩니다.")) {
                return;
            }

            this.setStatus("저장 중...");

            const request: OpcClientOptionView = {
                options: this.getForm(),
                configuredOptionNames: this.optionNames.map(x => this.toPascalCase(x)),
                usedOptionNames: [],
                hasSavedOptions: true
            };

            const response = await fetch("/api/opc-client-options", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });

            if (!response.ok) {
                throw new Error(await response.text());
            }

            this.setStatus("저장 완료. Windows Service 제어 화면에서 서비스 재시작 후 적용됩니다.");
            alert("저장되었습니다. Windows Service 제어 화면에서 서비스 재시작 후 적용됩니다.");

            await this.load();
        } catch (e) {
            console.error(e);
            this.setStatus("저장 실패");
            alert("OPC Client 옵션 저장 중 오류가 발생했습니다.");
        }
    }


    private setForm(data: OpcClientUiOptions): void {
        for (const name of this.optionNames) {
            const element = $(`#${name}`);

            if (element.length === 0) {
                continue;
            }

            const value = data[name];

            if (element.attr("type") === "checkbox") {
                element.prop("checked", Boolean(value));
                continue;
            }

            element.val(value as string | number);
        }
    }

    private getForm(): OpcClientUiOptions {
        const result: OpcClientUiOptions = {};

        for (const name of this.optionNames) {
            const element = $(`#${name}`);

            if (element.length === 0) {
                continue;
            }

            if (element.attr("type") === "checkbox") {
                result[name] = Boolean(element.prop("checked"));
                continue;
            }

            if (element.attr("type") === "number") {
                const numberValue = Number(element.val());

                result[name] = Number.isNaN(numberValue)
                    ? 0
                    : numberValue;

                continue;
            }

            result[name] = String(element.val() ?? "");
        }

        return result;
    }

    private applyConfiguredStyle(data: OpcClientOptionView): void {
        $(".opc-option-used").removeClass("opc-option-used");

        const names = new Set<string>();

        for (const name of data.usedOptionNames ?? []) {
            names.add(this.toCamelCase(name));
        }

        for (const name of data.configuredOptionNames ?? []) {
            names.add(this.toCamelCase(name));
        }

        for (const name of names) {
            const element = $(`#${name}`);

            if (element.length === 0) {
                continue;
            }

            element.closest("tr").find("th").addClass("opc-option-used");
        }
    }

    private toPascalCase(value: string): string {
        if (value.length === 0) {
            return value;
        }

        return value.charAt(0).toUpperCase() + value.slice(1);
    }

    private toCamelCase(value: string): string {
        if (value.length === 0) {
            return value;
        }

        return value.charAt(0).toLowerCase() + value.slice(1);
    }

    private setStatus(message: string): void {
        $("#lblStatus").text(message);
    }
}