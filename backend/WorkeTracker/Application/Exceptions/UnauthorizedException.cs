namespace Application.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public string Code { get; }
        public object? Params { get; }

        public UnauthorizedException(string code, string message, object? @params = null) : base(message)
        {
            Code = code;
            Params = @params;
        }
    }
}
