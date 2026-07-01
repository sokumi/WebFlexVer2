import { escapeHtml } from "../../framework/common";

export default class Page {
    cards: any[] = [];
    cardMap = new Map<string, any>();
    tagToGroupMap = new Map<string, string>();
    eventSource: EventSource | null = null;

    init(): void {
        void this.loadCards();
        this.connectStream();

        window.addEventListener("beforeunload", () => {
            this.eventSource?.close();
        });
    }

    async loadCards(): Promise<void> {
        try {
            const response = await fetch("/main/card/list");

            if (!response.ok) {
                throw new Error(await response.text());
            }

            const result = await response.json();

            if (!result.success) {
                throw new Error(result.message ?? "카드 데이터 조회 실패");
            }

            this.cards = result.data ?? [];
            this.rebuildMaps();
            this.render();
        } catch (e) {
            console.error(e);
            $("#cardDashboardHost").html(`<div class="wf-dashboard-empty">카드 데이터를 조회하지 못했습니다.</div>`);
        }
    }

    rebuildMaps(): void {
        this.cardMap.clear();
        this.tagToGroupMap.clear();

        for (const card of this.cards) {
            this.cardMap.set(card.groupId, card);

            for (const tag of card.tags ?? []) {
                this.tagToGroupMap.set(tag.tagId, card.groupId);
            }
        }
    }

    render(): void {
        if (this.cards.length === 0) {
            $("#cardDashboardHost").html(`<div class="wf-dashboard-empty">표시할 카드가 없습니다.</div>`);
            return;
        }

        $("#cardDashboardHost").html(this.cards.map(card => this.renderCard(card)).join(""));
        this.refreshIcons();
    }

    renderCard(card: any): string {
        const state = card.state ?? "gray";
        const tags = card.tags ?? [];

        return `
            <article class="wf-dashboard-card is-${escapeHtml(state)}" data-group-id="${escapeHtml(card.groupId)}">
                <header class="wf-dashboard-card-header">
                    <div class="wf-dashboard-card-title-area">
                        <div class="wf-dashboard-card-code-row">
                            <span class="wf-dashboard-card-code">${escapeHtml(card.identityText ?? card.groupId)}</span>
                            <span class="wf-dashboard-state-badge is-${escapeHtml(state)}">
                                ${this.getStateIcon(state)}
                                ${escapeHtml(card.stateText ?? "")}
                            </span>
                        </div>

                        <h3>
                            ${escapeHtml(card.groupName ?? "")}
                            ${card.majorGroupName ? `<small>(${escapeHtml(card.majorGroupName)})</small>` : ""}
                        </h3>

                        <p>${escapeHtml(card.description ?? "")}</p>
                    </div>

                    <div class="wf-dashboard-connect-counts">
                        <span class="is-red">${Number(card.disconnectedCount ?? 0).toLocaleString()}</span>
                        <span class="is-gray">${Number(card.totalCount ?? 0).toLocaleString()}</span>
                        <span class="is-green">${Number(card.connectedCount ?? 0).toLocaleString()}</span>
                    </div>
                </header>

                <div class="wf-dashboard-tag-list">
                    ${tags.length === 0
                ? `<div class="wf-dashboard-tag-empty">대시보드 표시 태그가 없습니다.</div>`
                : tags.map((tag: any) => this.renderTag(tag)).join("")}
                </div>

                <footer class="wf-dashboard-card-footer">
                    <span>
                        ${this.getFooterIcon(state)}
                        ${escapeHtml(card.footerText ?? "")}
                    </span>
                    ${state === "flashRed" || state === "red" ? `<i data-lucide="zap"></i>` : ""}
                </footer>
            </article>
        `;
    }

    renderTag(tag: any): string {
        const state = tag.state ?? "gray";
        const value = tag.cookieValue ?? tag.value ?? "---";

        return `
            <div class="wf-dashboard-tag-row" data-tag-id="${escapeHtml(tag.tagId)}">
                <span class="wf-dashboard-tag-dot is-${escapeHtml(state)}"></span>
                <span class="wf-dashboard-tag-name">${escapeHtml(tag.description ?? tag.tagName ?? tag.nodeId ?? tag.tagId)}</span>
                <strong>${escapeHtml(value)}</strong>
            </div>
        `;
    }

    connectStream(): void {
        this.eventSource?.close();

        const source = new EventSource("/main/list/stream");
        this.eventSource = source;

        source.addEventListener("currentvalue", (event: MessageEvent) => {
            try {
                this.applyCurrentValue(JSON.parse(event.data));
            } catch (e) {
                console.error("card currentvalue parse error", e, event.data);
            }
        });

        source.onerror = e => {
            console.error("card currentvalue stream error", e);
        };
    }

    applyCurrentValue(row: any): void {
        const tagId = row.tagId ?? row.TAG_ID;
        const groupId = this.tagToGroupMap.get(tagId);

        if (groupId == null) {
            return;
        }

        const card = this.cardMap.get(groupId);

        if (card == null) {
            return;
        }

        const tag = (card.tags ?? []).find((x: any) => x.tagId === tagId);

        if (tag == null) {
            return;
        }

        tag.value = row.value ?? row.VALUE;
        tag.cookieValue = row.cookieValue ?? row.COOKIE_VALUE;
        tag.status = row.status ?? row.STATUS;
        tag.state = this.resolveState(row.status ?? row.STATUS);

        card.state = this.resolveCardState(card);
        card.stateText = this.getStateText(card.state);
        card.footerText = this.getFooterText(card.state);

        const $old = $(`[data-group-id="${this.escapeSelectorValue(groupId)}"]`);
        $old.replaceWith(this.renderCard(card));
        this.refreshIcons();
    }

    resolveState(status: any): string {
        const text = String(status ?? "").toLowerCase();

        if (text === "0" || text === "good") {
            return "green";
        }

        return "red";
    }

    resolveCardState(card: any): string {
        const states = (card.tags ?? []).map((x: any) => x.state ?? "gray");

        if (Number(card.totalCount ?? 0) === 0) {
            return "gray";
        }

        if (states.length === 0) {
            return Number(card.connectedCount ?? 0) === 0 ? "gray" : "green";
        }

        return states.sort((a: string, b: string) => this.getPriority(a) - this.getPriority(b))[0];
    }

    getPriority(state: string): number {
        if (state === "gray") return 0;
        if (state === "flashRed") return 1;
        if (state === "red") return 2;
        if (state === "orange") return 3;
        if (state === "green") return 4;
        return 9;
    }

    getStateText(state: string): string {
        if (state === "gray") return "휴면";
        if (state === "flashRed") return "위험";
        if (state === "red") return "점검 필요";
        if (state === "orange") return "주의";
        if (state === "green") return "정상";
        return "확인";
    }

    getFooterText(state: string): string {
        if (state === "gray") return "설비가 가동 중이 아닙니다";
        if (state === "flashRed") return "즉시 점검이 필요한 태그가 있습니다";
        if (state === "red") return "점검이 필요한 태그가 있습니다";
        if (state === "orange") return "주의가 필요한 태그가 있습니다";
        if (state === "green") return "모든 태그 정상 작동 중";
        return "상태 확인이 필요합니다";
    }

    getStateIcon(state: string): string {
        if (state === "green") return `<i data-lucide="check-circle"></i>`;
        if (state === "orange") return `<i data-lucide="triangle-alert"></i>`;
        if (state === "gray") return `<i data-lucide="moon"></i>`;
        return `<i data-lucide="x"></i>`;
    }

    getFooterIcon(state: string): string {
        if (state === "green") return `<i data-lucide="check-circle"></i>`;
        if (state === "gray") return `<i data-lucide="moon"></i>`;
        if (state === "orange") return `<i data-lucide="triangle-alert"></i>`;
        return `<i data-lucide="circle-alert"></i>`;
    }

    escapeSelectorValue(value: string): string {
        return String(value).replace(/\\/g, "\\\\").replace(/"/g, "\\\"");
    }

    refreshIcons(): void {
        (window as any).lucide?.createIcons();
    }
}