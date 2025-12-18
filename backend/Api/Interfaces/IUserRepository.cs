namespace Api.Interfaces
{   
    // Nece imati pandan u IUserRepositoryBase, jer nece imati CRUD kao Stock/Comment tabele
    public interface IUserRepository
    {
        Task<int> SoftDeleteAsync(string userId, DateTime utcNow, CancellationToken cancellationToken);

    }
}
