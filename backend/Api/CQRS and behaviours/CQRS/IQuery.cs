using MediatR;

namespace Api.CQRS
{   
    // IQuery interface for Query for reading iz DB

    /* IRequest je iz MediatR. 
       Nema IQuery koji ne vraca nista klijentu, jer citanje iz baze uvek sale klijentu nesto iz baze. */
    public interface IQuery<out TResponse> : IRequest<TResponse> where TResponse: notnull { }
    
}
