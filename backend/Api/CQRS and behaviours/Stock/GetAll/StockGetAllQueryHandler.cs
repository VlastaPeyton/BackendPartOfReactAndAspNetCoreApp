using Api.CQRS;
using Api.DTOs.StockDTO;
using Api.Helpers;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Mapper;

namespace Api.CQRS_and_behaviours.Stock.GetAll
{   
    public record StockGetAllQuery(StockQueryObject StockQueryObject) : IQuery<StockGetAllResult>;
    public record StockGetAllResult(IEnumerable<StockDTOResponse> StockDTOResponses);
    public class StockGetAllQueryHandler : IQueryHandler<StockGetAllQuery, StockGetAllResult>
    {   
        private readonly IStockRepositoryBase _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository tj StockRepositoryBase sada
        public StockGetAllQueryHandler(IStockRepositoryBase stockRepository) => _stockRepository = stockRepository;
       
        public async Task<StockGetAllResult> Handle(StockGetAllQuery query, CancellationToken cancellationToken)
        {
            var stocks = await _stockRepository.GetAllAsync(query.StockQueryObject, cancellationToken);
            var stockDTOs = stocks.Select(s => s.ToStockDtoResponse()).ToList();

            return new StockGetAllResult(stockDTOs);
        }
    }
}
