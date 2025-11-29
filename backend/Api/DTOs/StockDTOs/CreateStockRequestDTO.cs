
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Models;

namespace Api.DTOs.Stock
{
    /*  Objanjeno u StockDTO cemu DTO sluzi. 

        Sve isto kao Stock.cs, samo Id nema jer se on automatski dodeljuje u bazi i nema Comments/Portfolios, jer je to Navigation Property povezan preko PK-FK sa zeljenim vrstama
     Comments/Portfolios tabele, pa se ne pise ovde, jer to nikad ne saljem from FE, vec to je veza sa Comments/Portfolios tabelama.
       
        Mora imati annotations jer ovu klasu koristim za writing to DB endpoint argument pa da ModelState moze da validira polja. */
    public class CreateStockRequestDTO
    {

        [Required]
        [MinLength(5, ErrorMessage = "Symbol must be at least 3 chars")]
        [MaxLength(10, ErrorMessage = "Symbol cannot be over 10 chars")]
        public string Symbol { get; set; } = string.Empty; 
        
        [Required]
        [MinLength(5, ErrorMessage = "CompanyName must be at least 3 chars")]
        [MaxLength(10, ErrorMessage = "CompanyName cannot be over 10 chars")]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [Range(1,1000000000000)]
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
