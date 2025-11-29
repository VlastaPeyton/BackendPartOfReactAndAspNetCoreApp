namespace Api.Exceptions
{
    public class ResetPasswordException : Exception
    {
        public ResetPasswordException(string opis) : base(opis) { }
    }
}
