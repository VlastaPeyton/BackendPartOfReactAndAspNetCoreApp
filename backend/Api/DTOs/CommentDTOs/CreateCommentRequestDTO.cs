using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.CommentDTOs
{   
    // Objasnjeno u StockDTO cemu sluzi DTO. 

    // Mora imati annotations jer ovu klasu koristim za writing to DB Endpoint pa da ModelState moze da validira polja pre nego sto mapiram u CreateCommentCommandModel

    public class CreateCommentRequestDTO
    {   
        [Required]
        [MinLength(5,ErrorMessage = "Title must be at least 5 chars")]
        [MaxLength(200, ErrorMessage = "Title cannot be over 200 chars")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MinLength(5, ErrorMessage = "Content must be at least 5 chars")]
        [MaxLength(200, ErrorMessage = "Content cannot be over 200 chars")]
        public string Content { get; set; } = string.Empty;
    }
}
