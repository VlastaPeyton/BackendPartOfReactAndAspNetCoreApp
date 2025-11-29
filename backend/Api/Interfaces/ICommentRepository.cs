using Api.Helpers;
using Api.Models;

namespace Api.Interfaces
{
    /* Repository pattern kako bi, umesto u CommentController, u CommentRepository definisali tela Endpoint metoda tj DB calls smestili u Repository.
       Koriscenje interface mora zbog SOLID + testiranje(xUnit,Moq/FakeItEasy) bez koriscenja baze + loose coupling.
       Ne koristim CommentDTO, vec Comment, jer Repository direktno sa bazom komunicira.
       
       Objasnjenje za CancellationToken pogledaj u CommentController. 

       Za svaku klasu koja predstavlja Service pravim interface pomocu koga radim DI u Controller, dok u Program.cs pisem da prepozna interface kao zeljenu klasu
     */
    public interface ICommentRepository
    {
        // Task, jer u CommentRepository bice definisane kao async + ce u CommentController Endpoint mozda da ih poziva pomocu await
        // Metoda koja ima Comment?, zato sto compiler warning prikaze ako method's return moze biti null jer FirstOrDefault/FindAsync moze i null da vrati
        Task<List<Comment>> GetAllAsync(CommentQueryObject commentQueryObject, CancellationToken cancellationToken);
        Task<Comment?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken);
        Task<Comment?> DeleteAsync(int id, CancellationToken cancellationToken); 
        Task<Comment?> UpdateAsync(int id, Comment comment, CancellationToken cancellationToken);
    }
}
