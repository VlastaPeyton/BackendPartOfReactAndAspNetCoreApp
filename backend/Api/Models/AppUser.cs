using Microsoft.AspNetCore.Identity;

namespace Api.Models
{   
    // Models folder sluzi za Entity klase jer te klase ce biti tabele u bazi. 

    // Zbog logovanja na App kroz AddIdentity u Program.cs mi treba ova klasa.
    public class AppUser : IdentityUser 
    {
        /*  IdentityUser predstavlja AspNetUsers tabela koja se automatski kreira u bazi.
         
            AppUser nasledio sva polja iz IdentityUser, gde najvise nas zanima Id, UserName, Email i HashedPassword polje. 
         Ako dodam custom field u AppUser (ali da nije Navigation attribute), to polje ce biti dodatna kolona u AspNetUsers tabeli.
         
          Zbog IdentityUser definicije, "ne mogu" AppUser:IdentityUser<AppUserId>, gde AppUserId bi bio Value Object, jer morao bih onda AppUserId:IEquatable sto je cimanje. 
         Ovako onda, Id je string by default u IdentityUser, jer i GUID je string.
            
         Zbog IdentityUser, imam automatski resen Race Condition problem pomocu ConcurrencyStamp kolone - pogledaj Race Condtitions.txt
         */

        // U ApplicationDbContext OnModelCreating definisem PK(AppUser.Id) vezu sa FK iz Porftolio.cs (AppUserId) mada to bi EF i sam znao 
        public List<Portfolio> Portfolios { get; set; } = new List<Portfolio>();  // Navigational property => AppUser.Include(Portfolios)
        /* Dobra praksa default value, zbog Register/Login endpoints i SeedAdminAsync gde mi ovo polje nije potrebno, pa da mu ne da neku nezeljenu vrednost 

           Obzirom da AppUser moze imati vise Stocks, nije dobra praksa da AppUser ima polje List<Stock> Stocks, vec svaki taj Stock od AppUser predstavljam kao Portfolio (npr Portfolio1 = {AppUserId1, StockId1, AppUser1, Stock1}), a sve Stocks od AppUser predstavlja kao List<Portfolio>.
      
          Ovo je 1-to-many AppUser-Portfolio veza, jer AppUser ima List<Portfolio> dok Portfolio ima AppUser i AppUserId polje, ali EF Core NECE zakljucit ovu vezu sam, jer u Portfolio neam Id polje, vec ga pravim od AppUserId+StockId, pa zato u OnModelCreating definisem taj composite PK i ovu vezu.
          Da je Portfolio.cs imao List<AppUser> AppUsers polje, ovo bi bilo many-to-many veza.
         */

        /* Dodajem kolone potrebne za Refresh Token jer Refresh Token is not stateless as JWT(Access Token) i mora biti povezan u bazi za odgovarajuceg user
         Ove kolone, zbog ?, su nullable, sto je dobro zbog SeedAdminAsync jer admina pravim na pocetku kroz BE (ne register kroz FE) i ne dodeljujem mu token dok se on ne login. */
        public string? RefreshTokenHash { get; set; } // Mora da se hashuje pre skaldistenja u bazu
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public DateTime? LastRefreshTokenUsedAt { get; set; }
        // Nakon dodavanja RefreshToken kolona, pokrenem migraciju da u AppUser tabeli u bazi ih dodam

    }
}
