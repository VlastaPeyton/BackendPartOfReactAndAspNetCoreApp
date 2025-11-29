using Api.DTOs.Stock;
using Api.Helpers;
using Api.Models;

namespace Api.Interfaces
{
    /* Repository pattern kako bi, umesto u StockController, u StockRepository definisali tela Endpoint metoda tj DB calls smestili u Repository.
       Koriscenje interface mora zbog SOLID + testiranje(xUnit,Moq/FakeItEasy) bez koriscenja baze + loose coupling.
    
       Ne korisitm StockDTO, vec Stock, jer Repository direkt sa bazom komunicira. 
       
       Objasnjenje za CancellationToken pogledaj u CommentController. 
      
       Za svaku klasu koja predstavlja Service pravim interface pomocu koga radim DI u Controller, dok u Program.cs pisem da prepozna interface kao zeljenu klasu
    */
    public interface IStockRepository
    {
        // Task, jer u StockRepository metode bice definisane kao async + ce u StockController Endpoint mozda da ih poziva pomocu await
        // Metoda koja ima Stock?, zato sto compiler warning prikaze ako method's return moze biti null jer FirstOrDefault/FindAsync moze i null da vrati 

        Task<List<Stock>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken); 
        Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken); 
        Task<Stock> CreateAsync(Stock stock, CancellationToken cancellationToken); 
        Task<Stock?> UpdateAsync(int id, Stock stock, CancellationToken cancellationToken); 
        Task<Stock?> DeleteAsync(int id, CancellationToken cancellationToken); 
        Task<bool> StockExists(int id, CancellationToken cancellationToken);
        Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken); 
    }
}
