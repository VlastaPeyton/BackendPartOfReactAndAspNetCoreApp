namespace Api.Exceptions
{
    public class FMPException : Exception
    {
        public FMPException(string opis) : base($"{opis}") { }
    }
    
}
