using Api.CQRS;
using Api.Data;
using Api.DTOs.StockDTO;
using Api.DTOs.StockDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Mapper;
using FluentValidation;

namespace Api.CQRS_and_behaviours.Stock.Update
{   
    public record StockUpdateCommand(int Id, UpdateStockCommandModel UpdateStockCommandModel) : ICommand<Result<StockUpdateResult>>;
    public record StockUpdateResult(StockDTOResponse StockDTOResponse);

    public class StockUpdateCommandValidator : AbstractValidator<StockUpdateCommand>
    {
        public StockUpdateCommandValidator()
        {
            RuleFor(x => x.UpdateStockCommandModel.Symbol).NotEmpty();
        }
    }

    public class StockUpdateCommandHandler : ICommandHandler<StockUpdateCommand, Result<StockUpdateResult>>
    {
        private readonly IStockRepositoryBase _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository tj StockRepositoryBase sada
        private readonly ApplicationDBContext _dbContext;  // Jer Repository nema SaveChangesAsync da bih smanjio br round trip ka Db - pogledaj Transakcija.txt i UnitOfWork.txt

        public StockUpdateCommandHandler(IStockRepositoryBase stockRepository, ApplicationDBContext dbContext)
        {
            _stockRepository = stockRepository;
            _dbContext = dbContext;
        }

        public async Task<Result<StockUpdateResult>> Handle(StockUpdateCommand command, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.UpdateAsync(command.Id, command.UpdateStockCommandModel, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (stock is null)
                return Result<StockUpdateResult>.Fail("Not found stock");

            return Result<StockUpdateResult>.Success( new StockUpdateResult(stock.ToStockDtoResponse()));
        }
    }
}
