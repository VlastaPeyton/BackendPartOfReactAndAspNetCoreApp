using Api.Models;

namespace Api.DTOs.PortfolioDTOs
{
    public class AppUserDtoResponse
    {
        public List<PortfolioDtoResponse> Portfolios { get; set; } // Mora biti ugasen circular reference u JSON Serialization u Program.cs, jer PortfolioDtoResponse sadrzi AppUserDto polje
        /*public string? RefreshTokenHash { get; set; }
          public DateTime? RefreshTokenExpiryTime { get; set; }
          public DateTime? LastRefreshTokenUsedAt { get; set; } 
          Ovo su osetljiva polja koja ne saljem klijentu ! */
    }
}
