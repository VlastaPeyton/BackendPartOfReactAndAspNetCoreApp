namespace Api.Exceptions
{
    public class RoleAssignmentException : Exception
    {
        public RoleAssignmentException(string opis) : base($"{opis}") { }
    }
}
