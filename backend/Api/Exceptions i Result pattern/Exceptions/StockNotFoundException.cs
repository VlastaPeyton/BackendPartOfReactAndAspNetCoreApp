namespace Api.Exceptions_i_Result_pattern.Exceptions
{
    public class StockNotFoundException : Exception
    {
        public StockNotFoundException(string opis) : base(opis) { }
    }
}
