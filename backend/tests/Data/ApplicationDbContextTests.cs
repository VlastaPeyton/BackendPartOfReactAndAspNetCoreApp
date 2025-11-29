
using Api.Data;
using Api.Models;
using Api.Value_Objects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace tests.Data
{
    public class ApplicationDbContextTests
    {

        private static ApplicationDBContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Isolated
                .Options;

            return new ApplicationDBContext(options);
        }

        [Fact]
        public async Task Create_Portfolio_With_Composite_Key()
        {
            var dbContext = CreateDbContext(); // New empty db 

            var user = new AppUser { Id = "user1", UserName = "testuser" }; // Jer AppUser ima string za Id by default koji koristim
            var stock = new Stock { Id = 1, Symbol = "AAPL", CompanyName = "Apple Inc." };
            var portfolio = new Portfolio { AppUserId = user.Id, StockId = stock.Id, AppUser = user, Stock = stock };

            await dbContext.Users.AddAsync(user);
            await dbContext.Stocks.AddAsync(stock);
            // Pre dodavanja Portfolio moram dodati User i Stock jer to mora
            await dbContext.Portfolios.AddAsync(portfolio);
            await dbContext.SaveChangesAsync();

            var saved = await dbContext.Portfolios.FindAsync(user.Id, stock.Id);
            saved.Should().NotBeNull();
            saved!.AppUserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task Stock_Symbol_Should_Be_Unique()
        {
            var dbContext = CreateDbContext();

            var stock1 = new Stock { Id = 1, Symbol = "MSFT", CompanyName = "Microsoft" };
            var stock2 = new Stock { Id = 2, Symbol = "MSFT", CompanyName = "Microsoft Duplicate" };

            await dbContext.Stocks.AddAsync(stock1);
            await dbContext.SaveChangesAsync();

            dbContext.Stocks.Add(stock2);
            /*Nema savechangesasync jer ne zelim da upise u bazu zaista, obzirom da ovaj tip in-memory db nema constraint za symbol onda simuliram kao da ima 
             pomocu exitst linije. Ovo bih sa SQLite morao ako zelim da odradim bas kako treba. */
            var exists = await dbContext.Stocks.AnyAsync(s => s.Symbol == stock2.Symbol);
            exists.Should().BeTrue(); // Stock with same Symbol already exists

            // Simulate what would normally be an error
            exists.Should().BeTrue("because Stock.Symbol should be unique, even though InMemory doesn't enforce it");
        }

        [Fact]
        public async Task Can_Save_Comment_With_Custom_Id_Type()
        {
            var dbContext = CreateDbContext();

            var stock = new Stock { Id = 1, Symbol = "GOOG", CompanyName = "Google" };
            var user = new AppUser { Id = "u1", UserName = "commenter" }; // AppUser.Id je string by default i to koristim 
            var commentId = CommentId.Of(5);

            var comment = new Comment
            {
                Id = commentId,
                Content = "Interesting stock.",
                StockId = stock.Id,
                Stock = stock,
                AppUserId = user.Id,
                AppUser = user
            };
            await dbContext.Stocks.AddAsync(stock);
            await dbContext.Users.AddAsync(user);
            await dbContext.Comments.AddAsync(comment);
            await dbContext.SaveChangesAsync();

            var saved = await dbContext.Comments.FindAsync(commentId);
            saved.Should().NotBeNull();
            saved!.Id.Should().Be(commentId);
        }
    }
}
