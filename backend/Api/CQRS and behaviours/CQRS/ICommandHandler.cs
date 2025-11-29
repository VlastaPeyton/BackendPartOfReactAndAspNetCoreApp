using MediatR;

namespace Api.CQRS
{   
    // CommandHandler koji ce da se aktivira putem ISender.Send iz endpoint 

    // Kada ICommand<out TResponse>  za endpoint koji vraca odgovor klijentu nakon izmene u bazi
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse> where TCommand: ICommand<TResponse>
                                                                                                    where TResponse: notnull { }

    // Kada ICommand nema <outTResponse> za endpoint koji ne vraca nista klijentu nakon izmene u bazi
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Unit> where TCommand: ICommand<Unit> { }
    
}
