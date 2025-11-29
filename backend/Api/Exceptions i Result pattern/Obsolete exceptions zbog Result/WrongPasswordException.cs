namespace Api.Exceptions
{
    public class WrongPasswordException : Exception
    {
        public WrongPasswordException(string opis) : base($"{opis}") { }    
    }
}
