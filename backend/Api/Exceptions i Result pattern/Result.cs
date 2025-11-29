namespace Api.Exceptions_i_Result_pattern
{
    // Pogledaj Result pattern.txt

    // Za slucaj kada servisna metoda vraca void
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        private Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Fail(string error) => new Result(false, error);
    }

    // Za slucaj kada servisna metoda vraca non-void
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }
        public T? Value { get; }
        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null); 
        public static Result<T> Fail(string error) => new Result<T>(false, default, error); 
    }
}
