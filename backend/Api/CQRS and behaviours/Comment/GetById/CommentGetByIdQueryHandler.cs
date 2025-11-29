using Api.CQRS;
using Api.DTOs.CommentDTOs;
using Api.Exceptions;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Mapper;

namespace Api.CQRS_and_Validation.Comment
{
    public record CommentGetByIdQuery(int Id) : IQuery<CommentGetByIdResult>; 
    public record CommentGetByIdResult(CommentDTOResponse commentDTOResponse); 

    // Nema validacija, jer Query cita iz baze
    public class CommentGetByIdQueryHandler : IQueryHandler<CommentGetByIdQuery, CommentGetByIdResult>
    {   
        private readonly ICommentRepository _commentRepository; 
        public CommentGetByIdQueryHandler(ICommentRepository commentRepository) => _commentRepository = commentRepository;
        
        // Mora metoda zbog interface. 
        public async Task<CommentGetByIdResult> Handle(CommentGetByIdQuery query, CancellationToken cancellationToken)
        {   
            var comment = await _commentRepository.GetByIdAsync(query.Id, cancellationToken);

            if (comment is null)
                throw new WrongIdException("Pogresan id"); 

            // Mapiram Comment Entity u DTO i vrati ga u GetById endpoint u CommentController 
            var commentDTOResponse = comment.ToCommentDTOResponse();

            return new CommentGetByIdResult(commentDTOResponse);
        }
    }
}
