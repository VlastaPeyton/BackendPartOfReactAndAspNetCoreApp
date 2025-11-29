using Api.DTOs.StockDTO;
using Api.DTOs.StockDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Helpers;

namespace Api.Interfaces
{
    public interface IStockService
    {   
        // Objasnjeno u ICommentService 
        public Task<List<StockDTOResponse>> GetAllAsync(StockQueryObject query, CancellationToken cancellationToken);
        public Task<Result<StockDTOResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
        public Task<StockDTOResponse> CreateAsync(CreateStockCommandModel command, CancellationToken cancellationToken);
        public Task<Result<StockDTOResponse>> UpdateAsync(int id, UpdateStockCommandModel command,CancellationToken cancellationToken);
        public Task<StockDTOResponse> DeleteAsync(int id, CancellationToken cancellationToken);
    }
}
