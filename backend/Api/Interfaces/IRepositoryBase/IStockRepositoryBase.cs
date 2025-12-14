using Api.Helpers;
using Api.Models;

namespace Api.Interfaces.IRepositoryBase
{
    public interface IStockRepositoryBase : IBaseRepository<Stock>
    {   
        // Overloaded metod iz IBaseRepository, pa u StockRepositoryBase imacu GetAllAsync odavde i iz IBaseRepository, ali koristicu samo ovaj
        Task<IEnumerable<Stock>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken);

        // Metodi koji nisu u (I)BaseRepository, vec samo u StockRepositoryBase, jer postoje u StockRepository i nigde drugde
        Task<bool> StockExists(int id, CancellationToken cancellationToken);
        Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken);
    }
}
