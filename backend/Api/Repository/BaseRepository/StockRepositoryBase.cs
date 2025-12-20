using Api.Data;
using Api.DTOs.StockDTOs;
using Api.Helpers;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Models;
using Api.Query_objects;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository.BaseRepository
{
    public class StockRepositoryBase : BaseRepository<Stock>, IStockRepositoryBase
    {
        // Nasledio BaseRepository<Stock> kako bih koristio sve njegove metode, osim onih koje moram da override. 
        // Implementirao IStockRepositoryBase, zbog SOLID, da bih u Service/CQRS mogo koristiti IStockRepositoryBase, a da se poziva StockRepositoryBase
        // Objasnjene metode u StockRepository jer ista su im tela kao ovde

        public StockRepositoryBase(ApplicationDBContext dBContext) : base(dBContext) { }

        protected override IQueryable<Stock> BuildQuery(QueryObjectParent query)
        {  // Ovde mora QueryObjectParent, jer cu je pozvati za Stock/CommentRepositoryBase iz BaseRepository.GetAllAsync, a znace zbog polimorfizma 
            return _dbContext.Stocks
                            .Include(c => c.Comments)
                            .ThenInclude(c => c.AppUser)
                            .AsNoTracking()
                            .AsQueryable(); // Nema AsSplitQuery, iako Stock.Comments je kolekcija, ali Stock.AppUser nije ! 
        }

        // CreateAsync mi odgovara iz BaseRepository, pa necu je override 

        // Overload metod, pa imacu GetAllAsync iz IStockRepositoryBase i iz IBaseRepository, a koristim oba u ovoj metodi
        public async Task<IEnumerable<Stock>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken)
        {   
            return await base.GetAllAsync(query, cancellationToken); // Mogu proslediti StockQueryObject jer on nasledio QueryObjectParent
        }

        public override async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Stocks.Include(c => c.Comments).ThenInclude(s => s.AppUser).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);                                                                                                                                                          // EF track changes after FirstOrDefaultAsync ali mi to ovde ne treba i zato nema znak jednakosti + AsNoTracking ima jer tracking ubaca overhead and memory bespotrebno ovde
        }

        // Overload metod, pa imacu UpdateAsync iz IStockRepositoryBase i iz IBaseRepository, ali mi treba ovaj
        public  async Task<Stock?> UpdateAsync(int id, UpdateStockCommandModel commandModel, CancellationToken cancellationToken)
        {
            var existingStock = await _dbContext.Stocks.FindAsync(id, cancellationToken);
            if (existingStock is null) 
                return null;

            existingStock.Symbol = commandModel.Symbol;
            existingStock.CompanyName = commandModel.CompanyName;
            existingStock.Purchase = commandModel.Purchase;
            existingStock.Dividend = commandModel.Dividend;
            existingStock.Industry = commandModel.Industry;
            existingStock.MarketCap = commandModel.MarketCap;

            return existingStock;
        }

        public override async Task<Stock?> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var stock = await _dbContext.Stocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            if (stock is null)
                return null;

            _dbContext.Stocks.Remove(stock);

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
