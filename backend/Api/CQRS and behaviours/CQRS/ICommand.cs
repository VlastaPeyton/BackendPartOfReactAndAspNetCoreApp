using MediatR;

namespace Api.CQRS
{   
    // ICommand interface for Command for write to DB 

    /* IRequest comes from MediatR.
       IRequest<TResponse> znaci svaka klasa (Command) koja implementira IRequest<TResponse> moze se slati kroz
      ISender.Send() (ISender.Send(command) stoji u endpoint i aktivira CommandHandler za taj command. 
       TResponse je generic za Response/Result objekat.
       Kada endpoint vraca klijentu odgovor nakon izmene u bazi.
       */
    public interface ICommand<out TResponse> : IRequest<TResponse> { }

    /* ICommand bez Response koristim kada endpoint ne vraca clientu nista nakon izmene u bazi
       Unit je void za MediatR i koristim kada CommandHandler ne vraca Response to client.*/
    public interface ICommand : ICommand<Unit> { }  
    
}
