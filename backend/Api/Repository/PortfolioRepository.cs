using Api.Data;
using Api.Interfaces;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository
{   
    // Objasnjeno u CommentRepository 
    public class PortfolioRepository : IPortfolioRepository
    {   
        private readonly ApplicationDBContext _dbContext;
        public PortfolioRepository(ApplicationDBContext context) 
        {
            _dbContext = context;
        }

        // Sve metode su async, jer u PortfolioController bice pozvace pomocu await. 
        public async Task<Portfolio> CreateAsync(Portfolio portfolio, CancellationToken cancellationToken)
        {
            await _dbContext.Portfolios.AddAsync(portfolio, cancellationToken); // EF starts tracking portfolio changes. 
            /* Portfolio ima composite PK (AppUserId+StockId) i 2 FK (AppUserId i StockId), defined in OnModelCreating, ali nema nikad automatsku dodelu vrednosti 
            kao za non-composite Id prilikom SaveChangesAsync Db ne zna kako da dodeli vrednost u composite PK, pa stoga mora u Db vec postojati AppUser i Stock 
            kako bi postojali AppUserId i StockId. */
            
            // treba SaveChangesAsync da se promena zabelezi u bazi
            
            return portfolio; 
        }

        public async Task<Portfolio?> DeletePortfolioAsync(AppUser appUser, string symbol, CancellationToken cancellationToken)
        {   
            var portfolio = await _dbContext.Portfolios.Include(p => p.Stock).FirstOrDefaultAsync(p => p.AppUserId == appUser.Id && p.Stock.Symbol.ToLower() == symbol.ToLower(), cancellationToken);  // Immediate execution
            // EF start tracking changes in portfolio object. Ne smem AsNoTracking, jer Remove(portfolio) ne moze za untracked entity objects. 
            // U OnModelCreating objasnjeno zasto sam Stock.Symbol Indexirao.
            // Include mi treba zbog ToPortfolioDtoResponse

            // FirstOrDefaultAsync moze vratiti null, ali mi ova provera treba zbog Remove 
            if (portfolio == null)
                return null;

            _dbContext.Portfolios.Remove(portfolio); // EF stop tracking portfolio and set its tracking to Detached
            
            // waits for SaveChangesAsync to apply changes in Db

            return portfolio;     
        }

        public async Task<IEnumerable<Stock>> GetUserPortfoliosAsync(AppUser user, CancellationToken cancellationToken)
        {   // Objasnjeno Entity klasama i u OnModelCreating veza izmedju Portfolio-AppUser/Stock 
            return await _dbContext.Portfolios.Where(u => u.AppUserId == user.Id)
                                        // Za svaki Portfolio kreira Stock na osnovu podatka iz Portfolio, jer 1 Stock je 1 Portfolio 
                                        .Select(portfolio => new Stock
                                        {
                                        Id = portfolio.StockId, 
                                        Symbol = portfolio.Stock.Symbol, 
                                        CompanyName = portfolio.Stock.CompanyName,
                                        Purchase = portfolio.Stock.Purchase,
                                        Dividend = portfolio.Stock.Dividend,
                                        Industry = portfolio.Stock.Industry,
                                        MarketCap = portfolio.Stock.MarketCap
                                        // Ne prosledjujem Comments and Portfolios polja, jer portfolio ih nema + imaju default vrednost u Stock bas zato + to su navigation property koja sluze za dohvatanje toga samo kad zatreba
                                        }).AsNoTracking().ToListAsync(cancellationToken);
            // AsNoTracking, jer ne azuriram nista sto sam procitao iz baze, obzirom da tracking adds overhead and memory
        }

        public async Task<Portfolio?> GetPortfolioBySymbol(string symbol, CancellationToken cancellationToken)
        {
            var portfolio = await _dbContext.Portfolios.FirstOrDefaultAsync(p => p.Stock.Symbol.ToLower() ==  symbol.ToLower(), cancellationToken);

            return portfolio;
        }
        public async Task SoftDeleteByUserIdAsync(string userId, DateTime utcNow, CancellationToken cancellationToken)
        {
            // Pogledaj u UserRepository zasto je ovo Bulk insert koji smanjuje br of round trips to Db

            /*U OnModelCreating pise "entityHasQueryFilter(c => !c.IsDeleted)" EF automatski uzima samo redove gde
              IsDeleted = false.Da bih postigao idempotentnost, jer sad brisanjem usera zelim da obrisem i njegove portfolios,
              gde su mozda neki od portfolios vec obrisani na drugi nacin(koji ne postoji za sada), moram IgnoreQueryFilter
              iako cu time da obrisem vec obrisane, nema veze, jer ovako postizem idempotency. */
            await _dbContext.Portfolios
                            .IgnoreQueryFilters() 
                            .Where(p => p.AppUserId == userId && !p.IsDeleted)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true)
                                                      .SetProperty(p => p.DeletedAt, utcNow),
                            cancellationToken);
        }

    }
}