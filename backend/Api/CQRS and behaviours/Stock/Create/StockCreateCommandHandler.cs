using Api.CQRS;
using Api.Data;
using Api.DTOs.StockDTO;
using Api.DTOs.StockDTOs;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
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
        private readonly IStockRepositoryBase _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository tj on StockRepositoryBase sada
        private readonly ApplicationDBContext _dbContext;  // Jer Repository nema SaveChangesAsync da bih smanjio br round trip ka Db - pogledaj Transakcija.txt i UnitOfWork.txt

        public StockCreateCommandHandler(IStockRepositoryBase stockRepository, ApplicationDBContext dbContext)
        {
            _stockRepository = stockRepository;
            _dbContext = dbContext;
        }
        
        public async Task<StockCreateResult> Handle(StockCreateCommand command, CancellationToken cancellationToken)
        {
            var stock = command.CreateStockCommandModel.ToStockFromCreateStockRequestDTO();

            await _stockRepository.CreateAsync(stock, cancellationToken); 
            await _dbContext.SaveChangesAsync(cancellationToken);

            var stockDtoResponse = stock.ToStockDtoResponse();

            return new StockCreateResult(stockDtoResponse);
        }
    }
}
