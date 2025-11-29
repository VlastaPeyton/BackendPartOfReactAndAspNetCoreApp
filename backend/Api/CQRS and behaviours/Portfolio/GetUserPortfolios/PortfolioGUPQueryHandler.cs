using Api.CQRS;
using Api.DTOs.StockDTO;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.CQRS_and_behaviours.Portfolio.GetUserPortfolios
{   
    public record PortfolioGUPQuery(string UserName) : IQuery<PortfolioGUPResult>;
    public record PortfolioGUPResult(List<StockDTOResponse> StockDTOResponses); 

    public class PortfolioGUPQueryHandler : IQueryHandler<PortfolioGUPQuery, PortfolioGUPResult>
    {
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly UserManager<AppUser> _userManager;
        public PortfolioGUPQueryHandler(IPortfolioRepository portfolioRepository, UserManager<AppUser> userManager) 
        { 
            _portfolioRepository = portfolioRepository; 
            _userManager = userManager;
        }

        public async Task<PortfolioGUPResult> Handle(PortfolioGUPQuery query, CancellationToken cancellationToken)
        {
            var appUser = await _userManager.FindByNameAsync(query.UserName); // Pretrazuje AspNetUsers tabelu da nadje AppUser 
                                                                        // userManager metode nemaju cancellationToken 

            if (appUser is null)
                throw new UserNotFoundException("User not found via userManager");

            var stocks = await _portfolioRepository.GetUserPortfoliosAsync(appUser, cancellationToken);

            // Mapiram listu Stock entity objekata u listu StockDTOResponse objekata
            var stockDtoResponses = stocks.Select(s => s.ToStockDtoResponse()).ToList();

            return new PortfolioGUPResult(stockDtoResponses);
        }
    }
}
