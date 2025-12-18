using Api.Data;
using Api.DTOs.CommentDTOs;
using Api.Events.IntegrationEvents;
using Api.Helpers;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using Api.Value_Objects;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository
{
    /* Repository pattern kako bi, umesto u CommentController/CommentService/CQRS, u CommentRepository definisali tela endpoint metoda + DB calls u Repository se rade i zato
      ovde ne ide CommentDTO, vec samo Comment, jer (Models) Entity klase se koriste za EF Core tj Repository radi sa entity klasam jer direktno interaguje sa bazom.
               
      Service/CQRS prima/vraca DTO iz/u controller, mapira DTO u Entity i obratno, a Repository prima/vraca Entity iz/u Service/CQRS.
                 
      Repository ne baca custom exception niti vraca Result pattern, vec vraca null/entity/value. Repository moze baciti implicitni exception ako pukne nesto u bazi sto nije do nas, 
     ali to cu uhvatiti kao Exception u GlobalExceptionHandlingMiddleware.
      Service/CQRS Handler baca exception ili vraca Result pattern u zavisnosti sta mu repository vrati. 

      Repository za Write metode nece sadrzati SaveChangesAsync u vecini slucajeva, vec to bice u Service/CQRS ili Unit of work - pogledaj Transakcije.txt i UnitOfWork.txt
     */
    public class CommentRepository : ICommentRepository
    {   
        private readonly ApplicationDBContext _dbContext;

        public CommentRepository(ApplicationDBContext context)
        {   
            _dbContext = context;
        }

        /* Sve metode su async, jer u StockController bice pozvace pomocu await. 
           Metoda koja ima Task<Comment?>, zato sto compiler warning ce prikaze ako method's return moze biti null jer FirstOrDefault/FindAsync moze i null da vrati
          Ovo je dobra praksa da compiler ne prikazuje warning.
         */
        public async Task<IEnumerable<Comment>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken)
        {   // Iako Repository prima/vraca samo Entity objekte, CommentQueryObject nisam mogao mapirati u odgovarajuci Entity objekat
            // Sada koristim Soft delete, definisan u OnModelCreating da ocitava samo redove koji imaju IsDeleted=false

            var comments = _dbContext.Comments.Include(c => c.AppUser).Include(c => c.Stock).AsNoTracking().AsQueryable();  // Include is Eager loading. Deffered execution.
            // Comment ima AppUser polje i PK-FK vezu sa AppUser, pa zato moze Include(c => c.AppUser)
            // AsQueryable mora nakon Include kako bih zadrzao LINQ osobine, da mogu kasnije npr comments.Where(...), comments.OrderByDescending(...) itd.
            // Ovde nema EF tracking jer sam stavio AsNoTracking posto necu da modifikujem/brisem comments nakon ocitavanja iz baze, pa da ne dodajem overhead and memory zbog tracking

            // In if statement no need to AsQueryable again
            if (!string.IsNullOrWhiteSpace(commentQueryObject.Symbol))
                comments = comments.Where(s => s.Stock.Symbol == commentQueryObject.Symbol);
            // Ovaj Endpoint koristim cesto jer gledam Company profile za zeljeni Stock, a ta stranica ocitava sve komentare za njega, pa Stock.Symbol sam stavio ko Index da brze ocitava - pogledaj OnModelCreating

            if (commentQueryObject.IsDescending)
                comments = comments.OrderByDescending(c => c.CreatedOn);
            else
                comments = comments.OrderBy(c => c.CreatedOn);

            return await comments.ToListAsync(cancellationToken); // Mora ToListAsync, jer comments je AsQueryable (LINQ tipa tj SQL)
        }

        public async Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {   // Sada koristim Soft delete, definisan u OnModelCreating da ocitava samo redove koji imaju IsDeleted=false
            // FindAsync pretrazuje samo by Id i brze je od FirstOrDefaultAsync, ali ne moze ovde jer ima Include, pa mora FirstOrDefaultASync
            var existingComment = await _dbContext.Comments.Include(c => c.AppUser).AsNoTracking().FirstOrDefaultAsync(c => c.Id == CommentId.Of(id), cancellationToken); //  Mora ovako poredjenje jer Id je tipa CommentId
            // Id je PK i Index tako da pretrazuje bas brzo O(1) ili O(logn) u zavisnosti koja je struktura za index uzeta
            // EF start tracking changes done in existingComment after FirstOrDefaultAsync, ali ovde ne menjam/brisem objekat pa sam dodao AsNoTracking, jer tracking dodaje overhead and uses memory

            return existingComment;
        }

        public async Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken)
        {
            await _dbContext.Comments.AddAsync(comment, cancellationToken);
            /* EF start tracking comment object => Ako baza uradi nesto u vrsti koja predstavlja comment, EF to aplikuje u comment object i obratno. 
             EF change tracker marks comment tracking state to Added. Ne smem AsNoTracking, jer AddAsync(comment) nece hteti ako entity object nije tracked, 
             jer AsNoTracking se koristi samo za Read from Db metode gde ChangeTracker nepotreban.
            */

            // waits SaveChangesAsync to be updated in Db
            return comment; // Jos nema Id dodeljen u Db, ali nakon SaveChangesAsync ce mu biti zbog EF ChangeTracker
        }

        public async Task<Comment?> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var comment = await _dbContext.Comments.Include(c => c.AppUser).FirstOrDefaultAsync(c => c.Id == CommentId.Of(id), cancellationToken);  // Mora ovako poredjenje jer Id je tipa CommentId
            /* EF start tracking comment object, so every change made to comment will be applied to its corresponding row in Comment table after SaveChangesAsync
             Ne smem AsNoTracking, jer Remove(comment) nece hteti ako entity object nije tracked.
            
             Id je PK i Index, tako da FirstOrDefaultAsync u O(1)(ako je Recnik struktura indexa) ili O(logn) (ako je B-tree struktura indexa) nadje zeljeni komentar.
             */

            if (comment is null)
                return null;

            //_dbContext.Comments.Remove(comment); // Remove nema async, stoga nema ni cancellationToken.  EF in Change Tracker marks comment tracking state to Deleted
            // Umesto Remove, koristim Soft delete 
            comment.IsDeleted = true; // Zbog HasQueryFilter u OnModelCreating, selektuje redove gde IsDeleted=false

            // waits for SaveChangesAsync to apply changes to IsDeleted column of row of comment object

            return comment;
        }

        public async Task<Comment?> UpdateAsync(int id, UpdateCommentCommandModel commandModel, CancellationToken cancellationToken)
        {
            var existingComment = await _dbContext.Comments.Include(c => c.AppUser).FirstOrDefaultAsync(c => c.Id == CommentId.Of(id), cancellationToken); // Jer type(Comment.Id) = CommentId + HasConversion u OnModelCreating mora
            // Dohvatam Comment.AppUser nav atribut, jer zelim u CommentDTORepsonse da navedem i UserName

            if (existingComment is null)
                return null;

            existingComment.Title = commandModel.Title;
            existingComment.Content = commandModel.Content;
            // Ostala existingComment polja nisam mapirao i ona ostaju kao u bazi

            // waits for SaveChangesAsync to apply changes to column represented by existingComment object

            return existingComment;
        }

        public async Task DeleteByUserIdAsync(string userId, DateTime utcNow, CancellationToken cancellationToken)
        {
            // Pogledaj u UserRepository zasto je ovo Bulk insert i kako smanjuje br of round trips to Db

            /* U OnModelCreating pise "entityHasQueryFilter(c => !c.IsDeleted)" EF automatski uzima samo redove gde
              IsDeleted=false. Da bih postigao idempotentnost, jer sad brisanjem usera zelim da obrisem i njegove comments, 
              gde su mozda neki od comments vec obrisani na drugi nacin (CommentRepository.DeleteAsync), moram IgnoreQueryFilter 
              iako cu time da obrisem vec obrisane, nema veze, jer ovako postizem idempotency.
             */
            await _dbContext.Comments 
                            .IgnoreQueryFilters() 
                            .Where(c => c.AppUserId == userId && !c.IsDeleted)
                            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true)
                                                      .SetProperty(c => c.DeletedAt, utcNow),
                            cancellationToken);
        }
    }
}
