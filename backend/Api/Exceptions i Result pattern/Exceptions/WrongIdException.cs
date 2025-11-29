namespace Api.Exceptions
{
    public class WrongIdException : Exception
    {
        public WrongIdException(string opis) : base($"{opis}") { }
        
    }
}
