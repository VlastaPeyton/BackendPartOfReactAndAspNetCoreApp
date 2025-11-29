
using Api.DTOs.StockDTO;
using Api.DTOs.StockDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Helpers;
using Api.Interfaces;
using Api.Mapper;

namespace Api.Services
{   
    // Objasnjeno u CommentService
    public class StockService : IStockService
    {   
        private readonly IStockRepository _stockRepository; // Koristice CachedStockRepository, jer je on decorator on top of StockRepository 
        public StockService(IStockRepository stockRepository) => _stockRepository = stockRepository;

        public async Task<List<StockDTOResponse>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken)
        {
            var stocks = await _stockRepository.GetAllAsync(query, cancellationToken);  // Poziva CachedStockRepository jer je on Decorator over StockRepository
            // Iako Repository mora primiti Entity objekat, QueryObject ne mogu mapirati niti u jedan Entity objekat
            var stockDTOs = stocks.Select(s => s.ToStockDtoResponse()).ToList(); // Nema async, jer stocks nije u bazi, vec in-memory jer smo ga vec dohvatili iz baze
            
            return stockDTOs;
        }

        public async Task<Result<StockDTOResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.GetByIdAsync(id, cancellationToken);
            if (stock is null) // Jer GetByIdAsync moze i null da vrati 
                return Result<StockDTOResponse>.Fail("Nije pronadjen Stock by zeljeni id");

            // Mapiram Stock entity u StockDTOResponse 
            var stockDTOResponse = stock.ToStockDtoResponse();
            return Result<StockDTOResponse>.Success(stockDTOResponse);
        }

        public async Task<StockDTOResponse> CreateAsync(CreateStockCommandModel command, CancellationToken cancellationToken)
        {
            var stock = command.ToStockFromCreateStockRequestDTO();

            await _stockRepository.CreateAsync(stock, cancellationToken); // Iako CreateAsync ima return, ne treba "var result = _stockRepository.CreateAsync(stock), jer stock je Reference type, stoga promena stock u CreateAsync uticace i ovde

            var stockDtoResponse = stock.ToStockDtoResponse();

            return stockDtoResponse;
        }

        public async Task<Result<StockDTOResponse>> UpdateAsync(int id, UpdateStockCommandModel command, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.UpdateAsync(id, command.ToStockFromUpdateStockRequestDTO(), cancellationToken);
            if (stock is null)
                return Result<StockDTOResponse>.Fail("Not found stock");

            return Result<StockDTOResponse>.Success(stock.ToStockDtoResponse());

        }

        public async Task<StockDTOResponse> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var stock = await _stockRepository.DeleteAsync(id, cancellationToken);
            if (stock is null)
                throw new StockNotFoundException("Nije nadjen stock");
            
            return stock.ToStockDtoResponse();
        }
    }
}
