namespace Api.Exceptions
{
    public class CommentNotFoundException : Exception
    {
        public CommentNotFoundException(string opis) : base($"{opis}") { }
    }
}
