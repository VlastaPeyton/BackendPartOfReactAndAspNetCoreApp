using Api.Data;
using Api.DTOs.CommentDTOs;
using Api.Events.IntegrationEvents;
using Api.Exceptions;
using Api.Exceptions_i_Result_pattern;
using Api.Helpers;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Localization;
using Api.Mapper;
using Api.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Api.Services
{   
    // Objasnjeno u AccountService
    public class CommentService : ICommentService
    {
        private readonly ICommentRepositoryBase _commentRepository; // Moze i ICommentRepository
        private readonly IStockRepositoryBase _stockRepository;  // Koristi CachedStockRepository, jer je on decorator on top of StockRepository tj StockRepositoryBase sada
        private readonly UserManager<AppUser> _userManager;
        private readonly IFinacialModelingPrepService _finacialModelingPrepService;
        private readonly IStringLocalizer<Resource> _localization;
        private readonly ApplicationDBContext _dbContext;  // Jer Repository nema SaveChangesAsync da bih smanjio br round trip ka Db - pogledaj Transakcija.txt i UnitOfWork.txt
        private readonly IPublishEndpoint _publishEndpoint; // Jer koristim Outbox pattern pomocu MassTransit

        public CommentService(ICommentRepositoryBase commentRepository, 
                              IStockRepositoryBase stockRepository, 
                              UserManager<AppUser> userManager,
                              IFinacialModelingPrepService finacialModelingPrepService,
                              IStringLocalizer<Resource> localization,
                              ApplicationDBContext dbContext,
                              IPublishEndpoint publishEndpoint)
        {
            _commentRepository = commentRepository;
            _stockRepository = stockRepository;
            _userManager = userManager;
            _finacialModelingPrepService = finacialModelingPrepService;
            _localization = localization;
            _dbContext = dbContext;
            _publishEndpoint = publishEndpoint;
        }

        // Servis prima DTO iz kontroler ako je read endpoint
        // Controller mapira DTO u command i salje servisu, ako je write endpoint, a onda servis mapira command model u entity ako treba i salje u repository
        public async Task<IEnumerable<CommentDTOResponse>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken)
        {
            var comments = await _commentRepository.GetAllAsync(commentQueryObject, cancellationToken); // Iako Repository prima/vraca samo Entity objekte, CommentQueryObject nisam mogao mapirati u odgovarajuci Entity objekat
            var commentResponseDTOs = comments.Select(x => x.ToCommentDTOResponse()).ToList(); // Iz IEnumerable (lista u bazi) pretvaram u listu zbog povratnog tipa metode

            return commentResponseDTOs;
        }
        public async Task<CommentDTOResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
            if (comment is null)
                throw new CommentNotFoundException($"{_localization["CommentNotFoundException"]}");

            // Mapiram Comment entity u CommentResponseDTO
            var commentResponseDTO = comment.ToCommentDTOResponse();
            return commentResponseDTO;

        }
        public async Task<Result<CommentDTOResponse>> CreateAsync(string userName, string symbol, CreateCommentCommandModel command, CancellationToken cancellationToken)
        {   
            // Jer imam 2 SaveChangesAsync gde drugi zavisi od uspeha prvog
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try 
            {
                // U FE, zelim da ostavim komentar za neki stock, pa u search kucam npr "tsla" i onda on trazi sve stocks koji pocinju na "tsla" u bazi pomocu GetBySymbolAsync
                var stock = await _stockRepository.GetBySymbolAsync(symbol, cancellationToken); // Nadje u bazy stock za koji ocu da napisem komentar 
                                                                                                // ako nije naso "tsla" stock u bazi, nadje ga na netu pomocu FinancialModelingPrepService, pa ga ubaca u bazu, pa onda uzima ga iz baze da bih mi se pojavio na ekranu i da mogu da udjem u njega da comment ostavim
                if (stock is null)
                {
                    stock = await _finacialModelingPrepService.FindStockBySymbolAsync(symbol, cancellationToken);
                    if (stock is null) // Ako nije ga naso na netu, onda smo lose ukucali u search i to je biznis greska
                        return Result<CommentDTOResponse>.Fail("Nepostojeci stock symbol koji nema ni na netu ili FMP API ne radi mozda");

                    // Ako ga naso na netu, konvertovao FmpDto to Stock ali bez Id i ubaca ga u bazu
                    await _stockRepository.CreateAsync(stock, cancellationToken); // Baza nije jos uvek dodelila Stock.Id jer ceka SaveChangesAsync, dok ChangeTracker stavio Stock.Id neku privremeno
                    await _dbContext.SaveChangesAsync(cancellationToken); // Baza generisala Stock.Id i ChangeTracker sad vidi tu vrednost
                }

                // stock.Id postoji bilo da je FMP naso stock pa upisan u bazu ili da l vec postojao u bazi 

                // Mora ovako, jer AppUser ima many Portfolios(Stock), pa ne moze preko stock da nadjem appUser 
                var appUser = await _userManager.FindByNameAsync(userName); // Pretrazi AspNetUser tabelu da nadje usera na osnovu userName
                                                                            // _userManager methods does not use cancellationToken
                if (appUser is null)
                    return Result<CommentDTOResponse>.Fail("User not found in userManager");

                // Moram mapirati DTO(command) u Comment Entity jer Repository prima Entity kad god moze
                var comment = command.ToCommentFromCreateCommentRequestDTO(stock.Id);
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

                // Moram mapirati Comment Entity u CommentDTOResponse jer imam Comment.Id i Comment.StockId
                var commentDTOResponse = comment.ToCommentDTOResponse();

                return Result<CommentDTOResponse>.Success(commentDTOResponse);
            }
            catch 
            {
                await transaction.RollbackAsync();
                throw;
            }

        }
        public async Task<Result<CommentDTOResponse>> DeleteAsync(int id, string userName, CancellationToken cancellationToken)
        {
            // Authorization kako user moze samo svoj komentar brisati 

            // Pronadji zeljeni komentar u bazi 
            var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
            if (comment is null)
                return Result<CommentDTOResponse>.Fail("Comment not found");

            // Pronadji trenutn logged usera koji oce da obrise comment, jer ako nije loggedin ne sme 
            var appUser = await _userManager.FindByNameAsync(userName);

            if (appUser is null)
                return Result<CommentDTOResponse>.Fail("User not found in userManager");

            // Logged user moze obrisati samo svoj komentar
            if (comment.AppUserId != appUser.Id) 
                return Result<CommentDTOResponse>.Fail("You can only delete your own comments");
            
            // Obrisi svoj komentar 
            var deletedComment = await _commentRepository.DeleteAsync(id, cancellationToken);

            if (deletedComment is null)
                return Result<CommentDTOResponse>.Fail("Comment not found");

            await _dbContext.SaveChangesAsync(cancellationToken); // Ako neka od write repo metoda iznad fail, ovaj commit nece uspeti

            // Moram mapirati Comment Entity u DTO 
            return Result<CommentDTOResponse>.Success(comment.ToCommentDTOResponse());

        }
        public async Task<Result<CommentDTOResponse>> UpdateAsync(int id, UpdateCommentCommandModel commandModel, CancellationToken cancellationToken)
        {   
            var commentUpdated = await _commentRepository.UpdateAsync(id, commandModel, cancellationToken);

            if (commentUpdated is null)
                return Result<CommentDTOResponse>.Fail("Comment not found");

            await _dbContext.SaveChangesAsync(cancellationToken); //Ako write repo metoda iznad fail, ovaj commit nece uspeti

            // Moram mapirati Comment Entity u DTO 
            return Result<CommentDTOResponse>.Success(commentUpdated.ToCommentDTOResponse());
        }
    }
}
