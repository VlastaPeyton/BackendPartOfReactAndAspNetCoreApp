using Api.Helpers;
using Api.Models;

namespace Api.Interfaces.IRepositoryBase
{
    public interface ICommentRepositoryBase : IBaseRepository<Comment>
    {   
        // Overloaded method iz IBaseRepository, pa u CommentRepositoryBase imacu GetAllAsync odavde i iz IBaseRepository, ali koristicu samo ovaj
        Task<IEnumerable<Comment>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken);
    }
}
