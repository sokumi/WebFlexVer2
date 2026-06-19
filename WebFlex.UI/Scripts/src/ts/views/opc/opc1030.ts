type OpcClientUiOptions = {
    applicationName: string;
    applicationUri: string;
    productUri: string;
    applicationType: string;
    disableHiResClock: boolean;

    applicationCertificateStoreType: string;
    applicationCertificateStorePath: string;
    applicationCertificateSubjectName: string;
    applicationCertificateThumbprint: string;

    trustedPeerCertificatesStoreType: string;
    trustedPeerCertificatesStorePath: string;
    trustedIssuerCertificatesStoreType: string;
    trustedIssuerCertificatesStorePath: string;
    rejectedCertificateStoreType: string;
    rejectedCertificateStorePath: string;

    autoAcceptUntrustedCertificates: boolean;
    rejectSHA1SignedCertificates: boolean;
    rejectUnknownRevocationStatus: boolean;
    minimumCertificateKeySize: number;
    addAppCertToTrustedStore: boolean;
    suppressNonceValidationErrors: boolean;
    sendCertificateChain: boolean;

    operationTimeout: number;
    maxStringLength: number;
    maxByteStringLength: number;
    maxArrayLength: number;
    maxMessageSize: number;
    maxBufferSize: number;
    channelLifetime: number;
    securityTokenLifetime: number;

    defaultSessionTimeout: number;
    minSubscriptionLifetime: number;
    wellKnownDiscoveryUrls: string;
    discoveryServers: string;
    endpointCacheFilePath: string;

    endpointUrl: string;
    useSecurity: boolean;
    securityPolicyUri: string;
    messageSecurityMode: string;
    transportProfileUri: string;
    endpointSelectionTimeout: number;

    identityType: string;
    userName: string;
    password: string;
    certificateUserStoreType: string;
    certificateUserStorePath: string;
    certificateUserSubjectName: string;

    sessionName: string;
    sessionTimeout: number;
    updateBeforeConnect: boolean;
    checkDomain: boolean;
    preferredLocales: string;

    publishingInterval: number;
    lifetimeCount: number;
    keepAliveCount: number;
    maxNotificationsPerPublish: number;
    publishingEnabled: boolean;
    priority: number;

    attributeId: string;
    monitoringMode: string;
    samplingInterval: number;
    queueSize: number;
    discardOldest: boolean;
    deadbandType: string;
    deadbandValue: number;
    dataChangeTrigger: string;

    browseNodeClassMask: string;
    browseResultMask: string;
    browseMaxReferencesToReturn: number;
    readMaxAge: number;
    readTimestampsToReturn: string;

    enableSessionKeepAlive: boolean;
    keepAliveInterval: number;
    reconnectPeriod: number;
    maxReconnectAttempts: number;
};

export default class Page {
    private readonly storageKey = "webflex.opc1030.clientOptions";

    init(): void {
        $("#btnLoadDefaults").on("click", () => this.loadDefaults());
        $("#btnLoadSaved").on("click", () => this.loadSaved());
        $("#btnSaveLocal").on("click", () => this.saveLocal());
        $("#btnClearLocal").on("click", () => this.clearLocal());

        this.loadSavedOrDefaults();
    }

    private loadSavedOrDefaults(): void {
        const saved = this.getSaved();

        if (saved != null) {
            this.setForm(saved);
            this.setStatus("저장된 임시 옵션을 불러왔습니다.");
            return;
        }

        this.setForm(this.getDefaultOptions());
        this.setStatus("기본값을 불러왔습니다.");
    }

    private loadDefaults(): void {
        this.setForm(this.getDefaultOptions());
        this.setStatus("기본값을 불러왔습니다.");
    }

    private loadSaved(): void {
        const saved = this.getSaved();

        if (saved == null) {
            alert("임시 저장된 옵션이 없습니다.");
            return;
        }

        this.setForm(saved);
        this.setStatus("저장된 임시 옵션을 불러왔습니다.");
    }

    private saveLocal(): void {
        const data = this.getForm();

        localStorage.setItem(this.storageKey, JSON.stringify(data));

        this.setStatus("브라우저 localStorage에 임시 저장했습니다. Collector에는 적용되지 않았습니다.");
        alert("임시 저장되었습니다. 아직 OPC Collector 서비스에는 적용되지 않습니다.");
    }

    private clearLocal(): void {
        if (!confirm("임시 저장된 OPC Client 옵션을 삭제할까요?")) {
            return;
        }

        localStorage.removeItem(this.storageKey);
        this.setStatus("임시 저장값을 삭제했습니다.");
    }

    private getSaved(): OpcClientUiOptions | null {
        const raw = localStorage.getItem(this.storageKey);

        if (raw == null || raw === "") {
            return null;
        }

        try {
            return JSON.parse(raw) as OpcClientUiOptions;
        } catch {
            return null;
        }
    }

    private getDefaultOptions(): OpcClientUiOptions {
        return {
            applicationName: "WebFlexOpcCollector",
            applicationUri: "urn:localhost:WebFlexOpcCollector",
            productUri: "WebFlexOpcCollector",
            applicationType: "Client",
            disableHiResClock: true,

            applicationCertificateStoreType: "Directory",
            applicationCertificateStorePath: "pki/own",
            applicationCertificateSubjectName: "WebFlexOpcCollector",
            applicationCertificateThumbprint: "",

            trustedPeerCertificatesStoreType: "Directory",
            trustedPeerCertificatesStorePath: "pki/trusted",
            trustedIssuerCertificatesStoreType: "Directory",
            trustedIssuerCertificatesStorePath: "pki/issuer",
            rejectedCertificateStoreType: "Directory",
            rejectedCertificateStorePath: "pki/rejected",

            autoAcceptUntrustedCertificates: true,
            rejectSHA1SignedCertificates: false,
            rejectUnknownRevocationStatus: false,
            minimumCertificateKeySize: 1024,
            addAppCertToTrustedStore: false,
            suppressNonceValidationErrors: true,
            sendCertificateChain: false,

            operationTimeout: 600000,
            maxStringLength: 2147483647,
            maxByteStringLength: 2147483647,
            maxArrayLength: 65535,
            maxMessageSize: 419430400,
            maxBufferSize: 65535,
            channelLifetime: -1,
            securityTokenLifetime: -1,

            defaultSessionTimeout: 60000,
            minSubscriptionLifetime: 10000,
            wellKnownDiscoveryUrls: "",
            discoveryServers: "",
            endpointCacheFilePath: "",

            endpointUrl: "opc.tcp://127.0.0.1:49320",
            useSecurity: false,
            securityPolicyUri: "",
            messageSecurityMode: "",
            transportProfileUri: "",
            endpointSelectionTimeout: 15000,

            identityType: "Anonymous",
            userName: "",
            password: "",
            certificateUserStoreType: "Directory",
            certificateUserStorePath: "pki/user",
            certificateUserSubjectName: "",

            sessionName: "WebFlexOpcCollector",
            sessionTimeout: 60000,
            updateBeforeConnect: false,
            checkDomain: false,
            preferredLocales: "ko-KR,en-US",

            publishingInterval: 1000,
            lifetimeCount: 0,
            keepAliveCount: 0,
            maxNotificationsPerPublish: 0,
            publishingEnabled: true,
            priority: 100,

            attributeId: "Value",
            monitoringMode: "Reporting",
            samplingInterval: 1000,
            queueSize: 1,
            discardOldest: true,
            deadbandType: "None",
            deadbandValue: 0,
            dataChangeTrigger: "StatusValue",

            browseNodeClassMask: "Object,Variable",
            browseResultMask: "All",
            browseMaxReferencesToReturn: 0,
            readMaxAge: 0,
            readTimestampsToReturn: "Both",

            enableSessionKeepAlive: true,
            keepAliveInterval: 5000,
            reconnectPeriod: 10000,
            maxReconnectAttempts: -1
        };
    }

    private setForm(data: OpcClientUiOptions): void {
        this.setText("applicationName", data.applicationName);
        this.setText("applicationUri", data.applicationUri);
        this.setText("productUri", data.productUri);
        this.setText("applicationType", data.applicationType);
        this.setChecked("disableHiResClock", data.disableHiResClock);

        this.setText("applicationCertificateStoreType", data.applicationCertificateStoreType);
        this.setText("applicationCertificateStorePath", data.applicationCertificateStorePath);
        this.setText("applicationCertificateSubjectName", data.applicationCertificateSubjectName);
        this.setText("applicationCertificateThumbprint", data.applicationCertificateThumbprint);

        this.setText("trustedPeerCertificatesStoreType", data.trustedPeerCertificatesStoreType);
        this.setText("trustedPeerCertificatesStorePath", data.trustedPeerCertificatesStorePath);
        this.setText("trustedIssuerCertificatesStoreType", data.trustedIssuerCertificatesStoreType);
        this.setText("trustedIssuerCertificatesStorePath", data.trustedIssuerCertificatesStorePath);
        this.setText("rejectedCertificateStoreType", data.rejectedCertificateStoreType);
        this.setText("rejectedCertificateStorePath", data.rejectedCertificateStorePath);

        this.setChecked("autoAcceptUntrustedCertificates", data.autoAcceptUntrustedCertificates);
        this.setChecked("rejectSHA1SignedCertificates", data.rejectSHA1SignedCertificates);
        this.setChecked("rejectUnknownRevocationStatus", data.rejectUnknownRevocationStatus);
        this.setNumber("minimumCertificateKeySize", data.minimumCertificateKeySize);
        this.setChecked("addAppCertToTrustedStore", data.addAppCertToTrustedStore);
        this.setChecked("suppressNonceValidationErrors", data.suppressNonceValidationErrors);
        this.setChecked("sendCertificateChain", data.sendCertificateChain);

        this.setNumber("operationTimeout", data.operationTimeout);
        this.setNumber("maxStringLength", data.maxStringLength);
        this.setNumber("maxByteStringLength", data.maxByteStringLength);
        this.setNumber("maxArrayLength", data.maxArrayLength);
        this.setNumber("maxMessageSize", data.maxMessageSize);
        this.setNumber("maxBufferSize", data.maxBufferSize);
        this.setNumber("channelLifetime", data.channelLifetime);
        this.setNumber("securityTokenLifetime", data.securityTokenLifetime);

        this.setNumber("defaultSessionTimeout", data.defaultSessionTimeout);
        this.setNumber("minSubscriptionLifetime", data.minSubscriptionLifetime);
        this.setText("wellKnownDiscoveryUrls", data.wellKnownDiscoveryUrls);
        this.setText("discoveryServers", data.discoveryServers);
        this.setText("endpointCacheFilePath", data.endpointCacheFilePath);

        this.setText("endpointUrl", data.endpointUrl);
        this.setChecked("useSecurity", data.useSecurity);
        this.setText("securityPolicyUri", data.securityPolicyUri);
        this.setText("messageSecurityMode", data.messageSecurityMode);
        this.setText("transportProfileUri", data.transportProfileUri);
        this.setNumber("endpointSelectionTimeout", data.endpointSelectionTimeout);

        this.setText("identityType", data.identityType);
        this.setText("userName", data.userName);
        this.setText("password", data.password);
        this.setText("certificateUserStoreType", data.certificateUserStoreType);
        this.setText("certificateUserStorePath", data.certificateUserStorePath);
        this.setText("certificateUserSubjectName", data.certificateUserSubjectName);

        this.setText("sessionName", data.sessionName);
        this.setNumber("sessionTimeout", data.sessionTimeout);
        this.setChecked("updateBeforeConnect", data.updateBeforeConnect);
        this.setChecked("checkDomain", data.checkDomain);
        this.setText("preferredLocales", data.preferredLocales);

        this.setNumber("publishingInterval", data.publishingInterval);
        this.setNumber("lifetimeCount", data.lifetimeCount);
        this.setNumber("keepAliveCount", data.keepAliveCount);
        this.setNumber("maxNotificationsPerPublish", data.maxNotificationsPerPublish);
        this.setChecked("publishingEnabled", data.publishingEnabled);
        this.setNumber("priority", data.priority);

        this.setText("attributeId", data.attributeId);
        this.setText("monitoringMode", data.monitoringMode);
        this.setNumber("samplingInterval", data.samplingInterval);
        this.setNumber("queueSize", data.queueSize);
        this.setChecked("discardOldest", data.discardOldest);
        this.setText("deadbandType", data.deadbandType);
        this.setNumber("deadbandValue", data.deadbandValue);
        this.setText("dataChangeTrigger", data.dataChangeTrigger);

        this.setText("browseNodeClassMask", data.browseNodeClassMask);
        this.setText("browseResultMask", data.browseResultMask);
        this.setNumber("browseMaxReferencesToReturn", data.browseMaxReferencesToReturn);
        this.setNumber("readMaxAge", data.readMaxAge);
        this.setText("readTimestampsToReturn", data.readTimestampsToReturn);

        this.setChecked("enableSessionKeepAlive", data.enableSessionKeepAlive);
        this.setNumber("keepAliveInterval", data.keepAliveInterval);
        this.setNumber("reconnectPeriod", data.reconnectPeriod);
        this.setNumber("maxReconnectAttempts", data.maxReconnectAttempts);
    }

    private getForm(): OpcClientUiOptions {
        return {
            applicationName: this.getText("applicationName"),
            applicationUri: this.getText("applicationUri"),
            productUri: this.getText("productUri"),
            applicationType: this.getText("applicationType"),
            disableHiResClock: this.getChecked("disableHiResClock"),

            applicationCertificateStoreType: this.getText("applicationCertificateStoreType"),
            applicationCertificateStorePath: this.getText("applicationCertificateStorePath"),
            applicationCertificateSubjectName: this.getText("applicationCertificateSubjectName"),
            applicationCertificateThumbprint: this.getText("applicationCertificateThumbprint"),

            trustedPeerCertificatesStoreType: this.getText("trustedPeerCertificatesStoreType"),
            trustedPeerCertificatesStorePath: this.getText("trustedPeerCertificatesStorePath"),
            trustedIssuerCertificatesStoreType: this.getText("trustedIssuerCertificatesStoreType"),
            trustedIssuerCertificatesStorePath: this.getText("trustedIssuerCertificatesStorePath"),
            rejectedCertificateStoreType: this.getText("rejectedCertificateStoreType"),
            rejectedCertificateStorePath: this.getText("rejectedCertificateStorePath"),

            autoAcceptUntrustedCertificates: this.getChecked("autoAcceptUntrustedCertificates"),
            rejectSHA1SignedCertificates: this.getChecked("rejectSHA1SignedCertificates"),
            rejectUnknownRevocationStatus: this.getChecked("rejectUnknownRevocationStatus"),
            minimumCertificateKeySize: this.getNumber("minimumCertificateKeySize"),
            addAppCertToTrustedStore: this.getChecked("addAppCertToTrustedStore"),
            suppressNonceValidationErrors: this.getChecked("suppressNonceValidationErrors"),
            sendCertificateChain: this.getChecked("sendCertificateChain"),

            operationTimeout: this.getNumber("operationTimeout"),
            maxStringLength: this.getNumber("maxStringLength"),
            maxByteStringLength: this.getNumber("maxByteStringLength"),
            maxArrayLength: this.getNumber("maxArrayLength"),
            maxMessageSize: this.getNumber("maxMessageSize"),
            maxBufferSize: this.getNumber("maxBufferSize"),
            channelLifetime: this.getNumber("channelLifetime"),
            securityTokenLifetime: this.getNumber("securityTokenLifetime"),

            defaultSessionTimeout: this.getNumber("defaultSessionTimeout"),
            minSubscriptionLifetime: this.getNumber("minSubscriptionLifetime"),
            wellKnownDiscoveryUrls: this.getText("wellKnownDiscoveryUrls"),
            discoveryServers: this.getText("discoveryServers"),
            endpointCacheFilePath: this.getText("endpointCacheFilePath"),

            endpointUrl: this.getText("endpointUrl"),
            useSecurity: this.getChecked("useSecurity"),
            securityPolicyUri: this.getText("securityPolicyUri"),
            messageSecurityMode: this.getText("messageSecurityMode"),
            transportProfileUri: this.getText("transportProfileUri"),
            endpointSelectionTimeout: this.getNumber("endpointSelectionTimeout"),

            identityType: this.getText("identityType"),
            userName: this.getText("userName"),
            password: this.getText("password"),
            certificateUserStoreType: this.getText("certificateUserStoreType"),
            certificateUserStorePath: this.getText("certificateUserStorePath"),
            certificateUserSubjectName: this.getText("certificateUserSubjectName"),

            sessionName: this.getText("sessionName"),
            sessionTimeout: this.getNumber("sessionTimeout"),
            updateBeforeConnect: this.getChecked("updateBeforeConnect"),
            checkDomain: this.getChecked("checkDomain"),
            preferredLocales: this.getText("preferredLocales"),

            publishingInterval: this.getNumber("publishingInterval"),
            lifetimeCount: this.getNumber("lifetimeCount"),
            keepAliveCount: this.getNumber("keepAliveCount"),
            maxNotificationsPerPublish: this.getNumber("maxNotificationsPerPublish"),
            publishingEnabled: this.getChecked("publishingEnabled"),
            priority: this.getNumber("priority"),

            attributeId: this.getText("attributeId"),
            monitoringMode: this.getText("monitoringMode"),
            samplingInterval: this.getNumber("samplingInterval"),
            queueSize: this.getNumber("queueSize"),
            discardOldest: this.getChecked("discardOldest"),
            deadbandType: this.getText("deadbandType"),
            deadbandValue: this.getNumber("deadbandValue"),
            dataChangeTrigger: this.getText("dataChangeTrigger"),

            browseNodeClassMask: this.getText("browseNodeClassMask"),
            browseResultMask: this.getText("browseResultMask"),
            browseMaxReferencesToReturn: this.getNumber("browseMaxReferencesToReturn"),
            readMaxAge: this.getNumber("readMaxAge"),
            readTimestampsToReturn: this.getText("readTimestampsToReturn"),

            enableSessionKeepAlive: this.getChecked("enableSessionKeepAlive"),
            keepAliveInterval: this.getNumber("keepAliveInterval"),
            reconnectPeriod: this.getNumber("reconnectPeriod"),
            maxReconnectAttempts: this.getNumber("maxReconnectAttempts")
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