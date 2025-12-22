namespace Api.DTOs.Keyless_entity
{   
    // Pogledaj SQL Function vs Stored procedure.txt
    public class UserPortfolioTotal
    {
        public string AppUserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalStocks { get; set; }
        public decimal TotalPurchaseValue { get; set; }
    }
}
