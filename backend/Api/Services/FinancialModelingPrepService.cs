using Api.DTOs.StockDTOs;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using Newtonsoft.Json;

namespace Api.Service
{   
    // U FE, zelim da ostavim komentar za npr "TSLA" stock, pa kada ukucam "TSLA" u search, prvo proveri ima li ga u bazi, ako nema, onda ga skine sa FinancialModelingPrep, stavi u bazu, pa onda mogu da mu napisem komentar
    
    // CancellationToken objasnjen u bilo kojoj Controller klasi.
    public class FinancialModelingPrepService : IFinacialModelingPrepService
    {   
        private readonly HttpClient _httpClient; // Za sljanje Request to Web API. U Program.cs mora builder.Services.AddHttpClient<IFinacialModelingPrepService, FinancialModelingPrepService>() 
        private readonly IConfiguration _configuration; // Dohvata appsettings.json
        private readonly ILogger<FinancialModelingPrepService> _logger;
        public FinancialModelingPrepService(HttpClient httpClient, IConfiguration configuration, ILogger<FinancialModelingPrepService> logger)
        {    
            _configuration = configuration;
            _httpClient = httpClient;  // Pogledaj IHttpClientFactory, HttpClient, Resilience.txt 
            _logger = logger;
        }

        public async Task<Stock?> FindStockBySymbolAsync(string symbol, CancellationToken cancellationToken) 
        // Stock?, a ne Stock, jer return null ima i onda da compiler ne kuka
        {   
            // Mora try-catch zbog HttpClient
            try
            {
                _logger.LogWarning($"Poziva FMP API");
                var result = await _httpClient.GetAsync($"https://financialmodelingprep.com/api/v3/profile/{symbol}?apikey={_configuration["FMPApiKey"]}", cancellationToken);
                // U Program.cs dodat AddStandardResilienceHandler() na AddHttpClient cime imam built-in retry, timeout, circuit breaker iz Microsoft.Extensions.Http.Resilience. Pogledaj IHttpClientFactory, HttpClient, Resilience.txt
                _logger.LogWarning($"{result}");
                // result contains StatusCode, Header, Body ...
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadAsStringAsync(cancellationToken); // content = Response Body. Niz objekata jer tako ovaj sajt vraca 
  
                    var stocks = JsonConvert.DeserializeObject<FinancialModelingPrepStockDTO[]>(content);
                    // Convert JSON to list of FinancialModelingPrepStock objects, jer FinancialModelingPrep API vraca niz tipa FinancialModelingPrepStockDTO, ali nam samo prvi element treba jer 1 element ocemo
                    var stock = stocks?.FirstOrDefault(); // jer nam samo prvi elem treba posto je to nas stock trazeni. Moze stocks[0] ali ako stocks prazan niz, bice greska, a ovako vrati null
                    
                    if (stock is not null)
                        return stock?.ToStockFromFinancialModelingPrepStockDTO(); // stock? - jer moze stocks biti prazan niz, a samim tim i stock biti 
                    else
                        return null;
                }
                else
                    return null;
            }
            catch (Exception ex) // Jer httpClient moze baciti exception neki
            {
                return null;
            }
        }
    }
}
