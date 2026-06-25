import axios, {
    AxiosError,
    AxiosInstance,
    AxiosRequestConfig,
    AxiosResponse
} from "axios";

import type { ApiResponse } from "../dtos/apiResponse";

export type PagedResponse<T = unknown> = {
    currentPage: number;
    pageSize: number;
    totalCount: number;
    list: T[];
};

export type RequestOptions<TRequest = unknown> = {
    url: string;
    data?: TRequest;
    config?: AxiosRequestConfig;
};

const instance: AxiosInstance = axios.create({
    headers: {
        "Content-Type": "application/json"
    }
});

instance.interceptors.response.use(
    (response: AxiosResponse) => response,
    (error: AxiosError) => {
        const message =
            error.response?.statusText
            ?? error.message
            ?? "알 수 없는 오류";

        return Promise.reject(new Error(message));
    }
);

export const api = {
    async get<TResponse = unknown>(options: RequestOptions): Promise<ApiResponse<TResponse>> {
        const res = await instance.get<ApiResponse<TResponse>>(options.url, options.config);
        return res.data;
    },

    async post<TResponse = unknown, TRequest = unknown>(
        options: RequestOptions<TRequest>
    ): Promise<ApiResponse<TResponse>> {
        const res = await instance.post<ApiResponse<TResponse>>(options.url, options.data, options.config);
        return res.data;
    },

    async put<TResponse = unknown, TRequest = unknown>(
        options: RequestOptions<TRequest>
    ): Promise<ApiResponse<TResponse>> {
        const res = await instance.put<ApiResponse<TResponse>>(options.url, options.data, options.config);
        return res.data;
    },

    async delete<TResponse = unknown>(options: RequestOptions): Promise<ApiResponse<TResponse>> {
        const res = await instance.delete<ApiResponse<TResponse>>(options.url, options.config);
        return res.data;
    }
};