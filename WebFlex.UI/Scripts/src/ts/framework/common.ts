type RequestMethod = "GET" | "POST" | "PUT" | "DELETE";

type RequestOptions = {
    url: string;
    method?: RequestMethod;
    data?: unknown;
};

async function request<T = unknown>(options: RequestOptions): Promise<T> {
    const method = options.method ?? "GET";

    const fetchOptions: RequestInit = {
        method,
        headers: {}
    };

    if (options.data instanceof FormData) {
        fetchOptions.body = options.data;
    } else if (options.data) {
        fetchOptions.headers = {
            "Content-Type": "application/json"
        };

        fetchOptions.body = JSON.stringify(options.data);
    }

    const response = await fetch(options.url, fetchOptions);

    if (!response.ok) {
        const message = await response.text();
        throw new Error(message || "요청 처리 중 오류가 발생했습니다.");
    }

    const contentType = response.headers.get("content-type");

    if (contentType?.includes("application/json")) {
        return await response.json() as T;
    }

    return await response.text() as T;
}

function getEl<T extends HTMLElement = HTMLElement>(id: string): T | null {
    return document.getElementById(id) as T | null;
}

function onClick(id: string, handler: (e: MouseEvent) => void): void {
    getEl(id)?.addEventListener("click", handler);
}

function toFormData(data: Record<string, string | number | boolean | null | undefined>): FormData {
    const formData = new FormData();

    for (const key in data) {
        const value = data[key];

        if (value === null || value === undefined) {
            continue;
        }

        formData.append(key, String(value));
    }

    return formData;
}

function alert(message: string): void {
    window.alert(message);
}

function toast(message: string): void {
    console.log(message);
}

export const common = {
    request,
    getEl,
    onClick,
    toFormData,
    alert,
    toast
};