using Api.CQRS;
using Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.CQRS_and_behaviours.Behaviours.UnitOfWork_behaviour
{
    //Samo se Command validira, jer on menja podatke u bazi zato ICommand - ne koristim, jer ako treba vise SaveChangesAsync u Handle, onda ovo ne moze
    public class UnitOfWorkBehavior<TRequest, TResponse>: IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand<TResponse>
    {
        private readonly ApplicationDBContext _dbContext;

        public UnitOfWorkBehavior(ApplicationDBContext dbContext) => _dbContext = dbContext;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                var response = await next(); // Poziva se CommandHandler jer UnitOfWorkBehavior je poslednji u mediatr pipeline
                // Nakon sto CommandHandler zavrsi, SaveChangesAsync krece
                await _dbContext.SaveChangesAsync(cancellationToken);
                return response; // Vraca se u prehtodno registrovani (LoggingBehaviour)
            }
            catch (DbUpdateException)
            {
                throw new DbUpdateException("Greska prilikom CQRS Db update"); // Propagira dalje ka GlobalExceptionHandler
            }
        }
    }
}
