import { api, debounce, dispatchLayoutChanged, escapeHtml, getValue, setValue } from "../../framework/common";
import { notify } from "../../framework/notify";

type MajorGroupRow = {
    id: string;
    name: string;
    description?: string | null;
    sortOrder?: number | null;
    groupCount?: number;
    tagCount?: number;
};

type GroupRow = {
    id: string;
    majorGroupId?: string | null;
    majorGroupName?: string | null;
    name: string;
    description?: string | null;
    sortOrder?: number | null;
    tagCount?: number;
};

type TagRow = {
    id: string;
    nodeId?: string | null;
    tagName?: string | null;
    description?: string | null;
    showOnDashboard?: boolean;
    sortOrder?: number | null;
};

type DrawerMode = "major-list" | "major-edit" | "group-edit" | "move-tags";

export default class Page {
    majorGroups: MajorGroupRow[] = [];
    groups: GroupRow[] = [];
    selectedMajorGroupId: string | null = null;
    expandedMajorIds = new Set<string>();
    expandedGroupIds = new Set<string>();
    selectedTagIds = new Set<string>();
    tagMap = new Map<string, TagRow[]>();
    drawerMode: DrawerMode | null = null;
    private tagKeywords = new Map<string, string>();

    public async init(): Promise<void> {
        this.bindEvents();
        await this.loadAll();
        this.createIcons();
    }

    bindEvents(): void {
        $("#btnTreeCollapse").on("click", () => this.toggleTree());
        $("#btnAllGroups").on("click", async () => {
            this.selectedMajorGroupId = null;
            await this.loadGroups();
            this.renderTree();
        });

        $("#txtTreeKeyword").on("input", debounce(() => this.renderTree(), 200));
        $("#txtGroupKeyword").on("input", debounce(() => this.renderGroupTable(), 200));

        $("#btnManageMajor").on("click", () => this.openMajorList());
        $("#btnAddGroup").on("click", () => this.openGroupEdit(null));
        $("#btnAddMajor").on("click", () => this.openMajorEdit(null));

        $("#btnMoveSelected").on("click", () => this.openMoveTags());
        $("#btnDeleteSelected").on("click", () => void this.deleteTags());
        $("#btnClearSelected").on("click", () => {
            this.selectedTagIds.clear();
            this.renderGroupTable();
        });

        $("#btnCloseDrawer, #btnCancelDrawer, #groupDrawerBackdrop").on("click", () => this.closeDrawer());
        $("#btnSaveDrawer").on("click", () => void this.saveDrawer());

        $("#txtDescriptionLong").on("input", () => {
            $("#lblDescriptionCount").text(String(getValue("#txtDescriptionLong").length));
        });
    }

    async loadAll(): Promise<void> {
        await this.loadTree();
        await this.loadGroups();
    }

    async loadTree(): Promise<void> {
        try {
            const result = await api.get({ url: "/device/group-tree" });

            if (!result.success) {
                notify.warning(result.message ?? "그룹 트리 조회에 실패했습니다.");
                return;
            }

            const data = result.data ?? {};
            this.majorGroups = data.majorGroups ?? [];
            this.groups = data.groups ?? [];

            this.expandedMajorIds.clear();
            this.majorGroups.forEach(x => this.expandedMajorIds.add(x.id));

            this.renderTree();
            this.renderGroupSelects();
        } catch (e) {
            console.error(e);
            notify.warning("그룹 트리 조회 중 오류가 발생했습니다.");
        }
    }

    async loadGroups(): Promise<void> {
        try {
            const query = this.selectedMajorGroupId == null
                ? ""
                : `?majorGroupId=${encodeURIComponent(this.selectedMajorGroupId)}`;

            const result = await api.get({ url: `/device/group-list${query}` });

            if (!result.success) {
                notify.warning(result.message ?? "중그룹 조회에 실패했습니다.");
                return;
            }

            this.groups = result.data ?? [];
            this.tagMap.clear();

            this.renderGroupTable();
            this.renderGroupSelects();
        } catch (e) {
            console.error(e);
            notify.warning("중그룹 조회 중 오류가 발생했습니다.");
        }
    }

    renderTree(): void {
        const keyword = getValue("#txtTreeKeyword").toLowerCase();
        const totalTagCount = this.groups.reduce((sum, x) => sum + Number(x.tagCount ?? 0), 0);
        const noneGroups = this.groups.filter(x => !x.majorGroupId);

        let html = `
            <button type="button" class="wf-group-tree-row ${this.selectedMajorGroupId == null ? "is-active" : ""}" data-tree-all>
                <span class="wf-group-tree-left"><i data-lucide="globe"></i><span>전체</span></span>
                <span class="wf-group-tree-count">${totalTagCount}</span>
            </button>
        `;

        this.majorGroups.forEach(major => {
            const childGroups = this.groups.filter(x => x.majorGroupId === major.id);
            const majorMatch = String(major.name ?? "").toLowerCase().includes(keyword);
            const childMatch = childGroups.some(x => String(x.name ?? "").toLowerCase().includes(keyword));

            if (keyword.length > 0 && !majorMatch && !childMatch) return;

            const expanded = this.expandedMajorIds.has(major.id);
            const tagCount = childGroups.reduce((sum, x) => sum + Number(x.tagCount ?? 0), 0);

            html += `
                <button type="button" class="wf-group-tree-row ${this.selectedMajorGroupId === major.id ? "is-active" : ""}" data-major-id="${escapeHtml(major.id)}">
                    <span class="wf-group-tree-left"><i data-lucide="${expanded ? "chevron-down" : "chevron-right"}"></i><i data-lucide="folder"></i><span>${escapeHtml(major.name)}</span></span>
                    <span class="wf-group-tree-count">${tagCount}</span>
                </button>
            `;

            if (!expanded) return;

            childGroups.forEach(group => {
                html += `
                    <button type="button" class="wf-group-tree-row is-child" data-group-id="${escapeHtml(group.id)}">
                        <span class="wf-group-tree-left"><i data-lucide="corner-down-right"></i><i data-lucide="folder"></i><span>${escapeHtml(group.name)}</span></span>
                        <span class="wf-group-tree-count">${group.tagCount ?? 0}</span>
                    </button>
                `;
            });
        });

        if (noneGroups.length > 0) {
            const tagCount = noneGroups.reduce((sum, x) => sum + Number(x.tagCount ?? 0), 0);
            html += `
                <div class="wf-group-tree-divider"></div>
                <button type="button" class="wf-group-tree-row ${this.selectedMajorGroupId === "__none" ? "is-active" : ""}" data-major-id="__none">
                    <span class="wf-group-tree-left"><i data-lucide="folder-question"></i><span>미지정</span></span>
                    <span class="wf-group-tree-count">${tagCount}</span>
                </button>
            `;
        }

        $("#groupTree").html(html);

        $("#groupTree").find("[data-tree-all]").on("click", async () => {
            this.selectedMajorGroupId = null;
            await this.loadGroups();
            this.renderTree();
        });

        $("#groupTree").find("[data-major-id]").on("click", async event => {
            const id = String($(event.currentTarget).data("major-id"));
            this.selectedMajorGroupId = id;

            if (id !== "__none") {
                if (this.expandedMajorIds.has(id)) this.expandedMajorIds.delete(id);
                else this.expandedMajorIds.add(id);
            }

            await this.loadGroups();
            this.renderTree();
        });

        $("#groupTree").find("[data-group-id]").on("click", event => {
            const groupId = String($(event.currentTarget).data("group-id"));
            this.expandedGroupIds.add(groupId);
            this.renderGroupTable();
            void this.loadTags(groupId);
        });

        this.createIcons();
    }

    renderGroupTable(): void {
        const keyword = getValue("#txtGroupKeyword").toLowerCase();
        const rows = this.groups.filter(x => keyword.length === 0 || String(x.name ?? "").toLowerCase().includes(keyword) || String(x.majorGroupName ?? "").toLowerCase().includes(keyword) || String(x.description ?? "").toLowerCase().includes(keyword));

        $("#lblGroupSummary").text(`총 ${rows.length}건`);

        if (rows.length === 0) {
            $("#groupTableBody").html(`<tr><td colspan="7" class="text-center text-muted py-5">표시할 중그룹이 없습니다.</td></tr>`);
            this.syncSelectedState();
            return;
        }

        let html = "";

        rows.forEach(group => {
            const expanded = this.expandedGroupIds.has(group.id);
            html += `
                <tr>
                    <td class="wf-icon-col"><button type="button" class="wf-group-expand-btn" data-toggle-group="${escapeHtml(group.id)}"><i data-lucide="${expanded ? "chevron-down" : "chevron-right"}"></i></button></td>
                    <td><span class="wf-group-badge"><i data-lucide="folder"></i>${escapeHtml(group.majorGroupName ?? "미지정")}</span></td>
                    <td><strong>${escapeHtml(group.name)}</strong>${group.sortOrder != null ? `<span class="wf-order-badge ms-1">#${group.sortOrder}</span>` : ""}</td>
                    <td>${escapeHtml(group.description ?? "")}</td>
                    <td class="wf-number-col">${group.sortOrder ?? ""}</td>
                    <td class="wf-number-col"><span class="wf-tag-count-badge">${group.tagCount ?? 0}</span></td>
                    <td class="wf-action-col">
                        <button type="button" class="wf-row-icon-btn" data-edit-group="${escapeHtml(group.id)}" title="수정"><i data-lucide="pencil"></i></button>
                        <button type="button" class="wf-row-icon-btn is-danger" data-delete-group="${escapeHtml(group.id)}" title="삭제"><i data-lucide="trash-2"></i></button>
                    </td>
                </tr>
            `;

            if (expanded) {
                html += `
        <tr class="wf-tag-panel-row">
            <td colspan="7">
                <div class="wf-tag-panel">
                    <div class="wf-tag-panel-header">
                        <div class="wf-tag-panel-title">
                            <i data-lucide="tag"></i>
                            <span>${escapeHtml(group.name)} 태그 (${group.tagCount ?? 0})</span>
                        </div>

                        <div class="wf-tag-panel-tools">
                            <div class="wf-grid-search wf-tag-search">
                                <i data-lucide="search"></i>
                                <input type="text"
                                       class="form-control"
                                       data-tag-keyword="${escapeHtml(group.id)}"
                                       value="${escapeHtml(this.tagKeywords.get(group.id) ?? "")}"
                                       placeholder="태그 검색..." />
                            </div>
                        </div>
                    </div>

                    <div class="wf-tag-panel-body" id="tagBody_${this.cssEscape(group.id)}">
                        ${this.renderTags(group.id)}
                    </div>
                </div>
            </td>
        </tr>
    `;
            }
        });

        $("#groupTableBody").html(html);

        $("#groupTableBody").find("[data-toggle-group]").on("click", event => {
            event.stopPropagation();
            void this.toggleGroup(String($(event.currentTarget).data("toggle-group")));
        });
        $("#groupTableBody").find("[data-edit-group]").on("click", event => {
            event.stopPropagation();
            const groupId = String($(event.currentTarget).data("edit-group"));
            const group = this.groups.find(x => x.id === groupId);
            if (group) this.openGroupEdit(group);
        });
        $("#groupTableBody").find("[data-delete-group]").on("click", event => {
            event.stopPropagation();
            void this.deleteGroup(String($(event.currentTarget).data("delete-group")));
        });

        $("#groupTableBody").find(".wf-tag-check").on("click", event => event.stopPropagation());
        $("#groupTableBody").find(".wf-tag-check").on("change", event => this.onTagCheckChanged(event));

        $("#groupTableBody").find("[data-tag-keyword]").on("input", event => {
            const groupId = String($(event.currentTarget).data("tag-keyword"));
            const keyword = String($(event.currentTarget).val() ?? "");

            this.tagKeywords.set(groupId, keyword);

            const host = $(`#tagBody_${this.cssEscape(groupId)}`);
            host.html(this.renderTags(groupId));

            host.find(".wf-tag-check").on("click", e => e.stopPropagation());
            host.find(".wf-tag-check").on("change", e => this.onTagCheckChanged(e));
            host.find(".wf-tag-check-all").on("change", e => this.toggleAllTagsInGroup(e, groupId));
        });

        this.expandedGroupIds.forEach(groupId => { if (!this.tagMap.has(groupId)) void this.loadTags(groupId); });
        this.syncSelectedState();
        this.createIcons();
    }

    private toggleAllTagsInGroup(event: JQuery.ChangeEvent, groupId: string): void {
        const checked = $(event.currentTarget).prop("checked") === true;
        const keyword = (this.tagKeywords.get(groupId) ?? "").toLowerCase();
        const tags = this.tagMap.get(groupId) ?? [];

        const rows = tags.filter(tag => {
            if (keyword.length === 0) {
                return true;
            }

            return String(tag.id ?? "").toLowerCase().includes(keyword)
                || String(tag.nodeId ?? "").toLowerCase().includes(keyword)
                || String(tag.tagName ?? "").toLowerCase().includes(keyword)
                || String(tag.description ?? "").toLowerCase().includes(keyword);
        });

        rows.forEach(tag => {
            if (checked) {
                this.selectedTagIds.add(tag.id);
            } else {
                this.selectedTagIds.delete(tag.id);
            }
        });

        const host = $(`#tagBody_${this.cssEscape(groupId)}`);
        host.html(this.renderTags(groupId));

        host.find(".wf-tag-check").on("click", e => e.stopPropagation());
        host.find(".wf-tag-check").on("change", e => this.onTagCheckChanged(e));
        host.find(".wf-tag-check-all").on("change", e => this.toggleAllTagsInGroup(e, groupId));

        this.syncSelectedState();
    }

    private renderTags(groupId: string): string {
        const tags = this.tagMap.get(groupId);

        if (tags == null) {
            return `<div class="wf-tag-empty-text">태그 불러오는 중...</div>`;
        }

        const keyword = (this.tagKeywords.get(groupId) ?? "").toLowerCase();

        const rows = tags.filter(tag => {
            if (keyword.length === 0) {
                return true;
            }

            return String(tag.id ?? "").toLowerCase().includes(keyword)
                || String(tag.nodeId ?? "").toLowerCase().includes(keyword)
                || String(tag.tagName ?? "").toLowerCase().includes(keyword)
                || String(tag.description ?? "").toLowerCase().includes(keyword);
        });

        if (rows.length === 0) {
            return `<div class="wf-tag-empty-text">표시할 태그가 없습니다.</div>`;
        }

        const allChecked = rows.length > 0 && rows.every(tag => this.selectedTagIds.has(tag.id));

        return `
        <table class="wf-group-inner-table">
            <thead>
                <tr>
                    <th class="wf-check-col">
                        <input type="checkbox"
                               class="form-check-input wf-tag-check-all"
                               data-tag-group-id="${escapeHtml(groupId)}"
                               ${allChecked ? "checked" : ""} />
                    </th>
                    <th class="wf-number-col">순서</th>
                    <th>필드명</th>
                    <th>태그명</th>
                    <th>설명</th>
                    <th class="wf-number-col">위젯</th>
                    <th class="wf-number-col">정렬</th>
                </tr>
            </thead>
            <tbody>
                ${rows.map((tag, index) => `
                    <tr>
                        <td class="wf-check-col">
                            <input type="checkbox"
                                   class="form-check-input wf-tag-check"
                                   data-tag-id="${escapeHtml(tag.id)}"
                                   ${this.selectedTagIds.has(tag.id) ? "checked" : ""} />
                        </td>
                        <td class="wf-number-col"><span class="wf-order-badge">${index + 1}</span></td>
                        <td class="wf-tag-code">${escapeHtml(tag.nodeId ?? tag.id)}</td>
                        <td>${escapeHtml(tag.tagName ?? "")}</td>
                        <td>${escapeHtml(tag.description ?? "")}</td>
                        <td class="wf-number-col">${tag.showOnDashboard ? "사용" : "미사용"}</td>
                        <td class="wf-number-col">${tag.sortOrder ?? ""}</td>
                    </tr>
                `).join("")}
            </tbody>
        </table>
    `;
    }

    async toggleGroup(groupId: string): Promise<void> {
        if (this.expandedGroupIds.has(groupId)) {
            this.expandedGroupIds.delete(groupId);
            this.renderGroupTable();
            return;
        }
        this.expandedGroupIds.add(groupId);
        this.renderGroupTable();
        await this.loadTags(groupId);
    }

    async loadTags(groupId: string): Promise<void> {
        const host = $(`#tagBody_${this.cssEscape(groupId)}`);
        try {
            const result = await api.get({ url: `/device/tag-list?groupId=${encodeURIComponent(groupId)}` });
            if (!result.success) {
                this.tagMap.set(groupId, []);
                host.html(`<div class="wf-tag-empty-text">태그 조회 실패</div>`);
                return;
            }
            this.tagMap.set(groupId, result.data ?? []);
            if (host.length > 0) {
                host.html(this.renderTags(groupId));
                host.find(".wf-tag-check").on("click", event => event.stopPropagation());
                host.find(".wf-tag-check").on("change", event => this.onTagCheckChanged(event));
                host.find(".wf-tag-check-all").on("change", event => this.toggleAllTagsInGroup(event, groupId));
            }
            this.syncSelectedState();
        } catch (e) {
            console.error(e);
            this.tagMap.set(groupId, []);
            host.html(`<div class="wf-tag-empty-text">태그 조회 실패</div>`);
        }
    }

    onTagCheckChanged(event: JQuery.ChangeEvent): void {
        const tagId = String($(event.currentTarget).data("tag-id"));
        if ($(event.currentTarget).prop("checked") === true) this.selectedTagIds.add(tagId);
        else this.selectedTagIds.delete(tagId);
        this.syncSelectedState();
    }

    async selectGroupTags(groupId: string, checked: boolean): Promise<void> {
        if (!this.tagMap.has(groupId)) await this.loadTags(groupId);
        const tags = this.tagMap.get(groupId) ?? [];
        tags.forEach(tag => checked ? this.selectedTagIds.add(tag.id) : this.selectedTagIds.delete(tag.id));
        this.renderGroupTable();
    }

    openMajorList(): void {
        this.drawerMode = "major-list";
        $("#drawerTitle").text("대그룹 관리");
        $("#drawerDescription").text("대그룹을 추가, 수정, 삭제할 수 있습니다.");
        $("#majorListPanel").removeClass("d-none");
        $("#groupForm").addClass("d-none");
        $("#moveForm").addClass("d-none");
        $("#btnSaveDrawer").addClass("d-none");
        this.renderMajorList();
        this.openDrawer();
    }

    renderMajorList(): void {
        const html = this.majorGroups.map(major => `
            <div class="wf-major-item"><div><strong>${escapeHtml(major.name)}</strong>${major.sortOrder != null ? `<span class="wf-order-badge">#${major.sortOrder}</span>` : ""}<p>${escapeHtml(major.description ?? "")}</p></div>
            <div><button type="button" class="wf-row-icon-btn" data-edit-major="${escapeHtml(major.id)}"><i data-lucide="pencil"></i></button><button type="button" class="wf-row-icon-btn is-danger" data-delete-major="${escapeHtml(major.id)}"><i data-lucide="trash-2"></i></button></div></div>
        `).join("");
        $("#majorList").html(html || `<div class="wf-tag-empty-text">등록된 대그룹이 없습니다.</div>`);
        $("#majorList").find("[data-edit-major]").on("click", event => this.openMajorEdit(this.majorGroups.find(x => x.id === String($(event.currentTarget).data("edit-major"))) ?? null));
        $("#majorList").find("[data-delete-major]").on("click", event => void this.deleteMajor(String($(event.currentTarget).data("delete-major"))));
        this.createIcons();
    }

    openMajorEdit(major: MajorGroupRow | null): void {
        this.drawerMode = "major-edit";
        $("#drawerTitle").text(major == null ? "대그룹 추가" : "대그룹 수정");
        $("#drawerDescription").text("대그룹 정보를 입력하세요.");
        $("#majorListPanel").addClass("d-none");
        $("#groupForm").removeClass("d-none");
        $("#moveForm").addClass("d-none");
        $("#majorSelectField").addClass("d-none");
        $("#btnSaveDrawer").removeClass("d-none").text("저장");
        setValue("#txtEditType", "major");
        setValue("#txtEditId", major?.id ?? "");
        setValue("#txtGroupName", major?.name ?? "");
        setValue("#numSortOrder", major?.sortOrder ?? "");
        setValue("#txtDescription", major?.description ?? "");
        setValue("#txtDescriptionLong", "");
        $("#lblDescriptionCount").text("0");
        this.openDrawer();
    }

    openGroupEdit(group: GroupRow | null): void {
        this.drawerMode = "group-edit";
        $("#drawerTitle").text(group == null ? "중그룹 추가" : "중그룹 수정");
        $("#drawerDescription").text("중그룹 정보를 입력하세요.");
        $("#majorListPanel").addClass("d-none");
        $("#groupForm").removeClass("d-none");
        $("#moveForm").addClass("d-none");
        $("#majorSelectField").removeClass("d-none");
        $("#btnSaveDrawer").removeClass("d-none").text("저장");
        setValue("#txtEditType", "group");
        setValue("#txtEditId", group?.id ?? "");
        setValue("#selMajorGroup", group?.majorGroupId ?? (this.selectedMajorGroupId === "__none" ? "" : this.selectedMajorGroupId ?? ""));
        setValue("#txtGroupName", group?.name ?? "");
        setValue("#numSortOrder", group?.sortOrder ?? "");
        setValue("#txtDescription", group?.description ?? "");
        setValue("#txtDescriptionLong", "");
        $("#lblDescriptionCount").text("0");
        this.openDrawer();
    }

    openMoveTags(): void {
        if (this.selectedTagIds.size === 0) { notify.warning("이동할 태그를 선택해 주세요."); return; }
        this.drawerMode = "move-tags";
        $("#drawerTitle").text("중그룹 이동");
        $("#drawerDescription").text("선택된 태그를 다른 중그룹으로 이동합니다.");
        $("#majorListPanel").addClass("d-none");
        $("#groupForm").addClass("d-none");
        $("#moveForm").removeClass("d-none");
        $("#btnSaveDrawer").removeClass("d-none").text("이동");
        this.renderGroupSelects();
        this.openDrawer();
    }

    async saveDrawer(): Promise<void> {
        if (this.drawerMode === "major-edit") { await this.saveMajor(); return; }
        if (this.drawerMode === "group-edit") { await this.saveGroup(); return; }
        if (this.drawerMode === "move-tags") await this.moveTags();
    }

    async saveMajor(): Promise<void> {
        const request = { ID: getValue("#txtEditId"), MAJOR_GROUP_NAME: getValue("#txtGroupName"), SORT_ORDER: this.readNumber("#numSortOrder"), DESCRIPTION: this.getDescription() };
        if (request.MAJOR_GROUP_NAME.length === 0) { notify.warning("대그룹명을 입력해 주세요."); return; }
        await this.postAndReload("/device/save-major", request, "대그룹 저장 중 오류가 발생했습니다.");
    }

    async saveGroup(): Promise<void> {
        const request = { ID: getValue("#txtEditId"), MAJOR_GROUP_ID: getValue("#selMajorGroup"), GROUP_NAME: getValue("#txtGroupName"), SORT_ORDER: this.readNumber("#numSortOrder"), DESCRIPTION: this.getDescription() };
        if (request.GROUP_NAME.length === 0) { notify.warning("중그룹명을 입력해 주세요."); return; }
        await this.postAndReload("/device/save-group", request, "중그룹 저장 중 오류가 발생했습니다.");
    }

    async moveTags(): Promise<void> {
        const request = { TAG_IDS: Array.from(this.selectedTagIds), GROUP_ID: getValue("#selMoveGroup") };
        if (request.GROUP_ID.length === 0) { notify.warning("이동할 중그룹을 선택해 주세요."); return; }
        await this.postAndReload("/device/move-tags", request, "태그 이동 중 오류가 발생했습니다.", () => this.selectedTagIds.clear());
    }

    async deleteTags(): Promise<void> {
        if (this.selectedTagIds.size === 0) { notify.warning("삭제할 태그를 선택해 주세요."); return; }
        if (!confirm(`${this.selectedTagIds.size}개의 태그를 삭제하시겠습니까?`)) return;
        await this.postAndReload("/device/delete-tags", { TAG_IDS: Array.from(this.selectedTagIds) }, "태그 삭제 중 오류가 발생했습니다.", () => this.selectedTagIds.clear());
    }

    async deleteMajor(majorId: string): Promise<void> {
        if (!confirm("대그룹을 삭제하시겠습니까? 하위 중그룹은 미지정으로 변경됩니다.")) return;
        await this.postAndReload("/device/delete-major", { ID: majorId }, "대그룹 삭제 중 오류가 발생했습니다.");
    }

    async deleteGroup(groupId: string): Promise<void> {
        if (!confirm("중그룹을 삭제하시겠습니까? 태그가 등록된 중그룹은 삭제할 수 없습니다.")) return;
        await this.postAndReload("/device/delete-group", { ID: groupId }, "중그룹 삭제 중 오류가 발생했습니다.");
    }

    async postAndReload(url: string, data: any, errorMessage: string, afterSuccess?: () => void): Promise<void> {
        try {
            const result = await api.post({ url, data });
            if (!result.success) { notify.warning(result.message ?? "처리 중 오류가 발생했습니다."); return; }
            notify.success(result.message ?? "처리되었습니다.");
            afterSuccess?.();
            this.closeDrawer();
            await this.loadAll();
        } catch (e) {
            console.error(e);
            notify.warning(errorMessage);
        }
    }

    renderGroupSelects(): void {
        $("#selMajorGroup").html([`<option value="">미지정</option>`, ...this.majorGroups.map(x => `<option value="${escapeHtml(x.id)}">${escapeHtml(x.name)}</option>`)].join(""));
        $("#selMoveGroup").html(this.groups.map(x => `<option value="${escapeHtml(x.id)}">${escapeHtml(x.majorGroupName ?? "미지정")} / ${escapeHtml(x.name)}</option>`).join(""));
    }

    openDrawer(): void { $("#groupDrawerBackdrop").removeClass("d-none"); $("#groupDrawer").addClass("is-open"); this.createIcons(); }
    closeDrawer(): void { $("#groupDrawerBackdrop").addClass("d-none"); $("#groupDrawer").removeClass("is-open"); this.drawerMode = null; }
    private toggleTree(show?: boolean): void {
        const layout = $(".wf-group-content");
        const collapsed = show == null
            ? !layout.hasClass("is-tree-collapsed")
            : !show;

        layout.toggleClass("is-tree-collapsed", collapsed);

        const icon = collapsed ? "panel-right-open" : "panel-left-close";
        $("#btnTreeCollapse").html(`<i data-lucide="${icon}"></i>`);

        this.createIcons();
        dispatchLayoutChanged();
    }
    private syncSelectedState(): void {
        const tagCount = this.selectedTagIds.size;

        if (tagCount > 0) {
            $("#lblSelectedCount").text(`${tagCount}개`);
            $("#lblSelectedSummary").text(`${tagCount}개 태그 선택`);
            $("#groupActionBar").removeClass("d-none");
            return;
        }

        $("#lblSelectedSummary").text("선택 없음");
        $("#groupActionBar").addClass("d-none");
    }
    readNumber(selector: string): number | null { const value = getValue(selector); if (value.length === 0) return null; const numberValue = Number(value); return Number.isFinite(numberValue) ? numberValue : null; }
    getDescription(): string | null { const description = getValue("#txtDescription"); const descriptionLong = getValue("#txtDescriptionLong"); if (description.length === 0 && descriptionLong.length === 0) return null; if (descriptionLong.length === 0) return description; if (description.length === 0) return descriptionLong; return `${description}\n${descriptionLong}`; }
    cssEscape(value: string): string { return String(value).replace(/([ #;?%&,.+*~':"!^$[\]()=>|/@])/g, "\\$1"); }
    createIcons(): void { (window as any).lucide?.createIcons(); }
}
