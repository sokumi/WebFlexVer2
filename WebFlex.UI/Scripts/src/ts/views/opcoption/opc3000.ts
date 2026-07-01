type OpcHistoryReadRequest = {
    endpointUrl: string;
    nodeId: string;

    useSecurity: boolean;
    useAnonymous: boolean;
    userName: string;
    password: string;

    startTime: string;
    endTime: string;

    readMode: string;
    returnBounds: boolean;
    readModified: boolean;
    numValuesPerNode: number;
    timestampsToReturn: string;
    releaseContinuationPoints: boolean;
    maxContinuationReads: number;
};

type OpcHistoryValue = {
    sourceTimestamp?: string | null;
    serverTimestamp?: string | null;
    value?: string | null;
    statusCode: string;
};

type OpcHistoryReadResponse = {
    success: boolean;
    message: string;
    endpointUrl: string;
    nodeId: string;
    values: OpcHistoryValue[];
};

export default class Page {
    init(): void {
        $("#btnSearch").on("click", () => this.search());

        this.setDefaultTimeRange();
    }

    private setDefaultTimeRange(): void {
        const end = new Date();
        const start = new Date(end.getTime() - 60 * 60 * 1000);

        $("#startTime").val(this.toDateTimeLocalValue(start));
        $("#endTime").val(this.toDateTimeLocalValue(end));
    }

    private async search(): Promise<void> {
        try {
            const request = this.getRequest();

            if (request.endpointUrl === "") {
                alert("EndpointUrl을 입력해 주세요.");
                return;
            }

            if (request.nodeId === "") {
                alert("NodeId를 입력해 주세요.");
                return;
            }

            this.setStatus("조회 중...");
            $("#lblCount").text("0");
            $("#historyBody").html(`<tr><td colspan="4">조회 중입니다.</td></tr>`);

            const response = await fetch("/api/opc-collector/history/read", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });

            const result = await response.json() as OpcHistoryReadResponse;

            if (!response.ok || !result.success) {
                throw new Error(result.message ?? "History 조회 실패");
            }

            this.render(result.values);
            this.setStatus(result.message);
        } catch (e) {
            console.error(e);
            this.setStatus("조회 실패");
            $("#historyBody").html(`<tr><td colspan="4">조회 실패</td></tr>`);
            alert(e instanceof Error ? e.message : "History 조회 중 오류가 발생했습니다.");
        }
    }

    private getRequest(): OpcHistoryReadRequest {
        return {
            endpointUrl: this.getText("endpointUrl"),
            nodeId: this.getText("nodeId"),

            useSecurity: this.getChecked("useSecurity"),
            useAnonymous: this.getChecked("useAnonymous"),
            userName: this.getText("userName"),
            password: this.getText("password"),

            startTime: this.getText("startTime"),
            endTime: this.getText("endTime"),

            readMode: this.getText("readMode"),
            returnBounds: this.getChecked("returnBounds"),
            readModified: this.getChecked("readModified"),
            numValuesPerNode: this.getNumber("numValuesPerNode"),
            timestampsToReturn: this.getText("timestampsToReturn"),
            releaseContinuationPoints: this.getChecked("releaseContinuationPoints"),
            maxContinuationReads: this.getNumber("maxContinuationReads")
        };
    }

    private render(values: OpcHistoryValue[]): void {
        $("#lblCount").text(String(values.length));

        if (values.length === 0) {
            $("#historyBody").html(`<tr><td colspan="4">조회된 데이터가 없습니다.</td></tr>`);
            return;
        }

        const html = values.map(x => `
            <tr>
                <td>${this.escapeHtml(this.formatDate(x.sourceTimestamp))}</td>
                <td>${this.escapeHtml(this.formatDate(x.serverTimestamp))}</td>
                <td>${this.escapeHtml(x.value ?? "")}</td>
                <td>${this.escapeHtml(x.statusCode ?? "")}</td>
            </tr>
        `);

        $("#historyBody").html(html.join(""));
    }

    private getText(id: string): string {
        return String($(`#${id}`).val() ?? "").trim();
    }

    private getNumber(id: string): number {
        const value = Number($(`#${id}`).val());

        if (Number.isNaN(value)) {
            return 0;
        }

        return value;
    }

    private getChecked(id: string): boolean {
        return Boolean($(`#${id}`).prop("checked"));
    }

    private setStatus(message: string): void {
        $("#lblStatus").text(message);
    }

    private toDateTimeLocalValue(date: Date): string {
        const yyyy = date.getFullYear();
        const mm = String(date.getMonth() + 1).padStart(2, "0");
        const dd = String(date.getDate()).padStart(2, "0");
        const hh = String(date.getHours()).padStart(2, "0");
        const mi = String(date.getMinutes()).padStart(2, "0");

        return `${yyyy}-${mm}-${dd}T${hh}:${mi}`;
    }

    private formatDate(value?: string | null): string {
        if (value == null || value === "") {
            return "";
        }

        const date = new Date(value);

        if (Number.isNaN(date.getTime())) {
            return value;
        }

        const yyyy = date.getFullYear();
        const mm = String(date.getMonth() + 1).padStart(2, "0");
        const dd = String(date.getDate()).padStart(2, "0");
        const hh = String(date.getHours()).padStart(2, "0");
        const mi = String(date.getMinutes()).padStart(2, "0");
        const ss = String(date.getSeconds()).padStart(2, "0");

        return `${yyyy}-${mm}-${dd} ${hh}:${mi}:${ss}`;
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