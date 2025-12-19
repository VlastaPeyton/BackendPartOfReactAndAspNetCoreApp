using Api.CQRS;
using Api.Data;
using Api.DTOs.StockDTO;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
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
        private readonly IStockRepositoryBase _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository tj StockRepositoryBase sada
        private readonly ApplicationDBContext _dbContext;  // Jer Repository nema SaveChangesAsync da bih smanjio br round trip ka Db - pogledaj Transakcija.txt i UnitOfWork.txt

        public StockDeleteCommandHandler(IStockRepositoryBase stockRepository, ApplicationDBContext dBContext)
        {
            _stockRepository = stockRepository;
            _dbContext = dBContext;
        }

        public async Task<StockDeleteResult> Handle(StockDeleteCommand command, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.DeleteAsync(command.Id, cancellationToken);

            if (stock is null)
                throw new StockNotFoundException("Nije nadjen stock");

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new StockDeleteResult(stock.ToStockDtoResponse());
        }
    }
}
