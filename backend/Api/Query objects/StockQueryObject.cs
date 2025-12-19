using Api.Query_objects;

namespace Api.Helpers
{
    /* Koristi se za [FromQuery], jer Axios GET Request iz React moze poslati samo Request Header, a ne i Body, pa kroz Query Parameters (
     posle ? in URL) u FE prosledim ova polja ovim imenima i redosledom (moze lowercase jer ce bindovati bez obzira na lower/uppercase). 
     U FE necu proslediti nikad sva polja odjednom (kao sto vidim u klasi) iako mogu, pa za neprosledjena polja se automatski koristi default 
     vrednost koja moze biti implicitna ili explicitna kao u mom slucaju, ali bolja explicitna.
     */
    public class StockQueryObject : QueryObjectParent
    {
        public int NebitnoPolje { get; set; } = 0;
        // Ne treba ovo polje nikad, ali da bih iskoristio parent klasu zelim, ali u praksi ovde cu imati polja.
    }
}
