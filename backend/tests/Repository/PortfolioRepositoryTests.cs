using System.Runtime.ConstrainedExecution;
using Api.Data;
using Api.Models;
using Api.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace tests.Repository
{
    public class PortfolioRepositoryTests
    {
        // Static jer je ovo helper 
        private static ApplicationDBContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

            return new ApplicationDBContext(options);
        }

        // CreateAsync tesitram samo za success jer failure scenario handluje u Controller
        [Fact]
        public async Task AddPortfolioTest()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var repository = new PortfolioRepository(dbContext);

            var user = new AppUser { Id = "user1", UserName = "TestUser" }; // Jer AppUser ima string za Id by default koji koristim
            var stock = new Stock { Id = 1, Symbol = "AAPL", CompanyName = "Apple Inc." };
            var portfolio = new Portfolio { AppUserId = user.Id, StockId = stock.Id, AppUser = user, Stock = stock };

            await dbContext.Users.AddAsync(user);
            await dbContext.Stocks.AddAsync(stock);
            // Pre dbContext.Portfolios.AddPortfolios.AddAsync(portfolio) moram da dodam (imam) u bazu user i stock na koje ce se odnositi portfolio
            await dbContext.SaveChangesAsync();

            // Act 
            var result = await repository.CreateAsync(portfolio, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.AppUserId.Should().Be(user.Id);
            result.StockId.Should().Be(stock.Id);

            var dbPortfolio = await dbContext.Portfolios.FindAsync(user.Id, stock.Id);
            dbPortfolio.Should().NotBeNull();
        }

        [Fact]
        public async Task DeletePortfolioTest()
        {
            // Arrange
            var dbContext = CreateDbContext();
            var repository = new PortfolioRepository(dbContext);

            var user = new AppUser { Id = "user1", UserName = "TestUser" }; // Jer AppUser ima string za Id by default koji koristim
            var stock = new Stock { Id = 1, Symbol = "AAPL", CompanyName = "Apple Inc." };
            var portfolio = new Portfolio { AppUserId = user.Id, StockId = stock.Id, AppUser = user, Stock = stock };

            await dbContext.Users.AddAsync(user);
            await dbContext.Stocks.AddAsync(stock);
            // Iako brisem portfolio, InMemory nema nijedan user, stock i portfolio iako u AddPortfolioTest  sam dodao, ali ovde CreateDbContext napravi novu bazu 
            // Kao kod AddPortfolioTest, moram prvo User i Stock dodati da bih mogo Portfolio
            await dbContext.Portfolios.AddAsync(portfolio);
            await dbContext.SaveChangesAsync();

            // Act
            var deleted = await repository.DeletePortfolioAsync(user, "AAPL", CancellationToken.None);

            // Assert
            deleted.Should().NotBeNull();
            var exists = await dbContext.Portfolios.AnyAsync();
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task GetUserPortfoliosTest()
        {
            // Arrange
            var dbContext = CreateDbContext(); // Nova prazna baza, nema veze sa 2 one iznad koriscene
            var repository = new PortfolioRepository(dbContext);

            var user = new AppUser { Id = "user1", UserName = "TestUser" }; // Jer AppUser ima string za Id by default koji koristim
            var stock1 = new Stock { Id = 1, Symbol = "AAPL", CompanyName = "Apple Inc." };
            var stock2 = new Stock { Id = 2, Symbol = "MSFT", CompanyName = "Microsoft Corp." };

            await dbContext.Users.AddAsync(user);
            await dbContext.Stocks.AddRangeAsync(stock1, stock2);

            await dbContext.Portfolios.AddRangeAsync(
                new Portfolio { AppUserId = user.Id, StockId = stock1.Id, AppUser = user, Stock = stock1 },
                new Portfolio { AppUserId = user.Id, StockId = stock2.Id, AppUser = user, Stock = stock2 }
            );
            await dbContext.SaveChangesAsync();

            // Act
            var result = await repository.GetUserPortfoliosAsync(user, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(s => s.Symbol == "AAPL");
            result.Should().Contain(s => s.Symbol == "MSFT");
        }
    }
}
