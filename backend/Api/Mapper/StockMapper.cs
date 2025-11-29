using System.Runtime.CompilerServices;
using Api.DTOs.CommentDTOs;
using Api.DTOs.PortfolioDTOs;
using Api.DTOs.Stock;
using Api.DTOs.StockDTO;
using Api.DTOs.StockDTOs;
using Api.Models;

namespace Api.Mapper
{
    // Pogledaj DTO vs entity klase.txt 

    // Da napravim Stock Entity klasu od DTO klasa kada pokrecem Repository metode jer entity se koristi za Repository samo tj za EF Core
    // Da napravim DTO klasu od Stock entity klase kada saljem data to FE in Endpoint ili u Redis
    public static class StockMapper
    {   
        // Pogledaj DTO vs entity klase.txt 

        // Extension method for Stock.cs, without calling parameters
        public static StockDTOResponse ToStockDtoResponse(this Stock stock)
        {
            return new StockDTOResponse
            {
                Id = stock.Id, // Id mapiram, jer mapiram iz Stock to DTO
                Symbol = stock.Symbol,
                CompanyName = stock.CompanyName,
                Purchase = stock.Purchase,
                Dividend = stock.Dividend,
                Industry = stock.Industry,
                MarketCap = stock.MarketCap,
                //Comments = stock.Comments.Select(c => c.ToCommentDTOResponse()).ToList() // Ovako mapiram List type navigational property, jer polje DTO klase mora biti lista DTO tipa, a ne navigational property entity typa kao u entity klasi
                Comments = stock.Comments.Select(c => c.ToCommentDTOResponse()).ToList() ?? new List<CommentDTOResponse>()
            };
        }

        // Extension method for CreateStockRequestDTO.cs, without calling parameters
        public static Stock ToStockFromCreateStockRequestDTO(this CreateStockCommandModel command)
        {
            return new Stock
            {   // Ne mapiram Id iz DTO, jer se prilikom dodavanja u bazu sam generise 
                Symbol = command.Symbol,
                CompanyName = command.CompanyName,
                Purchase = command.Purchase,
                Dividend = command.Dividend,
                Industry = command.Industry,
                MarketCap = command.MarketCap,
                /* Ne mapiram Comments/Portfolios polja, jer nisu prisutna u CreateStockRequestDTO posto su collection navigation attribute u Stock pa imaju default polje, a nije ni logicno da postoje u UpdateStockRequestDTO
                Ova polja, kao i FK PK polja u Stock/Comment/Portfolio sluze da EF (ili ja u OnModelCreating), moze da napravim FK-PK vezu Stock-Comment/Portfolio i zato se ne salju from FE. */
            };
        }

        // Extension method for UpdateStockRequestDTO, without calling parameters
        public static Stock ToStockFromUpdateStockRequestDTO(this UpdateStockCommandModel command)
        {
            return new Stock
            {
                // Ne mapiram Id iz DTO, jer se prilikom dodavanja u bazu sam generise 
                Symbol = command.Symbol,
                CompanyName = command.CompanyName,
                Purchase = command.Purchase,
                Dividend = command.Dividend,
                Industry = command.Industry,
                MarketCap = command.MarketCap,
                /* Ne mapiram Comments/Portfolios polja, jer nisu prisutna u UpdateStockRequestDTO posto su collection navigation attribute u Stock pa imaju default polje, a nije ni logicno da postoje u UpdateStockRequestDTO
                 Ova polja, kao i FK PK polja u Stock/Comment/Portfolio sluze da EF (ili ja u OnModelCreating), moze da napravim FK-PK vezu Stock-Comment/Portfolio i zato se ne salje from FE.  */
            };
        }

        // Extension method for FinancialModelingPrepStockDTO, without calling parameters
        public static Stock ToStockFromFinancialModelingPrepStockDTO(this FinancialModelingPrepStockDTO financialModelingPrepStockDTO)
        {
            return new Stock
            {   // Ne smem Id jer baza ga automatski dodeli 
                Symbol = financialModelingPrepStockDTO.symbol,
                CompanyName = financialModelingPrepStockDTO.companyName,
                Purchase = financialModelingPrepStockDTO.price,
                Dividend = financialModelingPrepStockDTO.lastDiv,
                Industry = financialModelingPrepStockDTO.industry,
                MarketCap = financialModelingPrepStockDTO.mktCap
                /* Ne mapiram Comments/Portfolios polja, jer nisu prisutna u FinancialModelingPrepStockDTO posto su collection navigation attribute u Stock pa imaju default polje, a nije ni logicno da postoje u UpdateStockRequestDTO
                 Ova polja, kao i FK PK polja u Stock/Comment/Portfolio sluze da EF (ili ja u OnModelCreating), moze da napravim FK-PK vezu Stock-Comment/Portfolio i zato se ne salju from FE. */
            };
        }
        
        // Extension method for Portfolio.cs
        public static PortfolioDtoResponse ToPortfolioDtoResponse(this Portfolio portfolio)
        {
            return new PortfolioDtoResponse
            {
                StockId = portfolio.StockId,
                AppUserId = portfolio.AppUserId,
                Stock = portfolio.Stock.ToStockDtoResponse(),
            };
        }

        // Exntension method for AppUser.cs 
        public static AppUserDtoResponse ToAppUserDtoResponse(this AppUser appUser)
        {
            return new AppUserDtoResponse
            {
                Portfolios = appUser.Portfolios.Select(p => p.ToPortfolioDtoResponse()).ToList(),
            };
        }
    }
}
