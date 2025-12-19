using System.Runtime.ConstrainedExecution;
using Api.Data;
using Api.Interfaces.IRepositoryBase;
using Api.Query_objects;
using MassTransit.Internals.GraphValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository.BaseRepository
{
    /* U Program.cs NE registrujem services.AddScoped<IBaseRepository, BaseRepository>(), jer BaseRepository je abstract class + sluzi za code reuse, a ne
      konkretnu implementaciju.
    */

    // Kao i IBaseRepository, samo za Stock/CommentRepository cu napraviit, jer Portfolio nema CRUD
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
    {   // Ne mora biti abstract, ali pozeljno da bih znao da ne treba mi instanca ove klase, vec samo kao roditelj da sluzi ova klasa
        protected readonly ApplicationDBContext _dbContext;

        protected BaseRepository(ApplicationDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Ovo je "hook" koji svaka izvedena klasa mora da implementira
        protected abstract IQueryable<T> BuildQuery(QueryObjectParent query);

        /* Sve metode su virtual, da mogu ih override ako mi zatreba custom logika u Stock/CommentRepositoryBase, jer nemaju bas sve ove metode istu implementaciju 
          u Stock/CommentRepositoryBase.
        */
        public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken)
        {
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
            
            return entity;
            /* Ovo telo je dovoljno za StockRepository i ovu metodu necu override u StockRepositoryBase, ali za CommentRepository fali "_publishEndpoint...", 
             pa cu to u CommentRepositoryBase uraditi => ovu metodu override u CommentRepositoryBase.
            */
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync(QueryObjectParent query, CancellationToken cancellationToken)
        {   
            var queryIzChildMetode = BuildQuery(query);

            var skip = (query.PageNumber - 1) * query.PageSize;

            return await queryIzChildMetode.Skip(skip).Take(query.PageSize).ToListAsync(cancellationToken);
            /* Ovaj metod ce biti overloaded u (I)Stock/CommentRepositoryBase, jer u Stock/CommentRepository ima argument odgovarajuci +
             u oba slucaja ova metoda sadrzi Include i jos neke provere, pa zato cu koristiti GetAllAsync overloaded iz (I)Stock/CommentRepositoryBase, a 
             ne ovaj metod. Ova metoda je navedena ovde, tek onako, da ispostuje samo zato jer se pojavljuje u oba StockRepository i CommentRepository.
            */
        }
        public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
            // Ovu telo nije dovoljno za Stock/CommentRepository, pa ovu metodu cu override u Stock/CommentRepositoryBase.
        }
        public virtual async Task<T?> UpdateAsync(int id, T entity, CancellationToken cancellationToken)
        {
            var existingEntity = await GetByIdAsync(id, cancellationToken);
            if (existingEntity is null)
                return null;

            _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);

            return existingEntity;

            // Ovo telo nije dovoljno za Stock/CommentRepository, pa cu u Stock/CommentRepositoryBase da override ovu metodu
        }
        public virtual async Task<T?> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity is null)
                return null;

            _dbContext.Set<T>().Remove(entity);

            return entity;
            // Ovo telo nije dovoljno za Stock/CommentRepository, pa cu u Stock/CommentRepository da override ovu metodu
        }
    }
}
