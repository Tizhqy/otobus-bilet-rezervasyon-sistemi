namespace OtobusBiletRezervasyon.Services.FlowModels
{
    public enum ServiceResultType
    {
        Success,
        ValidationError,
        NotFound,
        Forbidden,
        Conflict,
        Expired,
        Error
    }

    public class ServiceResult
    {
        public bool Success { get; init; }
        public ServiceResultType Type { get; init; }
        public string Message { get; init; } = string.Empty;

        public static ServiceResult Ok(string message = "") => new()
        {
            Success = true,
            Type = ServiceResultType.Success,
            Message = message
        };

        public static ServiceResult Fail(ServiceResultType type, string message) => new()
        {
            Success = false,
            Type = type,
            Message = message
        };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; init; }

        public static ServiceResult<T> Ok(T data, string message = "") => new()
        {
            Success = true,
            Type = ServiceResultType.Success,
            Message = message,
            Data = data
        };

        public new static ServiceResult<T> Fail(ServiceResultType type, string message) => new()
        {
            Success = false,
            Type = type,
            Message = message
        };
    }
}
