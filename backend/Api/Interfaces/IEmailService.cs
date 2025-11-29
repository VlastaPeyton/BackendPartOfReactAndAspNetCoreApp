namespace Api.Interfaces
{
    /* Za svaku klasu koja predstavlja Service pravim interface pomocu koga radim DI u Controller, dok u Program.cs pisem da prepozna interface kao zeljenu klasu
     Interface pravim za svaki servis zbog SOLID + lako testiranje (xUnit, Moq/FakeItEasy) */

    public interface IEmailService
    {
        Task SendEmailAsync(string primalac, string subject, string message);

    }
}
