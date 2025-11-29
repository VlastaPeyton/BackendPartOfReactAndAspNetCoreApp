using Api.DTOs.CommentDTOs;
using Api.Exceptions;
using Api.Exceptions_i_Result_pattern;
using Api.Helpers;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services
{   
    // Objasnjeno u AccountService
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IStockRepository _stockRepository;  // Koristi CachedStockRepository, jer je on decorator on top of StockRepository
        private readonly UserManager<AppUser> _userManager;
        private readonly IFinacialModelingPrepService _finacialModelingPrepService;
        
        public CommentService(ICommentRepository commentRepository, IStockRepository stockRepository, UserManager<AppUser> userManager,
                              IFinacialModelingPrepService finacialModelingPrepService)
        {
            _commentRepository = commentRepository;
            _stockRepository = stockRepository;
            _userManager = userManager;
            _finacialModelingPrepService = finacialModelingPrepService;
        }

        // Servis prima DTO iz kontroler ako je read endpoint
        // Controller mapira DTO u command i salje servisu, ako je write endpoint, a onda servis mapira command model u entity ako treba i salje u repository
        public async Task<List<CommentDTOResponse>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken)
        {
            var comments = await _commentRepository.GetAllAsync(commentQueryObject, cancellationToken); // Iako Repository prima/vraca samo Entity objekte, CommentQueryObject nisam mogao mapirati u odgovarajuci Entity objekat
            var commentResponseDTOs = comments.Select(x => x.ToCommentDTOResponse()).ToList(); // Iz IEnumerable (lista u bazi) pretvaram u listu zbog povratnog tipa metode

            return commentResponseDTOs;
        }
        public async Task<CommentDTOResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
            if (comment is null)
                throw new CommentNotFoundException("Comment not found");

            // Mapiram Comment entity u CommentResponseDTO
            var commentResponseDTO = comment.ToCommentDTOResponse();
            return commentResponseDTO;

        }
        public async Task<Result<CommentDTOResponse>> CreateAsync(string userName, string symbol, CreateCommentCommandModel command, CancellationToken cancellationToken)
        {
            // U FE, zelim da ostavim komentar za neki stock, pa u search kucam npr "tsla" i onda on trazi sve stocks koji pocinju na "tsla" u bazi pomocu GetBySymbolAsync
            var stock = await _stockRepository.GetBySymbolAsync(symbol, cancellationToken); // Nadje u bazy stock za koji ocu da napisem komentar 
            // ako nije naso "tsla" stock u bazi, nadje ga na netu pomocu FinancialModelingPrepService, pa ga ubaca u bazu, pa onda uzima ga iz baze da bih mi se pojavio na ekranu i da mogu da udjem u njega da comment ostavim
            if (stock is null)
            {   
                stock = await _finacialModelingPrepService.FindStockBySymbolAsync(symbol, cancellationToken);
                if (stock is null) // Ako nije ga naso na netu, onda smo lose ukucali u search i to je biznis greska
                    return Result<CommentDTOResponse>.Fail("Nepostojeci stock symbol koji nema ni na netu ili FMP API ne radi mozda"); 

                else // Ako ga naso na netu, ubaca ga u bazu
                    await _stockRepository.CreateAsync(stock, cancellationToken);
            }

            var appUser = await _userManager.FindByNameAsync(userName); // Pretrazi AspNetUser tabelu da nadje usera na osnovu userName
                                                                        // _userManager methods does not use cancellationToken

            if (appUser is null)
                return Result<CommentDTOResponse>.Fail("User not found in userManager"); 

            // Moram mapirati DTO(command) u Comment Entity jer Repository prima Entity kad god moze
            var comment = command.ToCommentFromCreateCommentRequestDTO(stock.Id);
            comment.AppUserId = appUser.Id;

            await _commentRepository.CreateAsync(comment, cancellationToken); // Iako CreateAsync ima return, ne treba "var result = _commentRepository.CreateAsync(comment), jer comment je Reference type, stoga promena comment u CreateAsync uticace i ovde

            // Moram mapirati Comment Entity u CommentDTOResponse
            var commentDTOResponse = comment.ToCommentDTOResponse();

            return Result<CommentDTOResponse>.Success(commentDTOResponse);
        }
        public async Task<Result<CommentDTOResponse>> DeleteAsync(int id, string userName, CancellationToken cancellationToken)
        {
            // Authorization kako user moze samo svoj komentar brisati 

            // Pronadji zeljeni komentar u bazi 
            var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
            if (comment is null)
                return Result<CommentDTOResponse>.Fail("Comment not found");

            // Pronadji trenutnog usera koji oce da obrise comment
            var appUser = await _userManager.FindByNameAsync(userName);

            if (appUser is null)
                return Result<CommentDTOResponse>.Fail("User not found in userManager");

            // User moze obrisati samo svoj komentar
            if (comment.AppUserId != appUser.Id) 
                return Result<CommentDTOResponse>.Fail("You can only delete your own comments");
            
            // Obrisi svoj komentar 
            var deletedComment = await _commentRepository.DeleteAsync(id, cancellationToken);
            if (deletedComment is null)
                return Result<CommentDTOResponse>.Fail("Comment not found");
            
            // Moram mapirati Comment Entity u DTO 
            return Result<CommentDTOResponse>.Success(comment.ToCommentDTOResponse());

        }
        public async Task<Result<CommentDTOResponse>> UpdateAsync(int id, UpdateCommentCommandModel command, CancellationToken cancellationToken)
        {   
            var comment = await _commentRepository.UpdateAsync(id, command.ToCommentFromUpdateCommentRequestDTO(), cancellationToken);
            if (comment is null)
                return Result<CommentDTOResponse>.Fail("Comment not found"); 

            // Moram mapirati Comment Entity u DTO 
            return Result<CommentDTOResponse>.Success(comment.ToCommentDTOResponse());
        }
    }
}
