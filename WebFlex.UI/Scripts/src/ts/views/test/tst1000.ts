import * as echarts from "echarts";
import { Modal } from "bootstrap";

import { api } from "../../framework/common";
import { WebFlexGrid } from "../../framework/grid/webflexGrid";
import { notify } from "../../framework/notify";

import {
    dateTimeFormatter,
    numberFormatter,
    statusFormatter,
    textFormatter
} from "../../framework/grid/webflexGridFormatters";

type TestSummaryDto = {
    deviceCount: number;
    tagCount: number;
    goodCount: number;
    badCount: number;
};

type TestGridRowDto = {
    id: string;
    groupName: string;
    tagName: string;
    value: number;
    status: number;
    updatedAt: string;
};

type TestSaveRequest = {
    name: string;
    description: string;
    isEnabled: boolean;
};

export default class Page {
     grid: WebFlexGrid<TestGridRowDto> | null = null;
     chart: echarts.ECharts | null = null;
     modal: Modal | null = null;

    public init(): void {
        this.initGrid();
        this.initChart();
        this.initModal();

        this.bindEvents();

        void this.loadSummary();
        void this.loadGrid();
    }

     bindEvents(): void {
        $("#btnReload").on("click", () => {
            void this.reloadAll();
        });

        $("#btnLoadGrid").on("click", () => {
            void this.loadGrid();
        });

        $("#btnClearGrid").on("click", () => {
            void this.grid?.clearData();
        });

        $("#frmTest").on("submit", event => {
            event.preventDefault();
            void this.saveForm();
        });

        $("#btnOpenModal").on("click", () => {
            this.modal?.show();
        });

        $("#btnModalConfirm").on("click", () => {
            this.modal?.hide();
            this.showAlert("모달 확인 버튼을 클릭했습니다.");
        });

        window.addEventListener("resize", () => {
            this.chart?.resize();
        });
    }

     initGrid(): void {
        this.grid = new WebFlexGrid<TestGridRowDto>({
            selector: "#gridTest",
            height: 360,
            pagination: true,
            paginationSize: 10,
            columns: [
                {
                    title: "ID",
                    field: "id",
                    width: 120,
                    formatter: textFormatter
                },
                {
                    title: "그룹",
                    field: "groupName",
                    width: 130,
                    formatter: textFormatter
                },
                {
                    title: "태그명",
                    field: "tagName",
                    minWidth: 160,
                    formatter: textFormatter
                },
                {
                    title: "값",
                    field: "value",
                    width: 120,
                    hozAlign: "right",
                    formatter: numberFormatter
                },
                {
                    title: "상태",
                    field: "status",
                    width: 120,
                    formatter: statusFormatter
                },
                {
                    title: "수정일시",
                    field: "updatedAt",
                    width: 180,
                    formatter: dateTimeFormatter
                }
            ]
        });
    }

     initChart(): void {
        const element = document.getElementById("testChart");

        if (element == null) {
            return;
        }

        this.chart = echarts.init(element);

        this.chart.setOption({
            tooltip: {
                trigger: "axis"
            },
            grid: {
                left: 36,
                right: 18,
                top: 28,
                bottom: 28
            },
            xAxis: {
                type: "category",
                data: ["09:00", "10:00", "11:00", "12:00", "13:00", "14:00"]
            },
            yAxis: {
                type: "value"
            },
            series: [
                {
                    name: "수집량",
                    type: "line",
                    smooth: true,
                    data: [120, 180, 150, 260, 220, 310]
                },
                {
                    name: "오류",
                    type: "bar",
                    data: [3, 4, 2, 6, 5, 7]
                }
            ]
        });
    }

     initModal(): void {
        const element = document.getElementById("testModal");

        if (element == null) {
            return;
        }

        this.modal = new Modal(element);
    }

     async reloadAll(): Promise<void> {
        await this.loadSummary();
        await this.loadGrid();

        this.showAlert("전체 데이터를 다시 조회했습니다.");
    }

     async loadSummary(): Promise<void> {
        try {
            const result = await api.get<TestSummaryDto>({
                url: "/test/main/summary"
            });

            if (!result.success || result.data == null) {
                this.showAlert(result.message ?? "요약 조회 실패");
                return;
            }

            $("#lblDeviceCount").text(result.data.deviceCount.toLocaleString());
            $("#lblTagCount").text(result.data.tagCount.toLocaleString());
            $("#lblGoodCount").text(result.data.goodCount.toLocaleString());
            $("#lblBadCount").text(result.data.badCount.toLocaleString());
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "요약 조회 중 오류가 발생했습니다.");
        }
    }

     async loadGrid(): Promise<void> {
        try {
            const result = await api.get<TestGridRowDto[]>({
                url: "/test/main/grid"
            });

            if (!result.success) {
                this.showAlert(result.message ?? "그리드 조회 실패");
                return;
            }

            await this.grid?.setData(result.data ?? []);

            this.showAlert("그리드 데이터를 조회했습니다.");
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "그리드 조회 중 오류가 발생했습니다.");
        }
    }

     async saveForm(): Promise<void> {
        const request: TestSaveRequest = {
            name: String($("#txtName").val() ?? "").trim(),
            description: String($("#txtDescription").val() ?? "").trim(),
            isEnabled: $("#chkIsEnabled").prop("checked") === true
        };

        if (request.name.length === 0) {
            this.showAlert("이름을 입력해 주세요.");
            $("#txtName").trigger("focus");
            return;
        }

        try {
            const result = await api.post<TestSaveRequest, TestSaveRequest>({
                url: "/test/main/save",
                data: request
            });

            this.showAlert(result.message ?? "저장되었습니다.");
        } catch (e) {
            this.showAlert(e instanceof Error ? e.message : "저장 중 오류가 발생했습니다.");
        }
    }

     showAlert(message: string): void {
        notify.info(message);
    }
}