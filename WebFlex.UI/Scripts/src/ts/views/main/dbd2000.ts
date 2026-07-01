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
        const value = tag.cookieValue ?? tag.displayValue ?? tag.value ?? "---";

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

        const previousStatus = tag.status;

        tag.value = row.value ?? row.VALUE;
        tag.rawValue = row.value ?? row.VALUE;
        tag.cookieValue = row.cookieValue ?? row.COOKIE_VALUE;
        tag.displayValue = tag.cookieValue ?? tag.value;
        tag.status = row.status ?? row.STATUS;
        tag.state = this.resolveTagState(tag);

        this.applyConnectionCount(card, previousStatus, tag.status);

        card.state = this.resolveCardState(card);
        card.stateText = this.getStateText(card.state);
        card.footerText = this.getFooterText(card.state);

        this.updateCardElement(card, tag);
    }

    updateCardElement(card: any, tag: any): void {
        const groupId = card.groupId;
        const state = card.state ?? "gray";

        const $card = $(`[data-group-id="${this.escapeSelectorValue(groupId)}"]`);

        if ($card.length === 0) {
            return;
        }

        $card
            .removeClass("is-gray is-flashRed is-red is-orange is-green")
            .addClass(`is-${state}`);

        const $badge = $card.find(".wf-dashboard-state-badge");

        $badge
            .removeClass("is-gray is-flashRed is-red is-orange is-green")
            .addClass(`is-${state}`)
            .html(`
            ${this.getStateIcon(state)}
            ${escapeHtml(card.stateText ?? "")}
        `);

        $card.find(".wf-dashboard-connect-counts .is-red")
            .text(Number(card.disconnectedCount ?? 0).toLocaleString());

        $card.find(".wf-dashboard-connect-counts .is-gray")
            .text(Number(card.totalCount ?? 0).toLocaleString());

        $card.find(".wf-dashboard-connect-counts .is-green")
            .text(Number(card.connectedCount ?? 0).toLocaleString());

        const $tagRow = $card.find(`[data-tag-id="${this.escapeSelectorValue(tag.tagId)}"]`);
        const tagState = tag.state ?? "gray";
        const tagValue = tag.cookieValue ?? tag.displayValue ?? tag.value ?? "---";

        $tagRow.find(".wf-dashboard-tag-dot")
            .removeClass("is-gray is-flashRed is-red is-orange is-green")
            .addClass(`is-${tagState}`);

        $tagRow.find("strong").text(tagValue);

        const $footer = $card.find(".wf-dashboard-card-footer");

        $footer.find("span").html(`
        ${this.getFooterIcon(state)}
        ${escapeHtml(card.footerText ?? "")}
    `);

        const hasZap = state === "flashRed" || state === "red";
        const $zap = $footer.children("i[data-lucide='zap'], svg.lucide-zap");

        if (hasZap && $zap.length === 0) {
            $footer.append(`<i data-lucide="zap"></i>`);
        }

        if (!hasZap) {
            $zap.remove();
        }

        this.refreshIcons();
    }

    applyConnectionCount(card: any, previousStatus: any, currentStatus: any): void {
        const wasGood = this.isGoodStatus(previousStatus);
        const isGood = this.isGoodStatus(currentStatus);

        if (wasGood === isGood) {
            return;
        }

        const connectedCount = Number(card.connectedCount ?? 0);
        const totalCount = Number(card.totalCount ?? 0);

        card.connectedCount = isGood
            ? Math.min(totalCount, connectedCount + 1)
            : Math.max(0, connectedCount - 1);

        card.disconnectedCount = Math.max(0, totalCount - card.connectedCount);
    }

    resolveTagState(tag: any): string {
        if (tag.status == null || tag.status === "") {
            return "gray";
        }

        if (!this.isGoodStatus(tag.status)) {
            return "red";
        }

        const value = tag.cookieValue ?? tag.value ?? "";
        const options = tag.options ?? [];

        const matchedOptions = options
            .filter((option: any) => this.isMatched(option, value))
            .sort((a: any, b: any) => {
                const sortA = Number.isFinite(Number(a.sortOrder)) ? Number(a.sortOrder) : Number.MAX_SAFE_INTEGER;
                const sortB = Number.isFinite(Number(b.sortOrder)) ? Number(b.sortOrder) : Number.MAX_SAFE_INTEGER;

                if (sortA !== sortB) {
                    return sortA - sortB;
                }

                return this.getPriority(a.state) - this.getPriority(b.state);
            });

        return matchedOptions.length > 0
            ? matchedOptions[0].state
            : "green";
    }

    isMatched(option: any, value: any): boolean {
        const matchType = option.matchType ?? "";
        const textValue = option.textValue ?? "";
        const sourceValue = String(value ?? "");

        if (matchType === "Always") {
            return true;
        }

        if (matchType === "Equals") {
            return sourceValue.toLowerCase() === String(textValue).toLowerCase();
        }

        if (matchType === "Contains") {
            return sourceValue.toLowerCase().includes(String(textValue).toLowerCase());
        }

        if (matchType === "BoolEquals") {
            const boolValue = this.parseBool(sourceValue);
            const expectedValue = this.parseBool(textValue);

            return boolValue != null &&
                expectedValue != null &&
                boolValue === expectedValue;
        }

        if (matchType === "NumberRange") {
            const numberValue = this.parseNumber(sourceValue);

            if (numberValue == null) {
                return false;
            }

            const minValue = this.parseNumber(option.minValue);
            const maxValue = this.parseNumber(option.maxValue);

            if (minValue != null && numberValue < minValue) return false;
            if (maxValue != null && numberValue > maxValue) return false;

            return true;
        }

        if (matchType === "NumberGte") {
            const numberValue = this.parseNumber(sourceValue);
            const minValue = this.parseNumber(option.minValue);

            return numberValue != null &&
                minValue != null &&
                numberValue >= minValue;
        }

        if (matchType === "NumberLte") {
            const numberValue = this.parseNumber(sourceValue);
            const maxValue = this.parseNumber(option.maxValue);

            return numberValue != null &&
                maxValue != null &&
                numberValue <= maxValue;
        }

        return false;
    }

    parseNumber(value: any): number | null {
        if (value == null || value === "") {
            return null;
        }

        const match = String(value)
            .replace(/,/g, "")
            .match(/[-+]?\d*\.?\d+/);

        if (match == null) {
            return null;
        }

        const numberValue = Number(match[0]);

        return Number.isFinite(numberValue)
            ? numberValue
            : null;
    }

    parseBool(value: any): boolean | null {
        const text = String(value ?? "").trim().toLowerCase();

        if (text === "true" || text === "1" || text === "y" || text === "yes" || text === "on" || text === "가동") {
            return true;
        }

        if (text === "false" || text === "0" || text === "n" || text === "no" || text === "off" || text === "비가동" || text === "정지") {
            return false;
        }

        return null;
    }

    isGoodStatus(status: any): boolean {
        const text = String(status ?? "").toLowerCase();

        return text === "0" || text === "good";
    }

    resolveCardState(card: any): string {
        const states = (card.tags ?? []).map((x: any) => x.state ?? "gray");

        if (Number(card.totalCount ?? 0) === 0) {
            return "gray";
        }

        if (Number(card.connectedCount ?? 0) === 0) {
            return "gray";
        }

        if (states.length === 0) {
            return "green";
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