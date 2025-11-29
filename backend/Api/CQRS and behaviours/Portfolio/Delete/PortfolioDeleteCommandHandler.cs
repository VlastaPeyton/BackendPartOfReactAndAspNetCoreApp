using Api.CQRS;
using Api.DTOs.PortfolioDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Api.CQRS_and_behaviours.Portfolio.Delete
{   
    public record PortfolioDeleteCommand(string Symbol, string UserName) : ICommand<Result<PortfolioDeleteResult>>;
    public record PortfolioDeleteResult(PortfolioDtoResponse PortfolioDtoResponse); 

    public class PortfolioDeleteValidator : AbstractValidator<PortfolioDeleteCommand>
    {
        public PortfolioDeleteValidator()
        {
            RuleFor(x => x.Symbol).NotEmpty();
            RuleFor(x => x.UserName).NotEmpty();    
        }
    }

    public class PortfolioDeleteCommandHandler : ICommandHandler<PortfolioDeleteCommand, Result<PortfolioDeleteResult>>
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IPortfolioRepository _portfolioRepository;

        public PortfolioDeleteCommandHandler(UserManager<AppUser> userManager, IPortfolioRepository portfolioRepository)
        {
            _userManager = userManager;
            _portfolioRepository = portfolioRepository;
        }

        public async Task<Result<PortfolioDeleteResult>> Handle(PortfolioDeleteCommand command, CancellationToken cancellationToken)
        {
            var appUser = await _userManager.FindByNameAsync(command.UserName); // Ne podrzava cancellationToken
            var userStocks = await _portfolioRepository.GetUserPortfoliosAsync(appUser, cancellationToken);
            var stockInPortfolios = userStocks.Where(s => s.Symbol.ToLower() == command.Symbol.ToLower()).ToList(); // Nema moze async, jer ne pretrazujem u bazu, vec in-memory userStocks varijablu

            // Ovaj if-else je dobra praksa za ne daj boze, ali sam vec u AddPortfolio obezbedio da moze samo 1 isti stock biti u listi
            if (stockInPortfolios.Count == 1) // is not null jer samo 1 Stock unique stock moze biti u portfolios list, jer AddPortfolio metoda iznad to ogranicila
            {
                var deletedPortfolio = await _portfolioRepository.DeletePortfolioAsync(appUser, command.Symbol, cancellationToken);
                if (deletedPortfolio is not null)
                {
                    var deletedPortfolioDtoReponse = deletedPortfolio.ToPortfolioDtoResponse();
                    return Result<PortfolioDeleteResult>.Success(new PortfolioDeleteResult(deletedPortfolioDtoReponse));
                }
                else
                    return Result<PortfolioDeleteResult>.Fail("Stock is not in your porftolio list");
            }
            else
                return Result<PortfolioDeleteResult>.Fail("Stock is not in your portfolio list");
        }
    }
}
