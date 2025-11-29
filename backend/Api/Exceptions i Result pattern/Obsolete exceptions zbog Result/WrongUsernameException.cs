namespace Api.Exceptions
{
    public class WrongUsernameException :Exception 
    {
        public WrongUsernameException(string opis) : base($"{opis}") { }
    }
}
