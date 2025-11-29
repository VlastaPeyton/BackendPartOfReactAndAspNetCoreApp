using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Api.CQRS_and_Validation.Logging
{
    /* U Program.cs mora da se ValidationBehaviour ubaci u MediatR pipeline (nakon ValidationBehaviour )da bi ova klasa mogla automatski da prepozna sta treba da validira kada ISender.Send()
       TRequest : IRequest, jer LoggingBehavior se koristi za both Command and Query
       LoggingBehaviour je registrovan nakon ValidationBehaviour, pa ce Handle da se pozove kad ValidationBehaviour uradi "return next()"
    */
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest where TResponse : notnull
    {   
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

        public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger) 
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _logger.LogInformation($" Handle Request = {typeof(TRequest).Name} - Response={typeof(TResponse).Name} - RequestData={request}");
            
            var timer = new Stopwatch();
            timer.Start();

            var response = await next(); // Poziva se Handle metod u Command/QueryHandler jer je LogginBehaviour poslednji "middleware" u mediator pipeline
            timer.Stop();

            var timeTaken = timer.Elapsed;

            if (timeTaken.Seconds > 3)
                _logger.LogWarning($"The request took {timeTaken}");

            _logger.LogInformation($" Handled Request={typeof(TRequest).Name} with Response={typeof(TResponse).Name}");

            return response;
        }
    }
}
