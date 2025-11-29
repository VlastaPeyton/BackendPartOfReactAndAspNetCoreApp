namespace Api.DTOs.CommentDTOs
{   
    // Ista polja kao CreateCommentRequestDTO
    public class CreateCommentCommandModel
    {   
        // Nema annonations, jer ovo nije ulazi objekat u endpoint, vec kad se ulazni objekat validira, onda se on mapira u ovaj objekat
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
