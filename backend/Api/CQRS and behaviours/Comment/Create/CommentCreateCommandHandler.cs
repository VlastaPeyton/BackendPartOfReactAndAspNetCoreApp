using Api.CQRS;
using Api.DTOs.CommentDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace Api.CQRS_and_behaviours.Comment.Create
{   
    public record CommentCreateCommand(string UserName, string Symbol, CreateCommentCommandModel CreateCommenCommandModel) : ICommand<Result<CommentCreateResult>>;
    public record CommentCreateResult(CommentDTOResponse CommentDTOResponse);

    public class CommentCreateCommandValidator : AbstractValidator<CommentCreateCommand>
    {
        public CommentCreateCommandValidator()
        {
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Symbol).NotEmpty();
            // Necu sad da validiram CreateCommenCommandModel polja iako bih trebao
        }
    }

    public class CommentCreateCommandHandler : ICommandHandler<CommentCreateCommand, Result<CommentCreateResult>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IFinacialModelingPrepService _finacialModelingPrepService;
        private readonly UserManager<AppUser> _userManager;
        public CommentCreateCommandHandler(ICommentRepository commentRepository, IStockRepository stockRepository, 
                                           IFinacialModelingPrepService fmpService, UserManager<AppUser> userManager)
        {
            _commentRepository = commentRepository;
            _stockRepository = stockRepository;
            _finacialModelingPrepService = fmpService;
            _userManager = userManager;
        }
        public async Task<Result<CommentCreateResult>> Handle(CommentCreateCommand command, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.GetBySymbolAsync(command.Symbol, cancellationToken);
            if (stock is null)
            {
                stock = await _finacialModelingPrepService.FindStockBySymbolAsync(command.Symbol, cancellationToken);
                if (stock is null) 
                    return Result<CommentCreateResult>.Fail("Nepostojeci stock symbol koji nema ni na netu ili FMP API ne radi mozda");
                else 
                    await _stockRepository.CreateAsync(stock, cancellationToken);
            }
            var appUser = await _userManager.FindByNameAsync(command.UserName);
            if (appUser is null)
                return Result<CommentCreateResult>.Fail("User not found in userManager");
            
            var comment = command.CreateCommenCommandModel.ToCommentFromCreateCommentRequestDTO(stock.Id);
            comment.AppUserId = appUser.Id;

            await _commentRepository.CreateAsync(comment, cancellationToken);

            // Moram mapirati Comment Entity u CommentDTOResponse pre nego sto CQRS Handler vrati podatke Controlleru
            var commentDTOResponse = comment.ToCommentDTOResponse();

            return Result<CommentCreateResult>.Success(new CommentCreateResult(commentDTOResponse));
        }
    }
}
