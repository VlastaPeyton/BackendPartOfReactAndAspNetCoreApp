namespace Api.DTOs.StockDTOs
{
    // Ista polja kao UpdateStockRequestDTO, samo nema annoations jer se ovde mapira dto nakon validacije 

    public class UpdateStockCommandModel
    {
        public string Symbol { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal Purchase { get; set; }
        public decimal Dividend { get; set; }
        public string Industry { get; set; } = string.Empty;
        public long MarketCap { get; set; }
    }
}
