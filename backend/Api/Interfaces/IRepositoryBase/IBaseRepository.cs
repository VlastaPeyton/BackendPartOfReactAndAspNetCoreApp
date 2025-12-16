namespace Api.Interfaces.IRepositoryBase
{   
    /* Prvo sam napravio IStock/IComment/IPortfolioRepository i Stock/Comment/PortfolioRepository, ali Stock/CommentRepository imaju
      zajednicke CRUD metode, pa da smanjim boilerplate code, pravim IBaseRepository i BaseRepository kako bih tu napravio zajednicku 
      CRUD logiku za Stock/CommentRepository. 
       Napravicu nove IStock/CommentRepositoryBase i Stock/CommentRepositoryBase, jer ne zelim da rusim postojece (I)Stock/CommentRepository klase. 
       Ne radim za PortfolioRepository, jer on nema nista zajednicko CRUD kao ova 2.
       
       Ovde pisem opste argumente za CRUD zajednicke metode na osnovu Stock/CommentRepository, dok u (I)Stock/CommentRepositoryBase pisem override/overload njihove ako zatreba, 
    a zatrebace, jer nisu sve CRUD metode istog potpisa i tela u StockRepository i CommentRepository.

      U Program.cs NE registrujem services.AddScoped<IBaseRepository, BaseRepository>(), jer BaseRepository je abstract class + sluzi za code reuse, a ne 
      konkretnu implementaciju.
     */
    public interface IBaseRepository<T> where T : class
    {
        Task<T> CreateAsync(T entity, CancellationToken cancellationToken);

        // GetAllAsync metoda u Stock/CommentRepository prima argument, zato cu je overload u (I)Stock/CommentRepositoryBase, a ovde je pisem  samo jer je zajednicka
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken); 
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken);
        // UpdateAsync je ovde da ispostuje CRUD, ali cu u IStock/CommentRepositoryBase da overload jer mi ne treba ovakav potpis, pa ovu necu ni koristiti
        Task<T?> UpdateAsync(int id, T entity, CancellationToken cancellationToken);
        Task<T?> DeleteAsync(int id, CancellationToken cancellationToken);
    }
}
