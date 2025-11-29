using Api.DTOs.CommentDTOs;

namespace Api.CQRS_and_Validation.Comment
{
    //public record CommentGetByIdRequest(int Id); 
    /* Moze i "id" da se zove, ali je ruzno. Svakako, .NET automatski mapira "id" iz [HttpGet("{id:int}")] u ovaj "Id" jer je case insesitive.
       Obzirom da samo 1 argument prostog tipa postoji u CommentGetByIdRequest, onda ne treba mi taj Request objekat, ali Query objekat mora postojati uvek .*/

    // CommentGetByIdResponse je ustvari CommentDTOResponse
    public record CommentGetByIdResponse(CommentDTOResponse commentDTOResponse);
}
