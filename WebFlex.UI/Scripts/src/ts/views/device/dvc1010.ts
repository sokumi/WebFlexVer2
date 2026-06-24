import { api } from "../../framework/common";

export default class {
    devices: any[] = [];
    nodes: any[] = [];
    treeNodes: any[] = [];
    selectedNodes: any[] = [];
    selectedDeviceId = "";

    init(): void {
        $("#btnTagSelectAll").on("click", this.btnTagSelectAll_onClick);
        $("#btnTagClearSelect").on("click", this.btnTagClearSelect_onClick);
        $("#btnTagDelete").on("click", this.btnTagDelete_onClick);
        $("#chkTagAll").on("change", this.chkTagAll_onChange);

        $("#btnBrowse").on("click", this.btnBrowse_onClick);
        $("#btnSelectAll").on("click", this.btnSelectAll_onClick);
        $("#btnClearSelect").on("click", this.btnClearSelect_onClick);
        $("#btnSave").on("click", this.btnSave_onClick);

        $("#btnSearch").on("click", this.btnSearch_onClick);

        $("#selDevice").on("change", this.selDevice_onChange);

        this.loadDevices();
    }

    async loadDevices(): Promise<void> {
        try {
            const res = await api.get({ url: "/device/manage/list" });

            this.devices = res.data ?? [];

            const $selDevice = $("#selDevice");
            $selDevice.empty();
            $selDevice.append(`<option value="">디바이스 선택</option>`);

            for (const device of this.devices) {
                $selDevice.append(
                    `<option value="${device.id}">${device.deviceName} (${device.deviceType})</option>`
                );
            }
        } catch (e) {
            alert(e instanceof Error ? e.message : "디바이스 조회 중 오류가 발생했습니다.");
        }
    }

    selDevice_onChange = (): void => {
        this.selectedDeviceId = String($("#selDevice").val() ?? "");

        this.nodes = [];
        this.treeNodes = [];
        this.selectedNodes = [];

        this.renderNodes();
        this.renderSelectedNodes();

        if (this.selectedDeviceId.length > 0) {
            this.loadTags();
        }
    };

    btnBrowse_onClick = async (): Promise<void> => {
        if (!this.selectedDeviceId) {
            alert("디바이스를 선택하세요.");
            return;
        }

        try {
            const onlyCollectable = $("#chkOnlyCollectable").prop("checked") === true;

            const res = await api.get({
                url: `/device/tag/browse?deviceId=${this.selectedDeviceId}&onlyCollectable=${onlyCollectable}`
            });

            if (!res.success) {
                alert(res.message ?? "노드 조회에 실패했습니다.");
                return;
            }

            this.nodes = res.data ?? [];
            this.treeNodes = this.buildTree(this.nodes);
            this.selectedNodes = [];

            this.renderNodes();
            this.renderSelectedNodes();
        } catch (e) {
            alert(e instanceof Error ? e.message : "노드 조회 중 오류가 발생했습니다.");
        }
    };

    btnSelectAll_onClick = (): void => {
        this.selectedNodes = this.nodes.filter(x => x.nodeClass === "Variable");
        this.renderNodes();
        this.renderSelectedNodes();
    };

    btnClearSelect_onClick = (): void => {
        this.selectedNodes = [];
        this.renderNodes();
        this.renderSelectedNodes();
    };

    btnSave_onClick = async (): Promise<void> => {
        if (!this.selectedDeviceId) {
            alert("디바이스를 선택하세요.");
            return;
        }

        const nodes = this.selectedNodes
            .filter(x => x.nodeClass === "Variable")
            .map(x => ({
                nodeId: x.nodeId,
                displayName: x.displayName,
                dataType: x.dataType
            }));

        if (nodes.length === 0) {
            alert("Variable 노드만 태그로 저장할 수 있습니다.");
            return;
        }

        if (!confirm(`${nodes.length}개의 태그를 저장하시겠습니까?`)) {
            return;
        }

        try {
            const res = await api.post({
                url: "/device/tag/insert",
                data: {
                    deviceId: this.selectedDeviceId,
                    nodes
                }
            });

            if (!res.success) {
                alert(res.message ?? "태그 저장에 실패했습니다.");
                return;
            }

            alert(res.message ?? "태그가 저장되었습니다.");

            this.selectedNodes = [];
            this.renderNodes();
            this.renderSelectedNodes();

            await this.loadTags();
        } catch (e) {
            alert(e instanceof Error ? e.message : "태그 저장 중 오류가 발생했습니다.");
        }
    };

    btnSearch_onClick = (): void => {
        this.loadTags();
    };

    async loadTags(): Promise<void> {
        if (!this.selectedDeviceId) return;

        try {
            const res = await api.get({
                url: `/device/tag/list?deviceId=${this.selectedDeviceId}`
            });

            this.renderTags(res.data ?? []);
        } catch (e) {
            alert(e instanceof Error ? e.message : "태그 조회 중 오류가 발생했습니다.");
        }
    }

    buildTree(nodes: any[]): any[] {
        const map = new Map<string, any>();

        for (const node of nodes) {
            map.set(node.nodeId, { ...node, children: [] });
        }

        const roots: any[] = [];

        for (const node of map.values()) {
            if (node.parentNodeId && map.has(node.parentNodeId)) {
                map.get(node.parentNodeId)?.children.push(node);
            } else {
                roots.push(node);
            }
        }

        return roots;
    }

    renderNodes(): void {
        const $area = $("#nodeList");
        $area.empty();

        for (const node of this.treeNodes) {
            $area.append(this.createNodeElement(node, 0));
        }
    }

    createNodeElement(node: any, depth: number): JQuery<HTMLElement> {
        const isVariable = node.nodeClass === "Variable";
        const checked = this.selectedNodes.some(x => x.NODE_ID === node.nodeId);
        const padding = depth * 18;

        const $wrapper = $(`<div class="tree-node"></div>`);

        const $row = $(`
            <div class="${isVariable ? "tree-node-row node-variable" : "tree-node-row node-object"}" style="padding-left:${padding}px">
            </div>
        `);

        if (isVariable) {
            const euText = node.engineeringUnit ? ` [${node.engineeringUnit}]` : "";
            const descText = node.description ? ` — ${node.description}` : "";

            const $label = $(`
                <label>
                    <input type="checkbox" ${checked ? "checked" : ""} />
                    <span>${node.displayName}${euText}</span>
                    <small>${node.nodeId}</small>
                    <span class="node-meta">${node.dataType}${descText} · ${node.accessLevel}</span>
                </label>
            `);

            $label.find("input").on("change", (e) => {
                const checked = $(e.currentTarget).prop("checked") === true;
                this.toggleNode(node, checked);
            });

            $row.append($label);
        } else {
            $row.append(`
                <div class="tree-group">
                    <span>▾ ${node.displayName}</span>
                    <small>${node.nodeId}</small>
                </div>
            `);
        }

        $wrapper.append($row);

        for (const child of node.children) {
            $wrapper.append(this.createNodeElement(child, depth + 1));
        }

        return $wrapper;
    }

    toggleNode(node: any, checked: boolean): void {
        if (checked) {
            if (!this.selectedNodes.some(x => x.NODE_ID === node.nodeId)) {
                this.selectedNodes.push(node);
            }
        } else {
            this.selectedNodes = this.selectedNodes.filter(x => x.NODE_ID !== node.nodeId);
        }

        this.renderSelectedNodes();
    }

    renderSelectedNodes(): void {
        const $body = $("#selectedNodeBody");
        $body.empty();

        for (const node of this.selectedNodes) {
            const $tr = $(`
                <tr>
                    <td>${node.nodeId}</td>
                    <td>${node.displayName}</td>
                    <td>${node.dataType ?? ""}</td>
                </tr>
            `);

            $body.append($tr);
        }
    }

    renderTags(tags: any[]): void {
        const $body = $("#tagBody");
        $body.empty();

        for (const tag of tags) {
            const $tr = $(`
            <tr>
                <td><input type="checkbox" class="chk-tag" data-id="${tag.id}" /></td>
                <td>${tag.id}</td>
                <td>${tag.tagName}</td>
                <td>${tag.nodeId}</td>
                <td>${tag.isCollectEnabled ? "Y" : "N"}</td>
                <td>${tag.saveToDatabase ? "Y" : "N"}</td>
            </tr>
        `);

            $body.append($tr);
        }

        $body.find(".chk-tag").on("change", () => {
            const total = $body.find(".chk-tag").length;
            const checked = $body.find(".chk-tag:checked").length;
            $("#chkTagAll").prop("checked", total > 0 && total === checked);
        });
    }

    chkTagAll_onChange = (): void => {
        const checked = $("#chkTagAll").prop("checked") === true;
        $("#tagBody .chk-tag").prop("checked", checked);
    };

    btnTagSelectAll_onClick = (): void => {
        $("#chkTagAll").prop("checked", true);
        $("#tagBody .chk-tag").prop("checked", true);
    };

    btnTagClearSelect_onClick = (): void => {
        $("#chkTagAll").prop("checked", false);
        $("#tagBody .chk-tag").prop("checked", false);
    };

    btnTagDelete_onClick = async (): Promise<void> => {
        const ids = $("#tagBody .chk-tag:checked")
            .map((_, el) => String($(el).data("id")))
            .get();

        if (ids.length === 0) {
            alert("삭제할 태그를 선택하세요.");
            return;
        }

        if (!confirm(`${ids.length}개의 태그를 삭제하시겠습니까?`)) return;

        try {
            const res = await api.post({
                url: "/device/tag/delete",
                data: { ids }
            });

            if (!res.success) {
                alert(res.message ?? "삭제에 실패했습니다.");
                return;
            }

            alert(res.message ?? "삭제되었습니다.");
            await this.loadTags();
        } catch (e) {
            alert(e instanceof Error ? e.message : "삭제 중 오류가 발생했습니다.");
        }
    };
}