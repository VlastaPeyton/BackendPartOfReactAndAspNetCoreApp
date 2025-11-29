using Api.DTOs.PortfolioDTOs;
using Api.DTOs.StockDTO;
using Api.Exceptions_i_Result_pattern;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services
{
    // Objasnjen u CommentService i StockSservice
    public class PortfolioService : IPortfolioService
    {
        private readonly UserManager<AppUser> _userManager; // Moze jer AppUser:IdentityUser
        private readonly IStockRepository _stockRepository; // Koristi CachedStockRepository, jer je on decorator on top of StockRepository
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IFinacialModelingPrepService _finacialModelingPrepService;

        public PortfolioService(UserManager<AppUser> userManager, IStockRepository stockRepository, IPortfolioRepository portfolioRepository, IFinacialModelingPrepService finacialModelingPrepService)
        {
             _userManager = userManager;
            _stockRepository = stockRepository;
            _portfolioRepository = portfolioRepository;
            _finacialModelingPrepService = finacialModelingPrepService;
        }

        public async Task<List<StockDTOResponse>> GetUserPortfoliosAsync(string userName, CancellationToken cancellationToken)
        {
            var appUser = await _userManager.FindByNameAsync(userName); // Pretrazuje AspNetUsers tabelu da nadje AppUser 
                                                                        // userManager metode nemaju cancellationToken 

            if (appUser is null)
                throw new UserNotFoundException("User not found via userManager");

            var stocks = await _portfolioRepository.GetUserPortfoliosAsync(appUser, cancellationToken);

            // Mapiram listu Stock entity objekata u listu StockDTOResponse objekata
            var stockDtoResponses = stocks.Select(s => s.ToStockDtoResponse()).ToList();

            return stockDtoResponses;
        }

        public async Task<Result<PortfolioDtoResponse>> AddPortfolioAsync(string symbol, string userName, CancellationToken cancellationToken)
        {
            var appUser = await _userManager.FindByNameAsync(userName); // Ne moze cancellationToken ovde
            if (appUser is null)
                return Result<PortfolioDtoResponse>.Fail("User not found via userManager");

            // Kada u search ukucan npr "tsla" izadje mi 1 ili lista Stocks ili ETFs koji pocinju sa "tsla" i zelim kliknuti Add da dodam bilo koji stock u portfolio, pa prvo provera da li je zeljeni stock u bazi
            var stock = await _stockRepository.GetBySymbolAsync(symbol, cancellationToken);

            // ako ne postoji Stock u bazi, trazi ga na netu u FininacinalModelingPrep
            if (stock is null)
            {
                stock = await _finacialModelingPrepService.FindStockBySymbolAsync(symbol, cancellationToken);
                /* FMP API u searchCompanies u FE prikazuje sve Stocks i ETFs koji sadrze "tsla",ipak necu moci dodati bilo koji, jer neki od njih su ETF (a ne Stock) zato sto FMP API in FindStockBySymbolAsync pretrazuje samo Stocks ! */
                if (stock is null)
                    return Result<PortfolioDtoResponse>.Fail("Ovo sto pokusavas dodati nije Stock, vec ETF, iako ga vidis u listu kad search uradis u FE. Jer API u BE pretrazuje samo Stocks (ne i ETFs). Dok SearchCompanies u FE pretrazuje FMP API koji prikazuje firme a one mogu biti Stock ili ETF.  ");
                else
                    await _stockRepository.CreateAsync(stock, cancellationToken); // stock je azuriran sa Id poljem, jer u CreateAsync EF tracking je to odradio. Sad stock je azuriran i ovde jer je reference type 
            }

            // Ako izabrani symbol nije Stock, vec ETF, iznad izlazimo iz ove metode zbog return. Ali ako je to Stock, onda proveravam da li on vec postoji u listi stocks of current appUser
            var userStocks = await _portfolioRepository.GetUserPortfoliosAsync(appUser, cancellationToken); // Lista svih Stocks koje appUser ima 
            if (userStocks.Any(e => e.Symbol.ToLower() == symbol.ToLower()))
                return Result<PortfolioDtoResponse>.Fail("Cannot add same stock to portfolio as this user has already this stock in portfolio");

            // Ako izabrani stock postoji, nebitno da l u bazi ili u FMP API, dodajemo ga u listu stockova za appUser
            var portfolio = new Portfolio // Jer Stock je jedan Portfolio
            {
                StockId = stock.Id,
                AppUserId = appUser.Id,
                Stock = stock,
                AppUser = appUser,
            };

            await _portfolioRepository.CreateAsync(portfolio, cancellationToken); // ne treba mi da dohvatim povratnu vrednost iz CreateAsync, jer portfolio je reference type pa je njegova promena u CreateAsync applied i ovde odma

            return Result<PortfolioDtoResponse>.Success(portfolio.ToPortfolioDtoResponse());
        }

        public async Task<Result<PortfolioDtoResponse>> DeletePortfolioAsync(string symbol, string userName, CancellationToken cancellationToken)
        {
            var appUser = await _userManager.FindByNameAsync(userName); // Ne podrzava cancellationToken
            var userStocks = await _portfolioRepository.GetUserPortfoliosAsync(appUser, cancellationToken);
            var stockInPortfolios = userStocks.Where(s => s.Symbol.ToLower() == symbol.ToLower()).ToList(); // Nema moze async, jer ne pretrazujem u bazu, vec in-memory userStocks varijablu

            // Ovaj if-else je dobra praksa za ne daj boze, ali sam vec u AddPortfolio obezbedio da moze samo 1 isti stock biti u listi
            if (stockInPortfolios.Count == 1) // is not null jer samo 1 Stock unique stock moze biti u portfolios list, jer AddPortfolio metoda iznad to ogranicila
            {
                var deletedPortfolio = await _portfolioRepository.DeletePortfolioAsync(appUser, symbol, cancellationToken);
                if (deletedPortfolio is not null)
                {
                    var deletedPortfolioDtoReponse = deletedPortfolio.ToPortfolioDtoResponse();
                    return Result<PortfolioDtoResponse>.Success(deletedPortfolioDtoReponse);
                }
                else
                    return Result<PortfolioDtoResponse>.Fail("Stock is not in your porftolio list");
            }
            else
                return Result<PortfolioDtoResponse>.Fail("Stock is not in your portfolio list");
        }
    }
}
