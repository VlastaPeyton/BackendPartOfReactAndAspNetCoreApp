using Api.DTOs.CommentDTOs;

namespace Api.DTOs.StockDTO
{
    /* DTO sluzi za Read(Endpoint send data to FE) / Write to DB from Endpoint jer zelim da razdvojim sloj koji pristupa bazi od API sloja kome pristupa korisnik tj ne zelim da 
    nikad direktno pristupam Entity klasi u API sloju (Endpoint). Takodje, DTO sluzi za Data Validation u slucaju writing to DB as Request object, jer se to ne radi u Entity klasi, 
    posto mora pre Entity klase da se validira dok je jos na ulazu u Endpoint kao Request DTO object.

       Ne sadrzi Data Annotations jer StockDTOResponse korisitm da Endpoint posalje podatke to FE. 
      
       DTO sadrzi samo proste tipove polja ! 

      StockDTOResponse nema Data Validation, jer se to odigra u Create/UpdateStockRequestDTO, dok StockDTOResponse sluzi samo za prikaz svega iz Stock tj za slanje "Stock" to FE.
    
      Koristim ovaj DTO za Redis, jer nije dobro skladisitit entity klase u Redis.
     */
    public class StockDTOResponse
    {
        public int Id { get; set; } // PK
        public string Symbol { get; set; } = string.Empty; // Ako ne unesem nista, u koloni Symbol bice prazan string 
        public string CompanyName { get; set; } = string.Empty;
        public decimal Purchase { get; set; }
        public decimal Dividend { get; set; }
        public string Industry { get; set; } = string.Empty;
        public long MarketCap { get; set; }
        public List<CommentDTOResponse> Comments { get; set; } // U StockDTO koristim CommentDTO, ne Comment jer je to Entity klasa koja samo u Repository se koristi

        // Nema List<Portfolio> polja, jer to ne treba da se posalje to FE
    }
}
