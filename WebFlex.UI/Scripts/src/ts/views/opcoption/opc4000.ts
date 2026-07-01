type TimescaleOptionTable = {
    schemaName: string;
    tableName: string;
    fullName: string;

    isHypertable: boolean;
    timeColumnName?: string | null;
    chunkTimeInterval?: string | null;

    chunkCount?: number | null;
    totalSize?: string | null;
    tableSize?: string | null;
    indexSize?: string | null;

    retentionEnabled: boolean;
    retentionDropAfter?: string | null;
    retentionScheduleInterval?: string | null;

    compressionEnabled: boolean;
    compressionPolicyEnabled: boolean;
    compressionAfter?: string | null;
    compressionScheduleInterval?: string | null;
    compressionSegmentBy?: string | null;
    compressionOrderBy?: string | null;

    lastError?: string | null;
};

type TimescaleApplyRequest = {
    schemaName: string;
    tableName: string;

    chunkTimeInterval?: string | null;

    retentionEnabled: boolean;
    retentionDropAfter?: string | null;
    retentionScheduleInterval?: string | null;

    compressionEnabled: boolean;
    compressionAfter?: string | null;
    compressionScheduleInterval?: string | null;
    compressionSegmentBy?: string | null;
    compressionOrderBy?: string | null;
};

type TimescaleApplyResult = {
    success: boolean;
    message: string;
    logs: string[];
};

export default class Page {
    private tables: TimescaleOptionTable[] = [];
    private selectedFullName = "";

    init(): void {
        $("#btnSearch").on("click", () => this.search());
        $("#btnApply").on("click", () => this.apply());

        this.search();
    }

    private async search(): Promise<void> {
        try {
            this.setStatus("조회 중...");

            const response = await fetch("/api/timescale-options/tables");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            this.tables = await response.json() as TimescaleOptionTable[];

            this.renderTables();

            if (this.tables.length > 0) {
                const firstHypertable = this.tables.find(x => x.isHypertable) ?? this.tables[0];
                this.selectTable(firstHypertable.fullName);
            }

            this.setStatus("조회 완료");
        } catch (e) {
            console.error(e);
            this.setStatus("조회 실패");
            $("#tableBody").html(`<tr><td colspan="9">조회 실패</td></tr>`);
            alert(e instanceof Error ? e.message : "TimescaleDB 옵션 조회 중 오류가 발생했습니다.");
        }
    }

    private async apply(): Promise<void> {
        try {
            const request = this.getApplyRequest();

            if (request.tableName === "") {
                alert("적용할 테이블을 선택해 주세요.");
                return;
            }

            const selected = this.tables.find(x => x.fullName === this.selectedFullName);

            if (selected == null || !selected.isHypertable) {
                alert("Hypertable에만 TimescaleDB 옵션을 적용할 수 있습니다.");
                return;
            }

            if (request.retentionEnabled && this.isEmpty(request.retentionDropAfter)) {
                alert("Retention 사용 시 RetentionDropAfter 값을 입력해 주세요.");
                return;
            }

            if (request.compressionEnabled && this.isEmpty(request.compressionOrderBy)) {
                request.compressionOrderBy = "time DESC";
            }

            const confirmMessage = [
                "TimescaleDB 옵션을 적용할까요?",
                "",
                `대상: ${request.schemaName}.${request.tableName}`,
                "",
                "주의:",
                "- Retention 설정은 오래된 데이터를 자동 삭제할 수 있습니다.",
                "- Chunk interval 변경은 새 chunk부터 적용됩니다.",
                "- Compression 설정은 TimescaleDB 버전에 따라 실패할 수 있습니다."
            ].join("\n");

            if (!confirm(confirmMessage)) {
                return;
            }

            this.setStatus("적용 중...");
            this.setLog("");

            const response = await fetch("/api/timescale-options/apply", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(request)
            });

            const result = await response.json() as TimescaleApplyResult;

            if (!response.ok || !result.success) {
                throw new Error(result.message ?? "적용 실패");
            }

            this.setLog(result.logs.join("\n"));
            this.setStatus(result.message);
            alert(result.message);

            await this.search();
        } catch (e) {
            console.error(e);
            this.setStatus("적용 실패");
            this.setLog(e instanceof Error ? e.message : "TimescaleDB 옵션 적용 중 오류가 발생했습니다.");
            alert(e instanceof Error ? e.message : "TimescaleDB 옵션 적용 중 오류가 발생했습니다.");
        }
    }

    private renderTables(): void {
        if (this.tables.length === 0) {
            $("#tableBody").html(`<tr><td colspan="9">조회된 테이블이 없습니다.</td></tr>`);
            return;
        }

        const html = this.tables.map(x => `
            <tr data-full-name="${this.escapeHtml(x.fullName)}">
                <td>
                    <button type="button" class="btnSelectTable btn-basic" data-full-name="${this.escapeHtml(x.fullName)}">선택</button>
                </td>
                <td>${this.escapeHtml(x.fullName)}</td>
                <td>${x.isHypertable ? "Y" : "N"}</td>
                <td>${this.escapeHtml(x.timeColumnName ?? "")}</td>
                <td>${this.escapeHtml(x.chunkTimeInterval ?? "")}</td>
                <td>${x.chunkCount ?? ""}</td>
                <td>${this.escapeHtml(x.totalSize ?? "")}</td>
                <td>${x.retentionEnabled ? this.escapeHtml(x.retentionDropAfter ?? "Y") : "N"}</td>
                <td>${x.compressionEnabled ? "Y" : "N"}</td>
            </tr>
        `);

        $("#tableBody").html(html.join(""));

        $(".btnSelectTable").on("click", (e) => {
            const fullName = String($(e.currentTarget).data("full-name") ?? "");
            this.selectTable(fullName);
        });
    }

    private selectTable(fullName: string): void {
        const table = this.tables.find(x => x.fullName === fullName);

        if (table == null) {
            return;
        }

        this.selectedFullName = fullName;

        $("#tableBody tr").removeClass("selected-row");
        $(`#tableBody tr[data-full-name="${this.escapeSelector(fullName)}"]`).addClass("selected-row");

        $("#schemaName").val(table.schemaName);
        $("#tableName").val(table.tableName);
        $("#isHypertable").prop("checked", table.isHypertable);
        $("#timeColumnName").val(table.timeColumnName ?? "");
        $("#currentChunkTimeInterval").val(table.chunkTimeInterval ?? "");
        $("#totalSize").val(table.totalSize ?? "");
        $("#tableSize").val(table.tableSize ?? "");
        $("#indexSize").val(table.indexSize ?? "");

        $("#chunkTimeInterval").val(table.chunkTimeInterval ?? "");

        $("#retentionEnabled").prop("checked", table.retentionEnabled);
        $("#retentionDropAfter").val(table.retentionDropAfter ?? "");
        $("#retentionScheduleInterval").val(table.retentionScheduleInterval ?? "");

        $("#compressionEnabled").prop("checked", table.compressionEnabled);
        $("#compressionAfter").val(table.compressionAfter ?? "");
        $("#compressionScheduleInterval").val(table.compressionScheduleInterval ?? "");
        $("#compressionSegmentBy").val(table.compressionSegmentBy ?? "");
        $("#compressionOrderBy").val(table.compressionOrderBy ?? "time DESC");

        if (!table.isHypertable) {
            this.setStatus("선택한 테이블은 hypertable이 아닙니다. 설정 적용 대상이 아닙니다.");
        } else {
            this.setStatus(`${table.fullName} 선택됨`);
        }
    }

    private getApplyRequest(): TimescaleApplyRequest {
        return {
            schemaName: this.getText("schemaName"),
            tableName: this.getText("tableName"),

            chunkTimeInterval: this.getNullableText("chunkTimeInterval"),

            retentionEnabled: this.getChecked("retentionEnabled"),
            retentionDropAfter: this.getNullableText("retentionDropAfter"),
            retentionScheduleInterval: this.getNullableText("retentionScheduleInterval"),

            compressionEnabled: this.getChecked("compressionEnabled"),
            compressionAfter: this.getNullableText("compressionAfter"),
            compressionScheduleInterval: this.getNullableText("compressionScheduleInterval"),
            compressionSegmentBy: this.getNullableText("compressionSegmentBy"),
            compressionOrderBy: this.getNullableText("compressionOrderBy")
        };
    }

    private getText(id: string): string {
        return String($(`#${id}`).val() ?? "").trim();
    }

    private getNullableText(id: string): string | null {
        const value = this.getText(id);

        return value === ""
            ? null
            : value;
    }

    private getChecked(id: string): boolean {
        return Boolean($(`#${id}`).prop("checked"));
    }

    private isEmpty(value?: string | null): boolean {
        return value == null || value.trim() === "";
    }

    private setStatus(message: string): void {
        $("#lblStatus").text(message);
    }

    private setLog(message: string): void {
        $("#txtLog").val(message);
    }

    private escapeHtml(value: string): string {
        return value
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/\"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    private escapeSelector(value: string): string {
        return value.replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
    }
}