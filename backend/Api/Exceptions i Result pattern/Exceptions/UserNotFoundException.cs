namespace Api.Exceptions_i_Result_pattern.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string opis) : base(opis) { }
    }
}
