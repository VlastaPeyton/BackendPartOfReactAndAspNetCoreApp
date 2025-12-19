using Api.Data;
using Api.DTOs.StockDTOs;
using Api.Helpers;
using Api.Interfaces;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository
{
    // Objasnjeno u CommentRepository
    public class StockRepository : IStockRepository
    {
        private readonly ApplicationDBContext _dbContext; 

        public StockRepository(ApplicationDBContext context) 
        {
            _dbContext = context;
        }

        // Metoda koja ima Stock?, zato sto compiler warning prikaze ako return moze biti null jer FirstOrDefault moze i null da vrati

        public async Task<IEnumerable<Stock>> GetAllAsync(StockQueryObject query,CancellationToken cancellationToken)
        {   // Iako treba da primi Entity argument, ne moze, jer QueryObject ne moze da se mapira niti u jedan Entity objekat

            var stocks = _dbContext.Stocks.Include(c => c.Comments).ThenInclude(c => c.AppUser).AsNoTracking().AsQueryable(); // Dohvati sve stocks + njihove komentare + AppUser svakog komentara. Ovo je deffered execution
            // Stock ima List<Comment> polje i FK-PK vezu sa Comment i zato moze include. Bez tog polja, moralo bi kompleksiniji LINQ.
            // Include.ThenInclude ne vraca IQueryable, vec IIncludableQueryable, pa mora AsQueryable da zadrzim LINQ osobine, pa mogu kasnije npr stocks.Where/Skip/Take/ToListAsync
            // Ovde nema EF change tracking zbog AsNoTracking, obzirom da ne azuriram ono sto sam dohvatio, pa da neam bespotrebni overhead and memory zbog tracking

            var skipNumber = (query.PageNumber - 1) * query.PageSize; // Pagination

            return await stocks.Skip(skipNumber).Take(query.PageSize).ToListAsync(cancellationToken); // Immediate execution 
        }

        public async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {  // Objasnjene za Include je u GetAllAsync
           // FirstOrDefaultAsync moze da vrati null ako ne nadje, pa zato nemam if(stock is null) return null 
           // FindAsync je brze od FirstOrDefaultAsync, ali nakon Include ne moze FindAsync.
           return await _dbContext.Stocks.Include(c => c.Comments).ThenInclude(s => s.AppUser).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken); // Dohvati zeljeni stock na osnovu Id polja + njegove komentare                                                                                                                                                         // EF track changes after FirstOrDefaultAsync ali mi to ovde ne treba i zato nema znak jednakosti + AsNoTracking ima jer tracking ubaca overhead and memory bespotrebno ovde
        }

        public async Task<Stock> CreateAsync(Stock stock, CancellationToken cancellationToken)
        {   // Stock nema setovano Id polje jer to baza sama popuni nakon SaveChangesAsync 
            await _dbContext.Stocks.AddAsync(stock, cancellationToken); // EF starts tracking stock object i sve sto baza promeni u vrsti koja se odnosi na ovaj object, EF ce da promeni u stock object i obratno.
            // EF in Change Tracker marks stock tracking state to Added. Ne sme AsNoTracking, jer to samo se radi za reading from DB + sto ne bi moglo SaveChangesAsync onda.
            
            // waits for SaveChangesAsync to be applyied in Db and to later apply that Id to ChangeTracker jer trenutno ChangeTracker ima Stock.Id=0 jer Id je tipa int

            return stock; // isti stock, samo sa azuriranim Id poljem, jer EF does tracking
        }

        public async Task<Stock?> UpdateAsync(int id, UpdateStockCommandModel commandModel, CancellationToken cancellationToken)
        {   // FindAsync moze da vrati null i zato Stock? da compiler se ne buni 
            var existingStock = await _dbContext.Stocks.FindAsync(id, cancellationToken); // Brze nego FirstOrDefaultAsync, ali nema Include (jer ne trazim Comments/Portfolios pa mi ne treba), pa moze FindAsync 
            // EF will track existingStock after FindAsync, so any change made to existingStock will apply to DB after SaveChangesAsync. Ne sme AsNoTracking, jer azuriram objekat u tabeli.
            // EF in Change Tracker marks existingStock tracking state to Unchanged jer ga je tek naso u bazi
            if (existingStock is null) // Mora da proverim, jer FindAsync vrati null ako nije nasla
                return null;

            // Azuriram samo polja koja su navedena u UpdateStockRequestDTO
            existingStock.Symbol = commandModel.Symbol;
            existingStock.CompanyName = commandModel.CompanyName;
            existingStock.Purchase = commandModel.Purchase;
            existingStock.Dividend = commandModel.Dividend;
            existingStock.Industry = commandModel.Industry;
            existingStock.MarketCap = commandModel.MarketCap;

            // waits for SaveChangesAsync da upise promene u bazu za vrstu koja odgovara existingStock

            return existingStock;
        }

        public async Task<Stock?> DeleteAsync(int id, CancellationToken cancellationToken)
        {   
            var stock = await _dbContext.Stocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken); 
            // EF tracks stock object, so every change made to stock will be applied to its corresponding row in Stocks table after SaveChangesAsync. Ne smem AsNoTracking jer Remove(stock) ne moze za untracked object.
            // As Id is PK for Stock, it is automatically also Index so this is fastest way for query
            
            // Iako FirstOrDefaultAsync vraca null, treba mi uslov ovaj zbog Remove
            if (stock is null)
                return null;

            _dbContext.Stocks.Remove(stock); // EF in Change Tracker marks stock tracking state to Deleted
            
            // needs SaveChangesAsync to be removed from Db

            return stock;
        }

        public async Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken)
        {   // FirstOrDefaultAsync moze da vrati null i zato Stock? return type, da se compiler ne buni. 
            return await _dbContext.Stocks.AsNoTracking().FirstOrDefaultAsync(s => s.Symbol == symbol, cancellationToken); // Ne moze FindAsync, iako je brze, jer FindAsync pretrazuje po Id samo
            // Iako ovaj Endpoit ne koristim cesto, jer retko pisem komentare za stock u FE, Stock.Symbol sam stavio u Index zbog DeletePortfolio, pa automatski i ovde brze ce da pretrazi
            // EF track changes after FirstOrDefaultAsync ali mi to ovde ne treba i zato nema znak jednakosti
        }

        public async Task<bool> StockExists(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Stocks.AnyAsync(s => s.Id == id, cancellationToken);
        }
    }
}
