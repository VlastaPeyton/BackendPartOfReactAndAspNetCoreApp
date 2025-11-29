using Api.CQRS;
using Api.CQRS_and_behaviours.Comment.Create;
using Api.Exceptions;
using Api.Exceptions_i_Result_pattern;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Api.CQRS_and_Validation.Comment.Delete
{
    public record CommentDeleteCommand(int Id, string userName) : ICommand<Result<CommentDeleteResult>>;
    
    // Mogo sam CommentDTOResponse da stavim u Result object, ali sam namerno ovako da pokazem i flat polja 
    public record CommentDeleteResult(int Id, int? StockId, string Title, string Content, DateTime CreatedOn, string CreatedBy);

    public class CommentDeleteCommandValidator : AbstractValidator<CommentDeleteCommand>
    {
        public CommentDeleteCommandValidator()
        {
            RuleFor(x =>  x.Id).NotEmpty();
        }
    }

    public class CommentDeleteCommandHandler : ICommandHandler<CommentDeleteCommand, Result<CommentDeleteResult>>
    {
        // CQRS Handler poziva Repository, a ne service, jer ako radim CQRS, ne koristim Service.
        private readonly ICommentRepository _commentRepository;
        private readonly UserManager<AppUser> _userManager;
        public CommentDeleteCommandHandler(ICommentRepository commentRepository, UserManager<AppUser> userManager)
        {
            _commentRepository = commentRepository; 
            _userManager = userManager;
        }

        public async Task<Result<CommentDeleteResult>> Handle(CommentDeleteCommand command, CancellationToken cancellationToken)
        {
            // Authorization kako user moze samo svoj komentar brisati 

            // Pronadji zeljeni komentar u bazi 
            var comment = await _commentRepository.GetByIdAsync(command.Id, cancellationToken);
            if (comment is null)
                return Result<CommentDeleteResult>.Fail("Comment not found"); 

            // Pronadji trenutnog usera koji oce da obrise comment
            var appUser = await _userManager.FindByNameAsync(command.userName);
            if (appUser is null)
                return Result<CommentDeleteResult>.Fail("User not found in userManager");

            // User moze obrisati samo svoj komentar
            if (comment.AppUserId != appUser.Id)
                return Result<CommentDeleteResult>.Fail("You can only delete your own comments");  
                
            // Obrisi svoj komentar 
            var deletedComment = await _commentRepository.DeleteAsync(command.Id, cancellationToken);
            if (deletedComment is null)
                return Result<CommentDeleteResult>.Fail("Comment not found"); 

            // Mapiram Comment Entity to DTO 
            return Result<CommentDeleteResult>.Success(new CommentDeleteResult(comment.Id.Value, comment.StockId, comment.Title, comment.Content, comment.CreatedOn, comment.AppUser?.UserName ?? "Nepoznata osoba"));  
        }
    }
}
