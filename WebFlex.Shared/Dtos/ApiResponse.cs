namespace WebFlex.Shared.Dtos.Common;

public class ApiResponse {
    public bool Success { get; set; }

    public string? Message { get; set; }

    public string? ErrorCode { get; set; }

    public object? Data { get; set; }

    public static ApiResponse Ok(string? message = null) {
        return new ApiResponse {
            Success = true,
            Message = message
        };
    }

    public static ApiResponse Ok(object? data, string? message = null) {
        return new ApiResponse {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse Fail(string message, string? errorCode = null) {
        return new ApiResponse {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

public class ApiResponse<T> {
    public bool Success { get; set; }

    public string? Message { get; set; }

    public string? ErrorCode { get; set; }

    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data, string? message = null) {
        return new ApiResponse<T> {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, string? errorCode = null) {
        return new ApiResponse<T> {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}