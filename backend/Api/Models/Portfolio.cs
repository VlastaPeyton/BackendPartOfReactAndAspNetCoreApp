using System.ComponentModel.DataAnnotations.Schema;
using Api.Value_Objects;

namespace Api.Models
{
    // Models folder sluzi za Entity klase jer te klase ce biti tabele u bazi. 

    /* Join table koja ce da predstavlja each Stock of each AppUser (zato Stock polje postoji), jer nije dobra praksa da AppUser ima Stocks listu kao polje i Stock da ima AppUsers listu kao polje.
       Takodje, predstavlja AppUser of each Stock i zato AppUser polje postoji.  
       Join table se koristi za Many-to-Many relationship (u ovom slucaju izmedju User-Stock). */

    [Table("Portfolios")] // Ime tabele u bazi explicitno definisano
    public class Portfolio // U principu, 1 Portfolio je 1 Stock za zeljenog AppUser-a
    {
        // U ApplicationDbContext OnModelCreating definisacu PK (i automatski Index bice) kao kombinaciju AppUserId i StockId, jer to ovde ne moze.
        public string AppUserId { get; set; } = default!;// AppUserId je string, jer AppUser.Id (IdentityUser.Id) String.
        public AppUser AppUser { get; set; } = default!; // Navigation property => Portfolio.Include(AppUser)
        public int StockId { get; set; }   // StockId je int, jer u Stock Id je int. Sada sam dodao StockId umesto int.
        public Stock Stock { get; set; } = default!; // Navigation property => Portfolio.Include(Stock)

        // Soft delete => Migracija da dodam ove kolone u bazu
        public bool IsDeleted { get; set; } = false; // Posto je PK=(AppUserId, StockId), kad user ponovo doda isti stock, radis restore (IsDeleted=false), a ne INSERT (jer red vec postoji).
        public DateTime? DeletedAt { get; set; } 

        /* AppUser i Stock ne mogu biti sa "?", jer moraju postojati za svaki Portfolio, jer AppUser.Id i Stock.Id moraju postojati vec u bazi kako bih ih povezao sa AppUserId i StockId
          jer AppUserId+StockId je composite PK koji pravim rucno u OnModelCreating.

           Ovo je 1-to-many AppUser-Portfolio veza, jer Portfolio ima AppUser i AppUserId polje, dok AppUser ima List<Portfolio> polje, pa EF zakljuci ovu vezu na osnovu imena polja bez da moram pisati u OnModelCreating.
           Ovo je 1-to-many Stock-Portfolio veza, jer Portfolio ima Stock i StockIde polje, dok Stock ima List<Portfolio> polje, pa EF zakljuci ovu vezu na osnovu imena polja bez da moram pisati u OnModelCreating.
         */
    }
    // Kad namestim sve za Portfolio -> Package Manager Console -> Migration da se pojavi ova tabela u bazi i onda rucno unesem kroz SQL Management jedan Portfolio na osnovu postojecih AppUsers i Stock
}
