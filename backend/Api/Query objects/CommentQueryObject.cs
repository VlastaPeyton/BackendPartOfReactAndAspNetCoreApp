using Api.Query_objects;

namespace Api.Helpers
{
    // Objasnjeno u StockQueryObject
    public class CommentQueryObject : QueryObjectParent
    {
        public int NebitnoPolje { get; set; } = 0; // Ne treba ovo polje nikad, ali da bih iskoristio parent klasu zelim. 
    }
}
