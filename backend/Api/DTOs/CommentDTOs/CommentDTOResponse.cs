namespace Api.DTOs.CommentDTOs
{
    /* Objasnjeno u StockDTO cemu sluzi DTO. 
     
       Sve isto kao Comment, osim IsDeleted i Stock/AppUser polja koje je Navigation Property, pa se ne moze dodati odavde nego preko PK-FK referencira zeljenu vrstu Stocks tabele. 
       Neam AppUserId iz Comment jer mi ne treba. 

       CommentDTO nema annotations za ModelState Validation, jer sluzi samo za prikaz svega iz Comment tj za slanje "Comment" to FE. */
    public class CommentDTOResponse
    {
        public int Id { get; set; } 
        public int? StockId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty; // Jer u Comment.cs imam AppUser polje
    }
}
