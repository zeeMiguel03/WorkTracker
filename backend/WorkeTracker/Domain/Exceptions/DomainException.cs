namespace Domain.Exceptions
{
    public class DomainException : Exception
    {
        public string Code { get; }
        public object? Params { get; }

        public DomainException(string code, string message, object? @params = null) : base(message)
        {
            Code = code;
            Params = @params;
        }
    }
}
