namespace Api.Query_objects
{   
    /* Pogledaj StockQueryObject. 

      Query objects imaju 2 zajednicka polja (nekad je to mnogo vise polja), pa da ne ponavljam isti kod u svakoj klasi
     i zato pravim njihovu roditeljsku klasu koja sadrzi zajednicka polja.
     */
    public class QueryObjectParent
    {
        public string? Symbol { get; set; } = null;

        public bool IsDescending { get; set; } = false;

        // Pagination defaults:
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
