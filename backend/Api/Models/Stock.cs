using System.ComponentModel.DataAnnotations.Schema;
using Api.Value_Objects;

namespace Api.Models
{   // Models folder sluzi za Entity klase jer te klase ce biti tabele u bazi. 

    // Stock table i ovo se koristi samo za EF Core tj bazu. Za endpoints se koristi StockDTO jer Stock se koristi samo u ApplicaitonDbContext kad interaguje sa bazon kroz EF Core.
    
    [Table("Stocks")] // Ime tabele u bazi explicitno definisano 
    public class Stock // Entity jer predstavlja tabelu u bazi
    {
        public int Id { get; set; } // PK and Index by default. Kad Id postane StockId, moracu da uradim kao za CommentId kod Redis jer JSON serialize/deserialize ne zna automatski custom type da prevede

        // Svaki Stock se prepoznaje po symbol (npr Microsoft = MSFT, a Tesla = TSLA)
        public string Symbol { get; set; } = string.Empty; // Ako ne unesem nista, u koloni Symbol bice prazan string 
        public string CompanyName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]  // Tip kolone "Purchase" u tabeli tj koliko max brojeva moze i koliko iza zareza
        public decimal Purchase {  get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Dividend { get; set; }

        public string Industry { get; set; } = string.Empty;

        public long MarketCap { get; set; }

        // U FE, search stock by ticker, and selecting on it, i can leave a comment for that stock
        public List<Comment> Comments { get; set; } = new();   // Navigation property => Include(Comments)
        public List<Portfolio> Portfolios { get; set; } = new List<Portfolio>(); // Navigation property => Include(Comments)

        /* Ovo je 1-to-many Stock-Comment veza, jer Stock ima List<Comment> polje, dok Comment ima Stock i StockId polje, pa EF zakljuci ovu vezu na osnovu imena polja bez da moram pisati u OnModelCreating + Comments postane navigational property.
           Ovo je 1-to-many Stock-Portfolio veza, jer Stock ima List<Comment> polje, dok Portfolio ima Stock i StockId polje, ali EF NECE zakljuciti ovu vezu, jer u Portfolio neam Id polje, vec ga pravim u OnModelCreate pa moram tamo definisati ovu vezu + Portfolios postane navigational property
         */
    }
}
 