using Api.DTOs.StockDTO;
using Api.Value_Objects;

namespace Api.DTOs.PortfolioDTOs
{   
    

    public class PortfolioDtoResponse
    {   
        public int StockId { get; set; }
        public string AppUserId { get; set; }
        public StockDTOResponse Stock { get; set; }
        //public AppUserDtoResponse AppUser { get; set; } // Mora biti ugasen circular reference u JSON Serialization u Program.cs, jer AppUserDtoResponse sadrzi List<PortfolioDtoResponse> polje
        // ne treba mi AppUser polje
    }
}
