using Api.CQRS;
using Api.CQRS_and_behaviours.Portfolio.GetUserPortfolios;
using Api.Data;
using Api.DTOs.Keyless_entity;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Api.CQRS_and_behaviours.Portfolio.TotalPurchase
{
    public record UserPortfolioTotalQuery(string UserName) : IQuery<UserPortfolioTotalResult>;

    public record UserPortfolioTotalResult(decimal TotalPurchaseValue);
    public class PortfolioTotalPurchaseQueryHandler : IQueryHandler<UserPortfolioTotalQuery, UserPortfolioTotalResult>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IStringLocalizer<Resource> _localization;

        public PortfolioTotalPurchaseQueryHandler(ApplicationDBContext dbContext, IStringLocalizer<Resource> localizer)
        {
            _dbContext = dbContext;
            _localization = localizer;
        }

        public async Task<UserPortfolioTotalResult> Handle(UserPortfolioTotalQuery query, CancellationToken cancellationToken)
        {
            // Zbog stored procedure, moram sve redove(Iako ovde samo 1 red ima) uzeti, a tek onda samo onaj koji mi treba
            var rows = await _dbContext.Set<UserPortfolioTotal>()
                                        .FromSqlInterpolated($"EXEC GetUserPortfolioTotalStoredProcedure @UserName = {query.UserName}")
                                        .AsNoTracking()
                                        .ToListAsync(cancellationToken);

            var userPortfolioTotal = rows.FirstOrDefault();

            if (userPortfolioTotal is null)
                throw new UserNotFoundException(_localization["StoredProcedureProble"]);

            return new UserPortfolioTotalResult(userPortfolioTotal.TotalPurchaseValue);


        }
    }
}
