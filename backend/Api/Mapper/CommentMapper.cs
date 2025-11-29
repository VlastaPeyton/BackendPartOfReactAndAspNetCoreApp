using Api.DTOs.CommentDTOs;
using Api.Models;

namespace Api.Mapper
{   
    // Da napravim Comment Entity klasu od DTO klasa kada pokrecem Repository metode.
    // Da napravim DTO klasu od Comment Entity klasa kada saljem data to FE in Endpoint
    public static class CommentMapper
    {   
        // Extension method za Comment, without calling parameters
        public static CommentDTOResponse ToCommentDTOResponse(this Comment comment)
        {
            return new CommentDTOResponse
            {
                Id = comment.Id.Value, // Id mapiram jer mapiram iz Comment to CommentDTO,a Id polje u Comment je ValueObject koji ima Value polje.
                Title = comment.Title,
                Content = comment.Content,
                CreatedOn = comment.CreatedOn,
                StockId = comment.StockId,
                CreatedBy = comment.AppUser?.UserName ?? "Nepoznata osoba" // Zbog ovoga mora Include(c=> c.AppUser) u CommentRepository kada dohvatam comment iz baze jer je AppUser navigacioni atribut i nece biti automatic dohvacen isto vazi i za dohvatanje Stock iz baze tj Stock.Include(s => s.AppUser)
                /* Ne mapiram AppUser,AppUserId,Stock,StockId polja, jer nisu prisutna u CommentDTO, dok AppUser/Stock su u Comment navigation property, koja, uz PK i FK polja u Comment/AppUser/Stock, sluze
                 da EF (ili rucno u OnModelCreating) definsie PK-FK vezu za Comment-AppUser/Stock. */
            };
        }

        // Extension method za CreateCommentRequestDTO, with calling parameter
        public static Comment ToCommentFromCreateCommentRequestDTO(this CreateCommentCommandModel command, int stockId)
        {
            return new Comment
            {
                // Id se ne mapira iz DTO,jer DTO nema Id, jer to tabela sama dodeli prema OnModelCreating zbog ValueGeneratedOnAdd za custom CommentId type
                Title = command.Title,
                Content = command.Content,
                StockId = stockId,
                // CreatedOn polje nisam mapirao, jer ne postoji u CreateCommentRequestDTO, pa bice DateTime.Now by default 
                /* Ne mapiram IsDelete, AppUser i Stock polja, jer nisu prisutna u CreateCommentRequestDTO, jer su u Comment to navigation property, koja, uz PK i FK polja u Comment/AppUser/Stock, sluze
                 da EF (ili ja u OnModelCreating) definsie PK-FK vezu za Comment-AppUser/Stock. */
            };
        }

        // Extension method za UpdateCommentRequestDTO, without calling parameters
        public static Comment ToCommentFromUpdateCommentRequestDTO(this UpdateCommentCommandModel command)
        {
            return new Comment
            {
                Title = command.Title,
                Content = command.Content,
                // Ostala non navigational property ili Id polja nisam mapirao jer to ne treba za ovaj slucaj  + UpdateCommentRequestDTO samo ova 2 polja ima
                /* Ne mapiram AppUser i Stock polja, jer nisu prisutna u UpdateCommentRequestDTO, jer su u Comment navigation property, koja, uz PK i FK polja u Comment/AppUser/Stock, sluze
                 da EF (ili ja u OnModelCreating) definsie PK-FK vezu za Comment-AppUser/Stock. */
            };
        }
    }
}
