import axios, { AxiosInstance, AxiosRequestConfig } from "axios";

// ─── 공통 응답 타입 ───────────────────────────────────────────
export interface ApiResponse<T = any> {
    success: boolean;
    message?: string;
    data?: T;
}

export interface PagedResponse<T = any> {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    list: T[];
}

// ─── 요청 옵션 타입 ───────────────────────────────────────────
export interface RequestOptions {
    url: string;
    data?: any;
    config?: AxiosRequestConfig;
}

// ─── axios 인스턴스 ───────────────────────────────────────────
const instance: AxiosInstance = axios.create({
    headers: {
        "Content-Type": "application/json"
    }
});

instance.interceptors.response.use(
    response => response,
    error => {
        const message = error.response?.data?.message
            ?? error.response?.statusText
            ?? error.message
            ?? "알 수 없는 오류";
        return Promise.reject(new Error(message));
    }
);

// ─── API 유틸 ─────────────────────────────────────────────────
export const api = {
    async get<T = any>(options: RequestOptions): Promise<ApiResponse<T>> {
        const res = await instance.get<ApiResponse<T>>(options.url, options.config);
        return res.data;
    },

    async post<T = any>(options: RequestOptions): Promise<ApiResponse<T>> {
        const res = await instance.post<ApiResponse<T>>(options.url, options.data, options.config);
        return res.data;
    },

    async put<T = any>(options: RequestOptions): Promise<ApiResponse<T>> {
        const res = await instance.put<ApiResponse<T>>(options.url, options.data, options.config);
        return res.data;
    },

    async delete<T = any>(options: RequestOptions): Promise<ApiResponse<T>> {
        const res = await instance.delete<ApiResponse<T>>(options.url, options.config);
        return res.data;
    }
};