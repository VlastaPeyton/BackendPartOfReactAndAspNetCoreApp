using Api.Data;
using Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Api.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDBContext _dbContext;
        
        public UserRepository(ApplicationDBContext context)
        {
            _dbContext = context;
        }

        public async Task<int> SoftDeleteAsync(string userId, DateTime utcNow, CancellationToken cancellationToken)
        {   
            /* ExecuteUpdateAsync vraca int u zavisnosti koliko redova je Where pronasao, a u mom slucaju vraca:
                  - 1 ako user nadjen i soft delete
                  - 0 ako vec soft deleted 

              Ovo je Bulk update gde EF ne uradi SELECT, pa azurira objekte posebno pa SaveChangesAsync, vec odma azurira u bazi
              gde izbegavam SELECT cime smanjujem br round trips ka bazi sa 2 na 1. - pogledaj Bulk insert, update, delete.txt
            */
            return await _dbContext.Users.Where(u => u.Id == userId && !u.IsDeleted)
                                         .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDeleted, true)
                                                                   .SetProperty(u => u.DeletedAt, utcNow)
                                                                   // Invalidacija RefreshToken podataka
                                                                   .SetProperty(u => u.RefreshTokenHash, (string?)null)
                                                                   .SetProperty(u => u.RefreshTokenExpiryTime, (DateTime?)null)
                                                                   .SetProperty(u => u.LastRefreshTokenUsedAt, (DateTime?)null)
                                                                   , cancellationToken);
        }
    }
}
