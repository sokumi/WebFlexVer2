import axios, { AxiosError, AxiosInstance, AxiosRequestConfig, AxiosResponse } from "axios";

export type ApiResponse<T = any> = {
    success: boolean;
    message?: string | null;
    data?: T | null;
};

export type RequestOptions<TRequest = any> = {
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
    (error: AxiosError<any>) => {
        const message =
            error.response?.data?.message
            ?? error.response?.statusText
            ?? error.message
            ?? "알 수 없는 오류가 발생했습니다.";

        return Promise.reject(new Error(message));
    }
);

export const api = {
    async get<TResponse = any>(options: RequestOptions): Promise<ApiResponse<TResponse>> {
        const res = await instance.get<ApiResponse<TResponse>>(options.url, options.config);
        return res.data;
    },

    async post<TResponse = any, TRequest = any>(options: RequestOptions<TRequest>): Promise<ApiResponse<TResponse>> {
        const res = await instance.post<ApiResponse<TResponse>>(options.url, options.data, options.config);
        return res.data;
    },

    async put<TResponse = any, TRequest = any>(options: RequestOptions<TRequest>): Promise<ApiResponse<TResponse>> {
        const res = await instance.put<ApiResponse<TResponse>>(options.url, options.data, options.config);
        return res.data;
    },

    async delete<TResponse = any>(options: RequestOptions): Promise<ApiResponse<TResponse>> {
        const res = await instance.delete<ApiResponse<TResponse>>(options.url, options.config);
        return res.data;
    }
};