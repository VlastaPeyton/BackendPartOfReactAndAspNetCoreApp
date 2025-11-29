using System.ComponentModel.DataAnnotations.Schema;
using Api.Value_Objects;

namespace Api.Models
{
    // Models folder sluzi za Entity klase jer te klase ce biti tabele u bazi. 
    // Comment table i ovo se koristi samo za EF Core tj bazu. Za endpoints se koristi CommentDTO
    
    [Table("Comments")] // Ime tabele u bazi explicitno definisano
    public class Comment 
    {
        public CommentId Id { get; set; } // PK and Index by default dok je bio int type, ali sam stavio custom type. Posto je custom type(Value Object) u OnModelCreating moram definisati da je PK.
        public int StockId { get; set; } // FK koji gadja Id u Stock klasi mora biti istog tipa kao Id u Stock klasi.
        public Stock? Stock { get; set; } // Navigational property => Comment.Include(Stock)
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.Now; 
        public string AppUserId { get; set; } // AppUserId je string, jer AppUser.Id je string. Ovo je FK koji gadja Id u AppUser klasi
        public AppUser AppUser { get; set; } // Navigational property => Comment.Include(AppUser)
        public bool IsDeleted { get; set; } = false; // Soft delete. Necu da brisem iz baze fizicki, vec IsDelete=true i u OnModelCreating stavim da ocitava samo redove gde je false i onda kao da sam ih izbrisao
                                                     // Migraciju uradi posle IsDelete jer sam dodao je naknadno 

        /*
          Ovo je 1-to-1 AppUser-Comment relationship jer Comment ima AppUser i AppUserId polje, pa EF zakljuci ovu vezu na osnovu imena polja bez da moram pisati u OnModelCreating.
          Ovo je 1-to-Many Stock-Comment relatiosnhip je Comment ima Stock i StockId polje, dok Stocl ima List<Comment> polje, pa EF zakljuci ovu vezu na osnovu imena polja bez da moram pisati u OnModelCreating.
        
          Mora Migration da se odradi nakon dodavanja ovog polja, ali pre toga izbrisati sve iz Comments tabele jer inace greska, pa je onda napunim.
         */
    }
}
