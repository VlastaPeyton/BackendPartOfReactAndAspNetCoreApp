using Api.CQRS;
using Api.DTOs.StockDTO;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Mapper;
using FluentValidation;

namespace Api.CQRS_and_behaviours.Stock.Delete
{   
    public record StockDeleteCommand(int Id) : ICommand<StockDeleteResult>;
    public record StockDeleteResult(StockDTOResponse StockDTOResponse);

    public class StockDeleteCommandValidator : AbstractValidator<StockDeleteCommand>
    {
        public StockDeleteCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class StockDeleteCommandHandler : ICommandHandler<StockDeleteCommand, StockDeleteResult> 
    {
        private readonly IStockRepository _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository 
        public StockDeleteCommandHandler(IStockRepository stockRepository) => _stockRepository = stockRepository;

        public async Task<StockDeleteResult> Handle(StockDeleteCommand command, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.DeleteAsync(command.Id, cancellationToken);
            if (stock is null)
                throw new StockNotFoundException("Nije nadjen stock");

            return new StockDeleteResult(stock.ToStockDtoResponse());
        }
    }
}
