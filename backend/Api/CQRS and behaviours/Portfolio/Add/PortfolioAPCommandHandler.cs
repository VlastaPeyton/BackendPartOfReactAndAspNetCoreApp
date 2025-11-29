using Api.CQRS;
using Api.DTOs.PortfolioDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Api.CQRS_and_behaviours.Portfolio.AddPortfolio
{   
    public record PortfolioApCommand(string Symbol, string UserName) : ICommand<Result<PortfolioApResult>>;
    public record PortfolioApResult(PortfolioDtoResponse PortfolioDtoResponse); 

    public class PortfolioApCommandValidator : AbstractValidator<PortfolioApCommand>
    {
        public PortfolioApCommandValidator()
        {
            RuleFor(x => x.Symbol).NotEmpty();
            RuleFor(x => x.UserName).NotEmpty();
        }
    }
    public class PortfolioAPCommandHandler : ICommandHandler<PortfolioApCommand, Result<PortfolioApResult>>
    {   
        private readonly UserManager<AppUser>  _userManager;
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IStockRepository _stockRepository; // CachedStockRepository jer je decorator on top of StockRepository
        private readonly IFinacialModelingPrepService _finacialModelingPrepService;
        public PortfolioAPCommandHandler(UserManager<AppUser> userManager, IPortfolioRepository portfolioRepository, 
                                         IStockRepository stockRepository, IFinacialModelingPrepService finacialModelingPrepService)
        {
            _userManager = userManager; 
            _portfolioRepository = portfolioRepository;
            _stockRepository = stockRepository;
            _finacialModelingPrepService = finacialModelingPrepService;
        }

        public async Task<Result<PortfolioApResult>> Handle(PortfolioApCommand command, CancellationToken cancellationToken)
        {
            var appUser = await _userManager.FindByNameAsync(command.UserName); // Ne moze cancellationToken ovde
            if (appUser is null)
                return Result<PortfolioApResult>.Fail("User not found via userManager");

            // Kada u search ukucan npr "tsla" izadje mi 1 ili lista Stocks ili ETFs koji pocinju sa "tsla" i zelim kliknuti Add da dodam bilo koji stock u portfolio, pa prvo provera da li je zeljeni stock u bazi
            var stock = await _stockRepository.GetBySymbolAsync(command.Symbol, cancellationToken);

            // ako ne postoji Stock u bazi, trazi ga na netu u FininacinalModelingPrep
            if (stock is null)
            {
                stock = await _finacialModelingPrepService.FindStockBySymbolAsync(command.Symbol, cancellationToken);
                /* FMP API u searchCompanies u FE prikazuje sve Stocks i ETFs koji sadrze "tsla",ipak necu moci dodati bilo koji, jer neki od njih su ETF (a ne Stock) zato sto FMP API in FindStockBySymbolAsync pretrazuje samo Stocks ! */
                if (stock is null)
                    return Result<PortfolioApResult>.Fail("Ovo sto pokusavas dodati nije Stock, vec ETF, iako ga vidis u listu kad search uradis u FE. Jer API u BE pretrazuje samo Stocks (ne i ETFs). Dok SearchCompanies u FE pretrazuje FMP API koji prikazuje firme a one mogu biti Stock ili ETF.  ");
                else
                    await _stockRepository.CreateAsync(stock, cancellationToken); // stock je azuriran sa Id poljem, jer u CreateAsync EF tracking je to odradio. Sad stock je azuriran i ovde jer je reference type 
            }

            // Ako izabrani symbol nije Stock, vec ETF, iznad izlazimo iz ove metode zbog return. Ali ako je to Stock, onda proveravam da li on vec postoji u listi stocks of current appUser
            var userStocks = await _portfolioRepository.GetUserPortfoliosAsync(appUser, cancellationToken); // Lista svih Stocks koje appUser ima 
            if (userStocks.Any(e => e.Symbol.ToLower() == command.Symbol.ToLower()))
                return Result<PortfolioApResult>.Fail("Cannot add same stock to portfolio as this user has already this stock in portfolio");

            // Ako izabrani stock postoji, nebitno da l u bazi ili u FMP API, dodajemo ga u listu stockova za appUser
            var portfolio = new Api.Models.Portfolio // Jer Stock je jedan Portfolio
            {              
                StockId = stock.Id,
                AppUserId = appUser.Id,
                Stock = stock,
                AppUser = appUser,
            };

            await _portfolioRepository.CreateAsync(portfolio, cancellationToken); // ne treba mi da dohvatim povratnu vrednost iz CreateAsync, jer portfolio je reference type pa je njegova promena u CreateAsync applied i ovde odma

            return Result<PortfolioApResult>.Success(new PortfolioApResult(portfolio.ToPortfolioDtoResponse()));
        }
    }
}
