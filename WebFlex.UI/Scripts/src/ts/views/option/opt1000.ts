import { api, debounce, escapeHtml, getValue, setValue, getChecked, setChecked } from "../../framework/common";
import { notify } from "../../framework/notify";

export default class Page {
    groups: any[] = [];
    tags: any[] = [];
    options: any[] = [];
    selectedGroupId = "";
    selectedTag: any = null;

    init(): void {
        this.bindEvents();
        void this.loadGroups();
    }

    bindEvents(): void {
        $("#txtGroupKeyword").on("input", debounce(() => this.renderGroups(), 200));

        $("#btnSaveTagDisplay").on("click", () => {
            void this.saveTagDisplay();
        });

        $("#btnNewOption").on("click", () => {
            this.openOptionDrawer(null);
        });

        $("#btnCloseDrawer, #btnCancelDrawer, #optionDrawerBackdrop").on("click", () => {
            this.closeDrawer();
        });

        $("#btnSaveOption").on("click", () => {
            void this.saveOption();
        });
    }

    async loadGroups(): Promise<void> {
        try {
            const result = await api.get({ url: "/option/card/tree" });

            if (!result.success) {
                notify.warning(result.message ?? "그룹 조회에 실패했습니다.");
                return;
            }

            this.groups = result.data ?? [];
            this.renderGroups();
        } catch (e) {
            console.error(e);
            notify.warning("그룹 조회 중 오류가 발생했습니다.");
        }
    }

    renderGroups(): void {
        const keyword = getValue("#txtGroupKeyword").toLowerCase();

        const rows = this.groups.filter(x =>
            keyword.length === 0 ||
            String(x.groupId ?? "").toLowerCase().includes(keyword) ||
            String(x.groupName ?? "").toLowerCase().includes(keyword) ||
            String(x.majorGroupName ?? "").toLowerCase().includes(keyword)
        );

        if (rows.length === 0) {
            $("#groupList").html(`<div class="wf-option-empty">조회된 그룹이 없습니다.</div>`);
            return;
        }

        $("#groupList").html(rows.map(group => `
            <button type="button"
                    class="wf-option-group-item ${this.selectedGroupId === group.groupId ? "is-active" : ""}"
                    data-group-id="${escapeHtml(group.groupId)}">
                <span>
                    <strong>${escapeHtml(group.groupName)}</strong>
                    ${group.majorGroupName ? `<em>${escapeHtml(group.majorGroupName)}</em>` : ""}
                </span>
                <small>${Number(group.dashboardTagCount ?? 0)} / ${Number(group.tagCount ?? 0)}</small>
            </button>
        `).join(""));

        $("#groupList").find("[data-group-id]").on("click", event => {
            const groupId = String($(event.currentTarget).data("group-id"));
            void this.selectGroup(groupId);
        });

        this.refreshIcons();
    }

    async selectGroup(groupId: string): Promise<void> {
        this.selectedGroupId = groupId;
        this.selectedTag = null;
        this.options = [];

        const group = this.groups.find(x => x.groupId === groupId);
        $("#lblSelectedGroup").text(group?.groupName ?? "태그 목록");
        $("#lblSelectedTag").text("태그 선택");
        $("#lblSelectedTagSub").text("옵션을 수정할 태그를 선택하세요.");
        $("#tagDisplayForm").addClass("d-none");
        $("#optionList").html(`<div class="wf-option-empty">태그를 선택해 주세요.</div>`);

        this.renderGroups();
        await this.loadTags();
    }

    async loadTags(): Promise<void> {
        try {
            const result = await api.get({
                url: `/option/card/tags?groupId=${encodeURIComponent(this.selectedGroupId)}`
            });

            if (!result.success) {
                notify.warning(result.message ?? "태그 조회에 실패했습니다.");
                return;
            }

            this.tags = result.data ?? [];
            $("#lblTagCount").text(`${this.tags.length}건`);
            this.renderTags();
        } catch (e) {
            console.error(e);
            notify.warning("태그 조회 중 오류가 발생했습니다.");
        }
    }

    renderTags(): void {
        if (this.tags.length === 0) {
            $("#tagList").html(`<div class="wf-option-empty">등록된 태그가 없습니다.</div>`);
            return;
        }

        $("#tagList").html(this.tags.map(tag => `
            <button type="button"
                    class="wf-option-tag-item ${this.selectedTag?.tagId === tag.tagId ? "is-active" : ""}"
                    data-tag-id="${escapeHtml(tag.tagId)}">
                <span class="wf-option-tag-main">
                    <strong>${escapeHtml(tag.description ?? tag.tagName ?? tag.nodeId ?? tag.tagId)}</strong>
                    <em>${escapeHtml(tag.tagId)} / ${escapeHtml(tag.nodeId ?? "")}</em>
                </span>
                <span class="wf-option-tag-meta">
                    ${tag.showOnDashboard ? `<b class="is-on">표시</b>` : `<b>숨김</b>`}
                    <small>${Number(tag.optionCount ?? 0)}개</small>
                </span>
            </button>
        `).join(""));

        $("#tagList").find("[data-tag-id]").on("click", event => {
            const tagId = String($(event.currentTarget).data("tag-id"));
            void this.selectTag(tagId);
        });
    }

    async selectTag(tagId: string): Promise<void> {
        this.selectedTag = this.tags.find(x => x.tagId === tagId);

        if (this.selectedTag == null) {
            return;
        }

        $("#lblSelectedTag").text(this.selectedTag.description ?? this.selectedTag.tagName ?? this.selectedTag.tagId);
        $("#lblSelectedTagSub").text(`${this.selectedTag.tagId} / ${this.selectedTag.nodeId ?? ""}`);
        $("#tagDisplayForm").removeClass("d-none");
        setChecked("#chkShowDashboard", this.selectedTag.showOnDashboard);
        setValue("#numTagSortOrder", this.selectedTag.sortOrder ?? "");

        this.renderTags();
        await this.loadOptions();
    }

    async loadOptions(): Promise<void> {
        if (this.selectedTag == null) return;

        try {
            const result = await api.get({
                url: `/option/card/options?tagId=${encodeURIComponent(this.selectedTag.tagId)}`
            });

            if (!result.success) {
                notify.warning(result.message ?? "옵션 조회에 실패했습니다.");
                return;
            }

            this.options = result.data ?? [];
            this.renderOptions();
        } catch (e) {
            console.error(e);
            notify.warning("옵션 조회 중 오류가 발생했습니다.");
        }
    }

    renderOptions(): void {
        if (this.selectedTag == null) {
            $("#optionList").html(`<div class="wf-option-empty">태그를 선택해 주세요.</div>`);
            return;
        }

        if (this.options.length === 0) {
            $("#optionList").html(`<div class="wf-option-empty">등록된 조건이 없습니다.</div>`);
            return;
        }

        $("#optionList").html(this.options.map(option => `
            <div class="wf-option-rule-item">
                <div>
                    <span class="wf-option-state is-${escapeHtml(option.state)}">${this.getStateText(option.state)}</span>
                    <strong>${escapeHtml(this.getConditionText(option))}</strong>
                    <p>${escapeHtml(option.description ?? "")}</p>
                </div>
                <div class="wf-option-rule-actions">
                    <button type="button" class="wf-row-icon-btn" data-edit-option="${escapeHtml(option.id)}">
                        <i data-lucide="pencil"></i>
                    </button>
                    <button type="button" class="wf-row-icon-btn is-danger" data-delete-option="${escapeHtml(option.id)}">
                        <i data-lucide="trash-2"></i>
                    </button>
                </div>
            </div>
        `).join(""));

        $("#optionList").find("[data-edit-option]").on("click", event => {
            const id = String($(event.currentTarget).data("edit-option"));
            this.openOptionDrawer(this.options.find(x => x.id === id) ?? null);
        });

        $("#optionList").find("[data-delete-option]").on("click", event => {
            const id = String($(event.currentTarget).data("delete-option"));
            void this.deleteOption(id);
        });

        this.refreshIcons();
    }

    async saveTagDisplay(): Promise<void> {
        if (this.selectedTag == null) {
            notify.warning("태그를 선택해 주세요.");
            return;
        }

        const request = {
            ID: this.selectedTag.tagId,
            SHOW_ON_DASHBOARD: getChecked("#chkShowDashboard"),
            SORT_ORDER: this.readNumber("#numTagSortOrder")
        };

        try {
            const result = await api.post({ url: "/option/card/save-tag", data: request });

            if (!result.success) {
                notify.warning(result.message ?? "저장에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "저장되었습니다.");
            await this.loadTags();
        } catch (e) {
            console.error(e);
            notify.warning("표시 설정 저장 중 오류가 발생했습니다.");
        }
    }

    openOptionDrawer(option: any): void {
        if (this.selectedTag == null) {
            notify.warning("태그를 선택해 주세요.");
            return;
        }

        $("#drawerTitle").text(option == null ? "조건 추가" : "조건 수정");
        setValue("#txtOptionId", option?.id ?? "");
        setValue("#selState", option?.state ?? "green");
        setValue("#selMatchType", option?.matchType ?? "Equals");
        setValue("#txtTextValue", option?.textValue ?? "");
        setValue("#numMinValue", option?.minValue ?? "");
        setValue("#numMaxValue", option?.maxValue ?? "");
        setValue("#numOptionSortOrder", option?.sortOrder ?? "");
        setValue("#txtOptionDescription", option?.description ?? "");

        $("#optionDrawerBackdrop").removeClass("d-none");
        $("#optionDrawer").addClass("is-open");
        this.refreshIcons();
    }

    closeDrawer(): void {
        $("#optionDrawerBackdrop").addClass("d-none");
        $("#optionDrawer").removeClass("is-open");
    }

    async saveOption(): Promise<void> {
        if (this.selectedTag == null) {
            notify.warning("태그를 선택해 주세요.");
            return;
        }

        const request = {
            ID: getValue("#txtOptionId"),
            TAG_ID: this.selectedTag.tagId,
            STATE: getValue("#selState"),
            MATCH_TYPE: getValue("#selMatchType"),
            TEXT_VALUE: getValue("#txtTextValue"),
            MIN_VALUE: this.readNumber("#numMinValue"),
            MAX_VALUE: this.readNumber("#numMaxValue"),
            SORT_ORDER: this.readNumber("#numOptionSortOrder"),
            DESCRIPTION: getValue("#txtOptionDescription")
        };

        try {
            const result = await api.post({ url: "/option/card/save-option", data: request });

            if (!result.success) {
                notify.warning(result.message ?? "저장에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "저장되었습니다.");
            this.closeDrawer();
            await this.loadOptions();
            await this.loadTags();
        } catch (e) {
            console.error(e);
            notify.warning("조건 저장 중 오류가 발생했습니다.");
        }
    }

    async deleteOption(id: string): Promise<void> {
        if (!confirm("조건을 삭제하시겠습니까?")) return;

        try {
            const result = await api.post({
                url: "/option/card/delete-option",
                data: { ID: id }
            });

            if (!result.success) {
                notify.warning(result.message ?? "삭제에 실패했습니다.");
                return;
            }

            notify.success(result.message ?? "삭제되었습니다.");
            await this.loadOptions();
            await this.loadTags();
        } catch (e) {
            console.error(e);
            notify.warning("조건 삭제 중 오류가 발생했습니다.");
        }
    }

    getConditionText(option: any): string {
        if (option.matchType === "Always") return "항상 적용";
        if (option.matchType === "Equals") return `값 = ${option.textValue ?? ""}`;
        if (option.matchType === "Contains") return `값 포함 ${option.textValue ?? ""}`;
        if (option.matchType === "NumberRange") return `${option.minValue ?? ""} ~ ${option.maxValue ?? ""}`;
        if (option.matchType === "NumberGte") return `${option.minValue ?? ""} 이상`;
        if (option.matchType === "NumberLte") return `${option.maxValue ?? ""} 이하`;
        if (option.matchType === "BoolEquals") return `Bool = ${option.textValue ?? ""}`;
        return "";
    }

    getStateText(state: string): string {
        if (state === "gray") return "회색";
        if (state === "flashRed") return "반짝임";
        if (state === "red") return "빨강";
        if (state === "orange") return "주황";
        if (state === "green") return "초록";
        return state;
    }

    readNumber(selector: string): number | null {
        const value = getValue(selector);
        if (value.length === 0) return null;
        const numberValue = Number(value);
        return Number.isFinite(numberValue) ? numberValue : null;
    }

    refreshIcons(): void {
        (window as any).lucide?.createIcons();
    }
}