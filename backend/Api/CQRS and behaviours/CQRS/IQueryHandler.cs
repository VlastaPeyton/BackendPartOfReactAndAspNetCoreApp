using MediatR;

namespace Api.CQRS
{   
    // IRequestHandler je iz MediatR

    // Nema slucaj kao kod ICommandHandler da moze da vrati Unit tj nista, jer kad citam iz baze, uvek to vracam u endpoint/handler 
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery: IQuery<TResponse>
                                                                                              where TResponse : notnull { }
}
