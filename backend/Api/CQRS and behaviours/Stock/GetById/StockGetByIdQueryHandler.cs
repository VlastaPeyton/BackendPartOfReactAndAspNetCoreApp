using Api.CQRS;
using Api.DTOs.StockDTO;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Mapper;

namespace Api.CQRS_and_behaviours.Stock.GetById
{
    public record StockGetByIdQuery(int Id) : IQuery<Result<StockGetByIdResult>>;
    public record StockGetByIdResult(StockDTOResponse StockDTOResponse);

    public class StockGetByIdQueryHandler : IQueryHandler<StockGetByIdQuery, Result<StockGetByIdResult>>
    {
        private readonly IStockRepository _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository 
        public StockGetByIdQueryHandler(IStockRepository stockRepository) => _stockRepository = stockRepository;

        public async Task<Result<StockGetByIdResult>> Handle(StockGetByIdQuery query, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.GetByIdAsync(query.Id, cancellationToken);

            if (stock is null) // Jer GetByIdAsync moze i null da vrati 
                return Result<StockGetByIdResult>.Fail("Nije pronadjen Stock by zeljeni id");

            // Mapiram Stock entity u StockDTOResponse 
            var stockDTOResponse = stock.ToStockDtoResponse();

            return Result<StockGetByIdResult>.Success(new StockGetByIdResult(stockDTOResponse));
        }
    }
}
