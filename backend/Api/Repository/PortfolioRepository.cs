using Api.Data;
using Api.Interfaces;
using Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository
{   /* Repository pattern kako bi, umesto u PortfolioController, u PortfolioRepository definisali tela Endpoint metoda tj DB calls smestili u Repository.
    
       Ne korisitm DTO klase, vec Entity klase, jer Repository direkt sa bazom komunicira. 
       Objasnjenje za CancellationToken pogledaj u StockRepository. 
      
      Controller radi mapiranje entity klasa u DTO osim ako koristim CQRS, jer nije dobro da repository vrati DTO obzriom da on radi sa domain i treba samo za entity klase da zna
    */
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
            /* Portfolio ima composite PK (AppUserId+StockId) + AppUserId i StockId su FK, defined in OnModelCreating, ali nema nikad automatsku dodelu vrednosti kao za non-composite Id, 
            pa stoga, DB will not insert value to composite PK jer to morao sam ja da odradim pre toga, obzirom da je composite PK. Tj moram da imam vec AppUser i Stock u bazi pre toga. */
            await _dbContext.SaveChangesAsync(cancellationToken); // Sacuvan portfolio u bazi i autonatski mu popuni composide PK (Id) polje => EF azurira portfolio objekat
            return portfolio; 
        }

        public async Task<Portfolio?> DeletePortfolioAsync(AppUser appUser, string symbol, CancellationToken cancellationToken)
        {   
            var portfolio = await _dbContext.Portfolios.FirstOrDefaultAsync(p => p.AppUserId == appUser.Id && p.Stock.Symbol.ToLower() == symbol.ToLower(), cancellationToken); 
            // EF start tracking changes in portfolio object. Ne smem AsNoTracking, jer Remove(portfolio) ne moze za untracked entity objects. 
            // U OnModelCreating objasnjeno zasto sam Stock.Symbol Indexirao.

            // FirstOrDefaultAsync moze vratiti null, ali mi ova provera treba zbog Remove 
            if (portfolio == null)
                return null;

            _dbContext.Portfolios.Remove(portfolio); // EF stop tracking portfolio and set its tracking to Detached
            await _dbContext.SaveChangesAsync(cancellationToken);

            return portfolio;
             
        }

        public async Task<List<Stock>> GetUserPortfoliosAsync(AppUser user, CancellationToken cancellationToken)
        {   // Objasnjeno Entity klasama i u OnModelCreating veza izmedju Portfolio-AppUser/Stock 
            return await _dbContext.Portfolios.AsNoTracking().Where(u => u.AppUserId == user.Id)
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
                                        }).ToListAsync(cancellationToken);
            // AsNoTracking, jer ne azuriram nista sto sam procitao iz baze, obzirom da tracking adds overhead and memory
        }
    }
}