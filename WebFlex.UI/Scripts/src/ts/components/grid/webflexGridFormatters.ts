import type { CellComponent } from "tabulator-tables";

export function textFormatter(cell: CellComponent): string {
    const value = cell.getValue();

    if (value == null || value === "") {
        return "";
    }

    return escapeHtml(String(value));
}

export function numberFormatter(cell: CellComponent): string {
    const value = cell.getValue();

    if (value == null || value === "") {
        return "";
    }

    const numberValue = Number(value);

    if (Number.isNaN(numberValue)) {
        return escapeHtml(String(value));
    }

    return numberValue.toLocaleString();
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
        return escapeHtml(String(value));
    }

    const yyyy = date.getFullYear();
    const mm = String(date.getMonth() + 1).padStart(2, "0");
    const dd = String(date.getDate()).padStart(2, "0");
    const hh = String(date.getHours()).padStart(2, "0");
    const mi = String(date.getMinutes()).padStart(2, "0");
    const ss = String(date.getSeconds()).padStart(2, "0");

    return `${yyyy}-${mm}-${dd} ${hh}:${mi}:${ss}`;
}

export function formatStatus(status: unknown): string {
    if (status == null || status === "") {
        return "";
    }

    if (typeof status === "number") {
        if (status === 0) return "Good";
        if (status === 1) return "Bad";
        return String(status);
    }

    const normalized = String(status).toLowerCase();

    if (normalized === "0" || normalized === "good") {
        return "Good";
    }

    if (normalized === "1" || normalized === "bad") {
        return "Bad";
    }

    return String(status);
}

export function isGoodStatus(status: unknown): boolean {
    if (status == null || status === "") {
        return true;
    }

    if (typeof status === "number") {
        return status === 0;
    }

    const normalized = String(status).toLowerCase();

    return normalized.includes("good") ||
        normalized === "0" ||
        normalized === "0x00000000";
}

export function escapeHtml(value: string): string {
    return value
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/\"/g, "&quot;")
        .replace(/'/g, "&#039;");
}