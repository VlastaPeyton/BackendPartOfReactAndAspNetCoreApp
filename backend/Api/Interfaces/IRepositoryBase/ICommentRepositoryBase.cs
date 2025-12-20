using Api.DTOs.CommentDTOs;
using Api.Helpers;
using Api.Models;

namespace Api.Interfaces.IRepositoryBase
{
    public interface ICommentRepositoryBase : IBaseRepository<Comment>
    {   
        // Overloaded method iz IBaseRepository, pa u CommentRepositoryBase imacu GetAllAsync/UpdateAsync odavde i iz IBaseRepository, ali koristicu odavde
        Task<IEnumerable<Comment>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken);
        Task<Comment?> UpdateAsync(int id, UpdateCommentCommandModel commandModel, CancellationToken cancellationToken);

        // Ovaj metod postoji samo u ICommentRepositorybase, ne i u (I)BaseRepository
        Task DeleteByUserIdAsync(string userId, DateTime utcNow, CancellationToken cancellationToken);
    }
}
