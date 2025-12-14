using Api.Data;
using Api.Helpers;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository.BaseRepository
{
    public class StockRepositoryBase : BaseRepository<Stock>, IStockRepositoryBase
    {
        // Nasledio BaseRepository<Stock> kako bih koristio sve njegove metode, osim onih koje moram da override. 
        // Implementirao IStockRepositoryBase, zbog SOLID, da bih u Service/CQRS mogo koristiti IStockRepositoryBase, a da se poziva StockRepositoryBase

        public StockRepositoryBase(ApplicationDBContext dBContext) : base(dBContext) { }

        // CreateAsync mi odgovara iz BaseRepository, pa necu je override 

        // Overload metod, pa imacu GetAllAsync iz IStockRepositoryBase i iz IBaseRepository, ali mi treba ovaj
        public async Task<IEnumerable<Stock>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken)
        {
            var stocks = _dbContext.Stocks
                                   .Include(c => c.Comments)
                                   .ThenInclude(c => c.AppUser)
                                   .AsNoTracking()
                                   .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.CompanyName))
                stocks = stocks.Where(s => s.CompanyName.Contains(query.CompanyName));

            if (!string.IsNullOrWhiteSpace(query.Symbol))
                stocks = stocks.Where(s => s.Symbol.Contains(query.Symbol));

            if (!string.IsNullOrWhiteSpace(query.SortBy) && query.SortBy.Equals("Symbol", StringComparison.OrdinalIgnoreCase))
                stocks = query.IsDescending ? stocks.OrderByDescending(s => s.Symbol) : stocks.OrderBy(s => s.Symbol);

            var skipNumber = (query.PageNumber - 1) * query.PageSize;

            return await stocks.Skip(skipNumber).Take(query.PageSize).ToListAsync(cancellationToken);
        }

        public override async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Stocks.Include(c => c.Comments).ThenInclude(s => s.AppUser).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);                                                                                                                                                          // EF track changes after FirstOrDefaultAsync ali mi to ovde ne treba i zato nema znak jednakosti + AsNoTracking ima jer tracking ubaca overhead and memory bespotrebno ovde
        }

        public override async Task<Stock?> UpdateAsync(int id, Stock stock, CancellationToken cancellationToken)
        {
            var existingStock = await _dbContext.Stocks.FindAsync(id, cancellationToken);
            if (existingStock is null) 
                return null;

            existingStock.Symbol = stock.Symbol;
            existingStock.CompanyName = stock.CompanyName;
            existingStock.Purchase = stock.Purchase;
            existingStock.Dividend = stock.Dividend;
            existingStock.Industry = stock.Industry;
            existingStock.MarketCap = stock.MarketCap;

            await _dbContext.SaveChangesAsync(cancellationToken); 

            return existingStock;
        }

        public override async Task<Stock?> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var stock = await _dbContext.Stocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            
            if (stock is null)
                return null;

            _dbContext.Stocks.Remove(stock);      
            await _dbContext.SaveChangesAsync(cancellationToken); 

            return stock;
        }

        public async Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken)
        {
            return await _dbContext.Stocks.AsNoTracking().FirstOrDefaultAsync(s => s.Symbol == symbol, cancellationToken);
        }

        public async Task<bool> StockExists(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Stocks.AnyAsync(s => s.Id == id, cancellationToken);
        }
    }
}
