import type { CellComponent } from "tabulator-tables";
import { escapeHtml } from "../../framework/common";

export function textFormatter(cell: CellComponent): string {
    const value = cell.getValue();

    if (value == null || value === "") {
        return "";
    }

    return escapeHtml(value);
}

export function numberFormatter(cell: CellComponent): string {
    const value = cell.getValue();

    if (value == null || value === "") {
        return "";
    }

    const numberValue = Number(value);

    if (Number.isNaN(numberValue)) {
        return escapeHtml(value);
    }

    return numberValue.toLocaleString();
}

export function boolFormatter(cell: CellComponent): string {
    const value = cell.getValue() === true;
    return value
        ? `<span class="wf-bool-dot good">Y</span>`
        : `<span class="wf-bool-dot muted">N</span>`;
}

export function statusFormatter(cell: CellComponent): string {
    const value = cell.getValue();
    const statusText = formatStatus(value);
    const statusClass = isGoodStatus(value) ? "good" : "bad";

    return `<span class="wf-status ${statusClass}">${escapeHtml(statusText)}</span>`;
}

export function dateTimeFormatter(cell: CellComponent): string {
    const value = cell.getValue();

    if (value == null || value === "") {
        return "";
    }

    const date = new Date(String(value));

    if (Number.isNaN(date.getTime())) {
        return escapeHtml(value);
    }

    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, "0");
    const dd = String(date.getDate()).padStart(2, "0");
    const hh = String(date.getHours()).padStart(2, "0");
    const mi = String(date.getMinutes()).padStart(2, "0");
    const ss = String(date.getSeconds()).padStart(2, "0");

    return `${yyyy}-${mm}-${dd} ${hh}:${mi}:${ss}`;
}

export function formatStatus(status: any): string {
    if (status == null || status === "") {
        return "";
    }

    if (status === 0 || status === "0" || String(status).toLowerCase() === "good") {
        return "Good";
    }

    if (status === 1 || status === "1" || String(status).toLowerCase() === "bad") {
        return "Bad";
    }

    return String(status);
}

export function isGoodStatus(status: any): boolean {
    if (status == null || status === "") {
        return true;
    }

    const normalized = String(status).toLowerCase();

    return normalized === "0" ||
        normalized === "good" ||
        normalized === "0x00000000" ||
        normalized.includes("good");
}