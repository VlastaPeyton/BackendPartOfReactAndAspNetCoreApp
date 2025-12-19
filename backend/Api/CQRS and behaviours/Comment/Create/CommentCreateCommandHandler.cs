using Api.CQRS;
using Api.Data;
using Api.DTOs.CommentDTOs;
using Api.Events.IntegrationEvents;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Mapper;
using Api.Models;
using FluentValidation;
using MassTransit;
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
            // Necu sad da validiram ostala CreateCommenCommandModel polja iako bih trebao
        }
    }

    public class CommentCreateCommandHandler : ICommandHandler<CommentCreateCommand, Result<CommentCreateResult>>
    {
        private readonly ICommentRepositoryBase _commentRepository; // Koristim ovo i IStockRepositoryBase umesto IStock/CommentRepository - pogledaj BaseRepository folder
        private readonly IStockRepositoryBase _stockRepository;    
        private readonly IFinacialModelingPrepService _finacialModelingPrepService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDBContext _dbContext; // Repository write metods nemaju SaveChangesAsync, pa to ovde pisem da smanjim No round trips ka Db - pogledaj Transakcije.txt i UnitOfWork.txt
        private readonly IPublishEndpoint _publishEndpoint;
        public CommentCreateCommandHandler(ICommentRepositoryBase commentRepository, 
                                           IStockRepositoryBase stockRepository, 
                                           IFinacialModelingPrepService fmpService, 
                                           UserManager<AppUser> userManager,
                                           ApplicationDBContext dbContext,
                                           IPublishEndpoint publishEndpoint)
            
        {
            _commentRepository = commentRepository;
            _stockRepository = stockRepository;
            _finacialModelingPrepService = fmpService;
            _userManager = userManager;
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
        }
        public async Task<Result<CommentCreateResult>> Handle(CommentCreateCommand command, CancellationToken cancellationToken)
        {
            // Jer imam 2 SaveChangesAsync gde drugi zavisi od uspeha prvog
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try 
            {
                var stock = await _stockRepository.GetBySymbolAsync(command.Symbol, cancellationToken);
                if (stock is null)
                {
                    stock = await _finacialModelingPrepService.FindStockBySymbolAsync(command.Symbol, cancellationToken);
                    if (stock is null)
                        return Result<CommentCreateResult>.Fail("Nepostojeci stock symbol koji nema ni na netu ili FMP API ne radi mozda");

                    // Ako ga naso na netu, konvertovao FmpDto to Stock ali bez Id i ubaca ga u bazu
                    await _stockRepository.CreateAsync(stock, cancellationToken); // Baza nije jos uvek dodelila Stock.Id jer ceka SaveChangesAsync, dok ChangeTracker stavio Stock.Id neku privremeno
                    await _dbContext.SaveChangesAsync(cancellationToken); // Baza generisala Stock.Id i ChangeTracker sad vidi tu vrednost

                }
                // stock.Id postoji bilo da je FMP naso stock pa upisan u bazu ili da l vec postojao u bazi 

                var appUser = await _userManager.FindByNameAsync(command.UserName);
                if (appUser is null)
                    return Result<CommentCreateResult>.Fail("User not found in userManager");

                var comment = command.CreateCommenCommandModel.ToCommentFromCreateCommentRequestDTO(stock.Id);
                comment.AppUserId = appUser.Id;

                await _commentRepository.CreateAsync(comment, cancellationToken); // Comment.Id ima neku temp vrednost u ChangeTracker privremeno dok SaveChangesAsync ne upise u bazi pravu

                /*  Outbox pattern via MassTransit + Publish event to message broker via MassTransit, pa MassTransit presretne event u Publish metodi i prvo upise u Outbox tabelu, pa 
                 MassTransit built-in background job periodicno proverava Outbox tabelu i salje neposlati integration event na message broker i oznaci kao poslat u Outbox table.
                 Ako nesto pukne izmedju SaveChangesAsync i Publish, event nikad ne ode u message broker jer ga Publish ne upise u Outbox.
                 Publish mora pre SaveChangesAsync da Outbox bi azuriralo.*/
                await _publishEndpoint.Publish(new CommentCreatedIntegrationEvent { Text = "Komentar upisan" }, cancellationToken);

                await _dbContext.SaveChangesAsync(cancellationToken); // Ako neka od write repo metoda iznad fail, ovaj commit nece uspeti 
                                                                      // Baza dodelila Id u Comment i ChangeTracker tu vrednost sad vidi

                await transaction.CommitAsync(cancellationToken);

                // Moram mapirati Comment Entity u CommentDTOResponse + SaveChangesAsync u comment dodao Id 
                var commentDTOResponse = comment.ToCommentDTOResponse();

                return Result<CommentCreateResult>.Success(new CommentCreateResult(commentDTOResponse));
            }
            catch 
            {
                await transaction.RollbackAsync();
                throw;
            }
            
        }
    }
}
