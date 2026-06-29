export { api, type ApiResponse, type RequestOptions } from "./api";

export type AnyObject = Record<string, any>;

export function escapeHtml(value: any): string {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/\"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

export function toBool(value: any): boolean {
    return value === true || value === "true" || value === "Y" || value === "1" || value === 1;
}

export function toNumber(value: any, defaultValue = 0): number {
    const result = Number(value);
    return Number.isNaN(result) ? defaultValue : result;
}

export function getValue(selector: string): string {
    return String($(selector).val() ?? "").trim();
}

export function setValue(selector: string, value: any): void {
    $(selector).val(value ?? "");
}

export function getChecked(selector: string): boolean {
    return $(selector).prop("checked") === true;
}

export function setChecked(selector: string, value: any): void {
    $(selector).prop("checked", toBool(value));
}

export function debounce<T extends (...args: any[]) => void>(callback: T, delay = 250): T {
    let timer: number | undefined;

    return function (this: any, ...args: any[]) {
        window.clearTimeout(timer);
        timer = window.setTimeout(() => callback.apply(this, args), delay);
    } as T;
}

export function dispatchLayoutChanged(): void {
    window.dispatchEvent(new CustomEvent("webflex:layoutChanged"));
}