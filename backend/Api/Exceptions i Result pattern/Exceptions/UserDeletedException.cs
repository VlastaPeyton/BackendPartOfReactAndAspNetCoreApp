namespace Api.Exceptions_i_Result_pattern.Exceptions
{
    public class UserDeletedException : Exception
    {
        public UserDeletedException(string opis) : base($"{opis}")
        {
            
        }
    }
}
