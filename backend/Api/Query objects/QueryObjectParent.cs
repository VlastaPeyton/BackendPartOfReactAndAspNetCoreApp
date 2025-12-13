namespace Api.Query_objects
{   
    /* Pogledaj StockQueryObject. 

      Query objects imaju 2 zajednicka polja (nekad je to mnogo vise polja), pa da ne ponavljam isti kod u svakoj klasi
     i zato pravim njihovu roditeljsku klasu koja sadrzi zajednicka polja.
     */
    public class QueryObjectParent
    {
        // Zbog https://localhost:port/api/stock(comment)/?symbol=tsla 
        public string? Symbol { get; set; } = null;

        // Zbog https://localhost:port/api/stock(comment)/?isdescending=true
        public bool IsDescending { get; set; } = false;
    }
}
