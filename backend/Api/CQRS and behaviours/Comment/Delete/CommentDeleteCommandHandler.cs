using Api.CQRS;
using Api.Data;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces.IRepositoryBase;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Api.CQRS_and_Validation.Comment.Delete
{
    public record CommentDeleteCommand(int Id, string UserName) : ICommand<Result<CommentDeleteResult>>;
    
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
        private readonly ICommentRepositoryBase _commentRepository; // Moze i ICommentRepository - pogledaj BaseRepository 
        private readonly ApplicationDBContext _dbContext; // Repository write metods nemaju SaveChangesAsync, pa to ovde pisem da smanjim No round trips ka Db - pogledaj Transakcije.txt i UnitOfWork.txt
        private readonly UserManager<AppUser> _userManager;
        public CommentDeleteCommandHandler(ICommentRepositoryBase commentRepository, 
                                           ApplicationDBContext dBContext,
                                           UserManager<AppUser> userManager)
        {
            _commentRepository = commentRepository; 
            _dbContext = dBContext;
            _userManager = userManager;
        }

        public async Task<Result<CommentDeleteResult>> Handle(CommentDeleteCommand command, CancellationToken cancellationToken)
        {
            // Authorization kako user moze samo svoj komentar brisati 

            // Pronadji zeljeni komentar u bazi 
            var comment = await _commentRepository.GetByIdAsync(command.Id, cancellationToken);
            if (comment is null)
                return Result<CommentDeleteResult>.Fail("Comment not found");

            // Pronadji trenutn logged usera koji oce da obrise comment, jer ako nije loggedin ne sme 
            var appUser = await _userManager.FindByNameAsync(command.UserName);

            if (appUser is null)
                return Result<CommentDeleteResult>.Fail("User not found from comment entity");

            // Logged user moze obrisati samo svoj komentar
            if (comment.AppUserId != appUser.Id)
                return Result<CommentDeleteResult>.Fail("You can only delete your own comments");  
             
            // Obrisi svoj komentar 
            var deletedComment = await _commentRepository.DeleteAsync(command.Id, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken); // Ako neka od write repo metoda iznad fail, ovaj commit nece uspeti

            if (deletedComment is null)
                return Result<CommentDeleteResult>.Fail("Comment not found"); 

            // Mapiram Comment Entity to DTO 
            return Result<CommentDeleteResult>.Success(new CommentDeleteResult(comment.Id.Value, comment.StockId, comment.Title, comment.Content, comment.CreatedOn, comment.AppUser?.UserName ?? "Nepoznata osoba, jer nisam dohvatio nav atribut"));  
        }
    }
}
