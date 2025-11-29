using Api.DTOs.CommentDTOs;

namespace Api.CQRS_and_behaviours.Comment.GetAll
{
    /* Request/Response mogu da imaju flat polja ili objekat. U oba slucaja, Request/Response polja moraju biti ista kao Query/Command i Result polja da bih mapiranje automatski moglo.
       Ne mora uvek postojati Request objekat, ali Query mora postojati. 
    */

    // Nemam CommetGetAllRequest objekat, jer zelim da GetAllCqrs i GetAll endpoints budu istog zaglavlja + da ista GetAll Repository metoda opsluzi Service i CQRS! 
    public record CommentGetAllResponse(List<CommentDTOResponse> commentResponseDTOs);
}
