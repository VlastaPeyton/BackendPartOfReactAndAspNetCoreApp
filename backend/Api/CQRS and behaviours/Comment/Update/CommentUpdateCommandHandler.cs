using Api.CQRS;
using Api.Data;
using Api.DTOs.CommentDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Mapper;
using FluentValidation;

namespace Api.CQRS_and_behaviours.Comment.Update
{   
    public record CommentUpdateCommand(int Id, UpdateCommentCommandModel UpdateCommentCommandModel) : ICommand<Result<CommentUpdateResult>>;
    public record CommentUpdateResult(CommentDTOResponse CommentDTOResponse); 

    public class CommentUpdateCommandValidator : AbstractValidator<CommentUpdateCommand>
    {
        public CommentUpdateCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UpdateCommentCommandModel.Title).NotEmpty();
            RuleFor(x => x.UpdateCommentCommandModel.Content).NotEmpty();
        }
    }

    public class CommentUpdateCommandHandler : ICommandHandler<CommentUpdateCommand, Result<CommentUpdateResult>>
    {
        private readonly ICommentRepositoryBase _commentRepository; // Moze i ICommentRepository - pogledaj BaseRepository 
        private readonly ApplicationDBContext _dbContext; // Repository write metods nemaju SaveChangesAsync, pa to ovde pisem da smanjim No round trips ka Db - pogledaj Transakcije.txt i UnitOfWork.txt

        public CommentUpdateCommandHandler(ICommentRepositoryBase commentRepository, ApplicationDBContext dBContext)
        {
            _commentRepository = commentRepository;
            _dbContext = dBContext;
        }
             
        public async Task<Result<CommentUpdateResult>> Handle(CommentUpdateCommand command, CancellationToken cancellationToken)
        {
            var commentUpdated = await _commentRepository.UpdateAsync(command.Id, command.UpdateCommentCommandModel, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken); //Ako write repo metoda iznad fail, ovaj commit nece uspeti

            if (commentUpdated is null)
                return Result<CommentUpdateResult>.Fail("Comment not found");

            return Result<CommentUpdateResult>.Success(new CommentUpdateResult(commentUpdated.ToCommentDTOResponse()));
        }
    }
}
