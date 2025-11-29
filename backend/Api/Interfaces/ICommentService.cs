using Api.DTOs.CommentDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Helpers;

namespace Api.Interfaces
{   
    public interface ICommentService
    {  
        // Servis prima DTO iz kontroler ako je read endpoint
        // Controller mapira DTO u command i salje servisu, ako je write endpoint, a onda servis mapira command u entity ako treba i salje u repository
        Task<List<CommentDTOResponse>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken);
        Task<CommentDTOResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<Result<CommentDTOResponse>> CreateAsync(string userName, string symbol, CreateCommentCommandModel command, CancellationToken cancellationToken);
        Task<Result<CommentDTOResponse>> DeleteAsync(int id, string userName, CancellationToken cancellationToken);
        Task<Result<CommentDTOResponse>> UpdateAsync(int id, UpdateCommentCommandModel command, CancellationToken cancellationToken);
    }
}
