using Api.DTOs.Keyless_entity;
using Api.DTOs.PortfolioDTOs;
using Api.DTOs.StockDTO;
using Api.Exceptions_i_Result_pattern;

namespace Api.Interfaces
{   
    // Objasnjeno u CommentService i StockService
    public interface IPortfolioService
    {
        Task<IEnumerable<StockDTOResponse>> GetUserPortfoliosAsync(string userName, CancellationToken cancellationToken);
        Task<Result<PortfolioDtoResponse>> AddPortfolioAsync(string symbol, string userName, CancellationToken cancellationToken);
        Task<Result<PortfolioDtoResponse>> DeletePortfolioAsync(string symbol, string userName, CancellationToken cancellationToken);
        Task<decimal> GetUserPortfolioTotalPurchaseAsync(string userName, CancellationToken cancellationToken);
    }
}
