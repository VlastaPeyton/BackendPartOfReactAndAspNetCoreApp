using Api.Models;

namespace Api.Interfaces
{   /* Repository pattern kako bi, umesto u PortfolioController, u PortfolioRepository definisali tela Endpoint metoda tj DB calls smestili u Repository.
       Koriscenje interface mora zbog SOLID + testiranje(xUnit,Moq/FakeItEasy) bez koriscenja baze + loose coupling.
    
       Ne korisitm DTO klase, vec Models klase, jer Repository direkt sa bazom komunicira.
       
       Objasnjenje za CancellationToken pogledaj u CommentController. 
       
       Za svaku klasu koja predstavlja Service pravim interface pomocu koga radim DI u Controller, dok u Program.cs pisem da prepozna interface kao zeljenu klasu

        Task<Portfolio?> je isto kao Task<Portfolio> samo se VS tad ne buni jer DeletePortfolio moze i null explicitno da vrati.
        
     */
    public interface IPortfolioRepository
    {
        Task<List<Stock>> GetUserPortfoliosAsync(AppUser user, CancellationToken cancellationToken);
        Task<Portfolio> CreateAsync(Portfolio portfolio, CancellationToken cancellationToken);
        Task<Portfolio?> DeletePortfolioAsync(AppUser appUser, string symbol, CancellationToken cancellationToken); 
    }
}
