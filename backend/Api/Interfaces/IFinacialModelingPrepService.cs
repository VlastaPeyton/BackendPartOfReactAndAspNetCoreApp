using Api.Models;

namespace Api.Interfaces
{
    /* Za svaku klasu koja predstavlja Service pravim interface pomocu koga radim DI u Controller, dok u Program.cs pisem da prepozna interface kao zeljenu klasu
       Interface pravim za svaki servis zbog SOLID + lako testiranje (xUnit, Moq/FakeItEasy)

       Do sad smo sa ovog sajta ocitavali u ReactJS sve Stocks, ali sad cu da Seed DB with data from this site

       Ovo nije repository, vec service, jer FinancialModleingPrepService je web API koji gadjam tj nije moj licni

       CancellationToken objasnjen u bilo kojoj Controller klasi */
    public interface IFinacialModelingPrepService
    {
        Task<Stock> FindStockBySymbolAsync(string symbol, CancellationToken cancellationToken);// Stock se razaznaje by symbol npr : Microsoft je MSFT symbol
    }
}
