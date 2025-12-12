namespace Api.DTOs.StockDTOs
{
    // Ova polja objekat ima kad u FindStockBySymbolAsync dohvatimo result jer ovo su polja elementa iz niza koji vraca FinancialModelingPrep API
    public class FinancialModelingPrepStockDTO
    {   // FMP API garantuje da ce sva polja biti non-null => default! 
        public string symbol { get; set; } = default!;
        public decimal price { get; set; } = default!;
        public double beta { get; set; } = default!;
        public int volAvg { get; set; } = default!;
        public long mktCap { get; set; } = default!;
        public decimal lastDiv { get; set; } = default!;
        public string range { get; set; } = default!;
        public double changes { get; set; } = default!;
        public string companyName { get; set; } = default!;
        public string currency { get; set; } = default!;
        public string cik { get; set; } = default!;
        public string isin { get; set; } = default!;
        public string cusip { get; set; } = default!;
        public string exchange { get; set; } = default!;
        public string exchangeShortName { get; set; } = default!;
        public string industry { get; set; } = default!;
        public string website { get; set; } = default!;
        public string description { get; set; } = default!;
        public string ceo { get; set; } = default!;
        public string sector { get; set; } = default!;
        public string country { get; set; } = default!;
        public string fullTimeEmployees { get; set; } = default!;
        public string phone { get; set; } = default!;
        public string address { get; set; } = default!;
        public string city { get; set; } = default!;
        public string state { get; set; } = default!;
        public string zip { get; set; } = default!;
        public double dcfDiff { get; set; } = default!;
        public double dcf { get; set; } = default!;
        public string image { get; set; } = default!;
        public string ipoDate { get; set; } = default!;
        public bool defaultImage { get; set; } = default!;
        public bool isEtf { get; set; } = default!;
        public bool isActivelyTrading { get; set; } = default!;
        public bool isAdr { get; set; } = default!;
        public bool isFund { get; set; } = default!;
    }  
}
