using System.Reflection.Metadata.Ecma335;
using Api.Data;
using Api.DTOs.StockDTO;
using Api.Helpers;
using Api.Interfaces;
using Api.Mapper;
using Api.Models;
using Api.Value_Objects;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json; // Jer u Controllers endpoint koristim ovaj NuGet, pa ne zelim da ga mesam za System.Text.Json 

namespace Api.Repository
{
    // Pogledaj Redis, Proxy & Decorator patterns.txt i JSON engine.txt
    public class CachedStockRepository : IStockRepository
    {
        private readonly IStockRepository _stockRepository;
        // CachedStockRepository dodaje cache logiku on top of StockRepository with Decorator pattern, pa u StockService/CQRS poziva se CachedStockRepository => moram koristim StockRepository tj IStockRepository jer u Program.cs IStockRepository registrovan kao StockRepository
        private readonly IDistributedCache _cache;
        // Program.cs with AddStackExchangeRedisCache konektujem Redis in Docker with IDistributedCache
        
        public CachedStockRepository(IStockRepository stockRepository, IDistributedCache cache)
        {
            _stockRepository = stockRepository;    
            _cache = cache;  
        }

        /* Key u Redis mora biti jedinstven, pa mogu birati izmedju Id i Symbol polja u Stock klasi => Symbol je uniquem, ali biram Id jer on je sto posto jedinstven, ali moram ga prebaciti u $"stock:{stock.Id}" jer IDistributedCache radi samo sa string key/value
           U Redis skladistim StockDTO, a ne Stock, jer ne valjda domain entity ostaje samo za EF Core i ne mesam ga sa cache + entitet ima FK-PK i navigational property pa onda je to jebeno malo resiti serijalizacijom, nego Stock->StockDTO i tu je sve reseno onda bez tih relationship stvari
        */
        public async Task<Stock> CreateAsync(Stock stock, CancellationToken cancellationToken)
        {   // Write-through strategija sa rucnom unosom u bazu pa posle u redis

            // Prvo, upisem u bazu Stock pomocu StockRepository metode
            await _stockRepository.CreateAsync(stock, cancellationToken); // Ne treba mi povratna vrednost ove metode i zato nisam naveo

            // Drugo, upisemu Redis StockDTO, gde je value tipa string(JSON) jer samo ova metoda je built-in preko IDistributedCache
            var stockDtoJson = JsonConvert.SerializeObject(stock.ToStockDtoResponse(), new JsonSerializerSettings
            {   // Za razliku od endpoint circular reference u Program.cs, ovde moram navoditi explicitno uvek 
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // sprecava circular reference
            });

            await _cache.SetStringAsync($"stock:{stock.Id}",
                                        stockDtoJson, 
                                        new DistributedCacheEntryOptions  // TTL ali ne fixni, vec na svakom citanju produzi mu se trajanje za jos tolko
                                        {
                                            SlidingExpiration = TimeSpan.FromMinutes(10) // AKo za 10min ne pricita se, brise se automatski, ali ako procita se, ostaje jos 10min 
                                        },
                                        cancellationToken);

            return stock;
        }

        public async Task<Stock?> DeleteAsync(int id, CancellationToken cancellationToken)
        {   //Cache-aside write + invalidation

            // Prvo, delete from DB
            var stock = await _stockRepository.DeleteAsync(id, cancellationToken);

            // Drugo, delete from Redis based on a key 
            await _cache.RemoveAsync($"stock:{id}", cancellationToken);

            return stock;
        }

        public async Task<List<Stock>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken)
        {
            // Nema potrebe cache ovde, jer GetAllAsync u StockRepository nema smisla za cache
            var listStocks = await _stockRepository.GetAllAsync(query, cancellationToken);
            return listStocks;
        }

        public async Task<Stock?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            // Cache-aside reading 

            // Prvo, proverim u cache
            var cachedStockDtoJson = await _cache.GetStringAsync($"stock:{id}", cancellationToken);
            // Ako ima u cache, onda vrati ga clientu
            if (!string.IsNullOrEmpty(cachedStockDtoJson))
            {
                var stockDTO = JsonConvert.DeserializeObject<StockDTOResponse>(cachedStockDtoJson)!; // Napravim StockDTO iz JSON jer u redis je JSON(StockDTO)
                var stock = new Stock
                {
                    //Mapiram Id, jer je ovo read scenario, dok u write ne bih smeo jer baza sama generise Id vrednost
                    Id = stockDTO.Id,
                    Symbol = stockDTO.Symbol,
                    CompanyName = stockDTO.CompanyName,
                    Purchase = stockDTO.Purchase,
                    Dividend = stockDTO.Dividend,
                    Industry = stockDTO.Industry,
                    MarketCap = stockDTO.MarketCap,
                    Comments = stockDTO.Comments.Select(c => new Comment
                    {
                        Id = CommentId.Of(c.Id),
                        StockId = stockDTO.Id,
                        Title = c.Title,
                        Content = c.Content,
                        CreatedOn = c.CreatedOn,
                        AppUser = new AppUser { UserName = c.CreatedBy}
                    }).ToList()

                }; // Napravim Stock iz StockDTO jer metoda to vraca
                return stock; 
            }
            // Ako nema u cahce, uzmem iz baze, upisem u cache i vratim clientu
            var dbStock = await _stockRepository.GetByIdAsync(id, cancellationToken);
            var stockDtoJson = JsonConvert.SerializeObject(dbStock.ToStockDtoResponse(), new JsonSerializerSettings
            {
                // Za razliku od endpoint circular reference u Program.cs, ovde moram navoditi explicitno uvek 
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // sprecava circular reference za Stock-Comment/Portfolio
            });
            await _cache.SetStringAsync($"stock:{id}", stockDtoJson, cancellationToken);

            return dbStock;
        }

        public async Task<Stock?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken)
        {
            // Necu da koristim Redis ovde, jer key je Stock.Id, a ne Stock.Symbol 
            var dbStock = await _stockRepository.GetBySymbolAsync(symbol, cancellationToken);
            return dbStock;
        }

        public async Task<bool> StockExists(int id, CancellationToken cancellationToken)
        {
            // Nema smisla koristiti cache ovde jer _stockRepository.StockExists ne vraca Stock objekat da bih ga upisao u cache, a da trazim u bazi, sporo je 
            bool stockExists = await _stockRepository.StockExists(id, cancellationToken);
            return stockExists;
        }

        public async Task<Stock?> UpdateAsync(int id, Stock stock, CancellationToken cancellationToken)
        {
            // Cache-aside with invalidation za write 

            // Prvo, azuriram bazu 
            var dbStock = await _stockRepository.UpdateAsync(id, stock, cancellationToken);
            if (dbStock is null)
                return null; // Ako ga nema u bazi, nema smisla ni cache menjati 

            // Drugo, invalidiram cache, gde nakon sledec GET ce se staviti u cache iz baze - dobra praksa jer stock ne azuriram cesto
            await _cache.RemoveAsync($"stock:{id}", cancellationToken); // Ako ga nema u Redis, ne baca gresku 

            return dbStock;

            /* ovaj kod je slucaj ako zelim da auziram i redis, ali to u praksi se ne radi cesto
            // Drugo, azuriram cache ako ga ima u cache - StockDTO je u Redis smesten
            var cachedStockDtoJson = await _cache.GetStringAsync($"stock:{id}", cancellationToken);
            // Ako ga ima u cache
            if (cachedStockDtoJson is not null)
            {
                var cachedStockDto = JsonConvert.DeserializeObject<StockDTO?>(cachedStockDtoJson);
                // Azuriram samo polja koja su navedena u UpdateStockRequestDTO
                cachedStockDto.Symbol = stock.Symbol;
                cachedStockDto.CompanyName = stock.CompanyName;
                cachedStockDto.Purchase = stock.Purchase;
                cachedStockDto.Dividend = stock.Dividend;
                cachedStockDto.Industry = stock.Industry;
                cachedStockDto.MarketCap = stock.MarketCap;

                // Upis nazad u Redis
                var updatedStockDtoJson = JsonConvert.SerializeObject(cachedStockDto);
                await _cache.SetStringAsync($"stock:{id}", updatedStockDtoJson, cancellationToken);
            }
            // Ako ga nema u cache
            else
            {
                var stockDTO = dbStock.ToStockDTO();
                var stockDtoJson = JsonConvert.SerializeObject(stockDTO);
                await _cache.SetStringAsync($"stock:{id}", stockDtoJson, cancellationToken);
            }
            
            return dbStock;
            */
        }
    }
}
