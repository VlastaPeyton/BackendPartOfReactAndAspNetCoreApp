using Api.CQRS;
using FluentValidation;
using MediatR;

namespace Api.CQRS_and_Validation
{
    /* Samo se Command validira, jer on menja podatke u bazi. Moze i Query, ali nema potrebe skoro nikad.
       IPipelineBehaviour je iz MediatR i sluzi da se ValidationBehaviour ugradi direkt u MediatR pipeline tj nakon ISender.Send(command) u endpoint
     aktivira se ValidationBehaviour, pa ako nema greske u validaciji, nakon toga sledeci validator(ako postoji), pa na kraju Halde metoda iz CommandHandler.
       Validation je iz FluentValidation.
        
       U Program.cs mora da se ValidationBehaviour ubaci u MediatR pipeline da bi ova klasa mogla automatski da prepozna sta treba da validira kada ISender.Send()
     */
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand<TResponse>
    {   
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        /* IValidator<TRequest> <=> AbstractValidator<Command> u CommandValidator klasi 
           IValidator<TRequest>, zbog DI u Program.cs za MediatR Pipeline, znace na koji CommandValidator se odnosi, jer CommandValidator: AbstractValidator<Command>
           IEnumerable<IValidator<TRequest>> - svi validatori za zeljeni Command (ali uvek samo 1 validator za 1 command imam tj neam 2 CommandValidator skoro nikad)
         */
        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        // Mora metoda zbog interface 
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {   
            var context = new ValidationContext<TRequest>(request); 

            // Call ValidateAsync method for each Handle method - ide kroz CommandValidator i pokrece svaki RuleFor 
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))); // Task.WhenAll = complete all validation operations for CommandValidator

            // Check for any erro in validationResults 
            var failures = validationResults.Where(r => r.Errors.Any()).SelectMany(r => r.Errors).ToList();

            // If any error occured, throw ValidationException 
            if (failures.Any())
                throw new ValidationException(failures); // ValidationException je built-in

            // next() will run next MediatR pipeline behaviour (ako postoji) registrovan nakon ValidationBehaviour u Program.cs, pa tek na kraju CommandHandler's Handle metodu jer ona je uvek na kraju pipeline
            return await next();
        }
    }
}
