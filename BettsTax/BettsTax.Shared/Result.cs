namespace BettsTax.Shared
{
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public string ErrorMessage { get; protected set; } = string.Empty;
        public List<string> Errors { get; protected set; } = new List<string>();

        protected Result(bool isSuccess, string errorMessage = "", List<string>? errors = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Errors = errors ?? new List<string>();
        }

        public static Result Success() => new Result(true);
        
        public static Result Failure(string errorMessage) => new Result(false, errorMessage);
        
        public static Result Failure(List<string> errors) => new Result(false, string.Join("; ", errors), errors);
        
        public static Result<T> Success<T>(T value) => new Result<T>(value, true);
        
        public static Result<T> Failure<T>(string errorMessage) => new Result<T>(default!, false, errorMessage);
        
        public static Result<T> Failure<T>(List<string> errors) => new Result<T>(default!, false, string.Join("; ", errors), errors);
    }

    public class Result<T> : Result
    {
        public T Value { get; private set; }

        internal Result(T value, bool isSuccess, string errorMessage = "", List<string>? errors = null)
            : base(isSuccess, errorMessage, errors)
        {
            Value = value;
        }
    }
}