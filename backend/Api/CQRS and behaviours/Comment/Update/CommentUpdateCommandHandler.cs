using Api.CQRS;
using Api.DTOs.CommentDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
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
        private readonly ICommentRepository _commentRepository;
        public CommentUpdateCommandHandler(ICommentRepository commentRepository) => _commentRepository = commentRepository;
             
        public async Task<Result<CommentUpdateResult>> Handle(CommentUpdateCommand command, CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.UpdateAsync(command.Id, command.UpdateCommentCommandModel.ToCommentFromUpdateCommentRequestDTO(), cancellationToken);
            if (comment is null)
                return Result<CommentUpdateResult>.Fail("Comment not found");

            return Result<CommentUpdateResult>.Success(new CommentUpdateResult(comment.ToCommentDTOResponse()));
        }
    }
}
