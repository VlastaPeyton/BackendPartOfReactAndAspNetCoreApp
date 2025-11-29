namespace Api.Helpers
{
    /* Koristi se za [FromQuery], jer Axios GET Request iz React moze poslati samo Request Header, a ne i Body, pa kroz Query Parameters (
    posle ? in URL) u FE prosledim ova polja ovim imenima i redosledom. Polje koje ne prosledim u url query, imace defaultnu vrednost koja moze biti
    implicitna kao u Symbol ili explicitna kao  IsDescending
    */
    public class CommentQueryObject
    {
        public string Symbol { get; set; }
        public bool IsDescending { get; set; } = true;

        // https://localhost:port/api/comment/?Symbol=${symbol}&IsDescending=true - Ovako sam u FE uradio jer moram poslati ova polja istog imena i redosleda kao u klasi
    }
}
