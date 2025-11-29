namespace Api.Exceptions
{
    public class UserCreatedException : Exception
    {
        public UserCreatedException(string opis) : base($"{opis}") { }
    }
}
