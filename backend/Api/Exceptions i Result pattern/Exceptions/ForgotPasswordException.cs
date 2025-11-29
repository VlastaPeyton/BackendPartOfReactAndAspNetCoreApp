namespace Api.Exceptions
{
    public class ForgotPasswordException : Exception
    {
        public ForgotPasswordException(string opis) : base(opis) { }
    }
}
