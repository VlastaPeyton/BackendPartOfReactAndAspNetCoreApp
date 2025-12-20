using Api.DTOs.StockDTOs;
using Api.Helpers;
using Api.Models;

namespace Api.Interfaces.IRepositoryBase
{
    public interface IStockRepositoryBase : IBaseRepository<Stock>
    {   
        // Overloaded metod iz IBaseRepository, pa u StockRepositoryBase imacu GetAllAsync/UpdateAsync odavde i iz IBaseRepository, ali koristicu odavde
        Task<IEnumerable<Stock>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken);
        Task<Stock?> UpdateAsync(int id, UpdateStockCommandModel commandModel, CancellationToken cancellationToken);

        // Metodi koji nisu u (I)BaseRepository, vec samo u (I)StockRepositoryBase
        Task<bool> StockExists(int id, CancellationToken cancellationToken);
        Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken);
    }
}
