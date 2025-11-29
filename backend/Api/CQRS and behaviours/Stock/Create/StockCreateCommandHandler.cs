using Api.CQRS;
using Api.DTOs.StockDTO;
using Api.DTOs.StockDTOs;
using Api.Interfaces;
using Api.Mapper;
using FluentValidation;

namespace Api.CQRS_and_behaviours.Stock.Create
{   
    public record StockCreateCommand(CreateStockCommandModel CreateStockCommandModel) : ICommand<StockCreateResult>;
    public record StockCreateResult(StockDTOResponse StockDTOResponse);

    public class StockCreateCommandValidator : AbstractValidator<StockCreateCommand>
    {
        public StockCreateCommandValidator()
        {
            RuleFor(x => x.CreateStockCommandModel.Symbol).NotEmpty();
        }
    }

    public class StockCreateCommandHandler : ICommandHandler<StockCreateCommand, StockCreateResult>
    {
        private readonly IStockRepository _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository 
        public StockCreateCommandHandler(IStockRepository stockRepository) => _stockRepository = stockRepository;
        
        public async Task<StockCreateResult> Handle(StockCreateCommand command, CancellationToken cancellationToken)
        {
            var stock = command.CreateStockCommandModel.ToStockFromCreateStockRequestDTO();

            await _stockRepository.CreateAsync(stock, cancellationToken); 

            var stockDtoResponse = stock.ToStockDtoResponse();

            return new StockCreateResult(stockDtoResponse);
        }
    }
}
