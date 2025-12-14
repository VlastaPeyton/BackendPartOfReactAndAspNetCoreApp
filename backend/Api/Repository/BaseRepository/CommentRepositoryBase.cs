using Api.Data;
using Api.Events.IntegrationEvents;
using Api.Helpers;
using Api.Interfaces.IRepositoryBase;
using Api.Models;
using Api.Value_Objects;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository.BaseRepository
{
    public class CommentRepositoryBase : BaseRepository<Comment>, ICommentRepositoryBase
    {
        // Nasledio BaseRepository<Stock> kako bih koristio sve njegove metode, osim onih koje moram da override. 
        // Implementirao ICommentRepositoryBase, zbog SOLID, da bih u Service/CQRS mogo koristiti ICommentRepositoryBase, a da se poziva CommentRepositoryBase

        private readonly IPublishEndpoint _publishEndpoint;
        public CommentRepositoryBase(ApplicationDBContext context, IPublishEndpoint publishEndpoint) : base(context)
        {
            _publishEndpoint = publishEndpoint;
        }

        public override async Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken)
        {
            await _dbContext.Comments.AddAsync(comment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _publishEndpoint.Publish(new CommentCreatedIntegrationEvent { Text = "Komentar upisan" }, cancellationToken);

            return comment;
        }

        // Overload metod iz (I)BaseRepository, pa imacu GetAllAsync iz ICommentRepositoryBase i iz IBaseRepository, ali mi treba ovaj
        public async Task<IEnumerable<Comment>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken)
        {
            var comments = _dbContext.Comments.Include(c => c.AppUser).Include(c => c.Stock).AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(commentQueryObject.Symbol))
                comments = comments.Where(s => s.Stock.Symbol == commentQueryObject.Symbol);

            if (commentQueryObject.IsDescending)
                comments = comments.OrderByDescending(c => c.CreatedOn);
            else
                comments = comments.OrderBy(c => c.CreatedOn);

            return await comments.ToListAsync(cancellationToken);
        }

        public override async Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Comments.Include(c => c.AppUser).AsNoTracking().FirstOrDefaultAsync(c => c.Id == CommentId.Of(id), cancellationToken);
        }

        public override async Task<Comment?> UpdateAsync(int id,  Comment comment, CancellationToken cancellationToken)
        {
            var existingComment = await _dbContext.Comments.FindAsync(id, cancellationToken); 
            if (existingComment is null)
                return null;

            existingComment.Title = comment.Title;
            existingComment.Content = comment.Content;

            await _dbContext.SaveChangesAsync(cancellationToken); 

            return existingComment;
        }

        public override async Task<Comment?> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var comment = await _dbContext.Comments.Include(c => c.AppUser).FirstOrDefaultAsync(c => c.Id == CommentId.Of(id), cancellationToken);  
            
            if (comment is null)
                return null;

            comment.IsDeleted = true; 

            await _dbContext.SaveChangesAsync(cancellationToken); 

            return comment;
        }
    }
}
