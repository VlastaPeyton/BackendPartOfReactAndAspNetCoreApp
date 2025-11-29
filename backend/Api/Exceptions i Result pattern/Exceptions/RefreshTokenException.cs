namespace Api.Exceptions
{
    public class RefreshTokenException : Exception
    {
        public RefreshTokenException(string opis) : base(opis) { }
    }
}
