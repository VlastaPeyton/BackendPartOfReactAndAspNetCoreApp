using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Stock
{
    /* Nema Id jer se on nikad ne sme menjati. I nema Comments/Portfolios, jer je to Navigation Property koji je povezan preko FK-PK sa zeljenim vrstama Comments tabele, pa se nikad ne salje iz FE.

    Objasnjeno u StockDTO cemu DTO sluzi. 
    Mora imati annotations jer ovu klasu koristim za writing to DB Endpoint argument pa da ModelState moze da validira anotated polja. */
    public class UpdateStockRequestDTO  
    {
        [Required]
        [MinLength(5, ErrorMessage = "Symbol must be at least 3 chars")]
        [MaxLength(10, ErrorMessage = "Symbol cannot be over 10 chars")]
        // Ove 3 linije iznad su Data Validation za Symbol kolonu
        public string Symbol { get; set; } = string.Empty; // Ako ne unesem nista, u koloni Symbol bice prazan string 

        [Required]
        [MinLength(5, ErrorMessage = "CompanyName must be at least 3 chars")]
        [MaxLength(10, ErrorMessage = "CompanyName cannot be over 10 chars")]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [Range(1, 1000000000000)]
        public decimal Purchase { get; set; }

        [Required]
        [Range(0.001, 100)]
        public decimal Dividend { get; set; }

        [Required]
        [MaxLength(10, ErrorMessage = "Industry cannot be over 10 chars")]
        public string Industry { get; set; } = string.Empty;

        [Required]
        [Range(1, 5000000000000)]
        public long MarketCap { get; set; }
    }
}
