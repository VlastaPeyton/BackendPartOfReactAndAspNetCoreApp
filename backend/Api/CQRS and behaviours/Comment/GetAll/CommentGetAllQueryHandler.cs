using Api.CQRS;
using Api.DTOs.CommentDTOs;
using Api.Helpers;
using Api.Interfaces;
using Api.Mapper;

namespace Api.CQRS_and_behaviours.Comment.GetAll
{   
    // Query/Result moraju imati polja istog imena i tipa kao Request/Response objekti, kako bih mogao lakse da mapiram

    public record CommentGetAllQuery(CommentQueryObject commentQueryObject) : IQuery<CommentGetallResult>; 
    public record CommentGetallResult(List<CommentDTOResponse> commentResponseDTOs);

    // Nema validacija za Query, jer je to citanje iz baze
    public class CommentGetAllQueryHandler : IQueryHandler<CommentGetAllQuery, CommentGetallResult>
    {
        // CQRS Handler poziva CommentRepository, a ne CommentService, jer ako radim CQRS, ne koristim Service ! 

        private readonly ICommentRepository _commentRepository;
        public CommentGetAllQueryHandler(ICommentRepository commentRepository) => _commentRepository = commentRepository;
        
        // Metoda mora zbog interface
        public async Task<CommentGetallResult> Handle(CommentGetAllQuery query, CancellationToken cancellationToken)
        {   /* CQRS, kao i Service metoda, mapira DTO u Entity jer Repository prima entity i obratno. Ovde nema DTO->Entity, zbog objasnjenja u GetAllAsync.
             ali ima Entity->DTO.*/ 
            var comments = await _commentRepository.GetAllAsync(query.commentQueryObject, cancellationToken); // Iako Repository prima/vraca samo Entity objekte, CommentQueryObject nisam mogao mapirati u odgovarajuci Entity objekat
            var commentResponseDTOs = comments.Select(x => x.ToCommentDTOResponse()).ToList(); // Iz IEnumerable (lista u bazi) pretvaram u listu zbog povratnog tipa metode

            return new CommentGetallResult(commentResponseDTOs);
        }
    }
}
