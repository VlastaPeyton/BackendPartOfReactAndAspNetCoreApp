using Api.CQRS_and_behaviours.Comment.Create;
using Api.CQRS_and_behaviours.Comment.GetAll;
using Api.CQRS_and_behaviours.Comment.Update;
using Api.CQRS_and_Validation.Comment;
using Api.CQRS_and_Validation.Comment.Delete;
using Api.DTOs.CommentDTOs;
using Api.Extensions;
using Api.Helpers;
using Api.Interfaces;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    
    [Route("api/comment")] // https://localhost:port/api/comment
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ISender _sender;
        public CommentController(ICommentService commentService, ISender sender)
        {
            _commentService = commentService;
            _sender = _sender;
        }

        /* 
           Za svaki Request pravi se automatski nova instanca kontrolera (AddTransient fakticki), onda DI automatski, na osnovu AddTransient/Scoped/Singleton<IService,Service>() iz Program.cs, u ctor of controller doda zeljeni servis i kad se 
         response posalje u FE, GC unistava kontroler, ali zivot servisa zavisi da li je Singleton, Transient ili Scoped. 
        
         Pogledaj AccountController, Endpoint.txt, HttpContext.txt, Authentication middleware.txt, CancellationToken.txt, Dependency Injection.txt, DTO vs entity klase.txt
         Sve vezano za GlobalExceptionHandler i Result pattern i Services je objasnjeno u AccountConttroller.

         Koristim mapper extensions da napravim Comment Entity klasu from DTO zbog Repository i napravim DTO from Comment Entity kad saljem data to FE.

         Svaki endpoint uradicu na 2 nacina: CQRS i Service. Zato sto se mora izabrati jedan od njih samo, ne sme mesano. Oba tipa u istom endpoint moraju sadrzati isti Result/Exception pattern. 
        */

        // Service endpoints 

        [HttpGet]   // https://localhost:port/api/comment
        [Authorize] // Moram se login i uneti JWT u Authorize dugme u Swagger da bi mogo da pokrenem ovaj Endpoint
        public async Task<IActionResult> GetAll([FromQuery] CommentQueryObject query, CancellationToken cancellationToken)
        {   /* Mora [FromQuery], jer GET Axios Request u ReactTS ne moze imati Body, vec samo Header, pa ne moze [FromBody]. 
               Kroz Query Parameters u FE (posle ? in URL), moram proslediti vrednosti za svako polje iz CommentQueryObject (iako neka imaju default value) redosledom i imenom iz CommentQueryObject
               U ReactTS, zbog [Authorize], moram proslediti i JWT kroz Request Header u commentsGetAPI funkciji.
               .NET automatski napravi CommentQueryObject iz URL query params.
            */
            
            var commentDTOs = await _commentService.GetAllAsync(query, cancellationToken);

            return Ok(commentDTOs); 
           // Frontendu ce biti poslato commentDTOs lista u Response Body, a StatusCode=200 u Response Status Line.
        }

        [HttpGet("{id:int}")] // https://localhost:port/api/comment/{id}
        [Authorize]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        // Mora bas "id" ime kao u liniji iznad i moze [FromRoute] jer id obicno prosledim kroz URL, a ne kroz Request body (JSON) ili Query
        {
            var commentResponseDTO = await _commentService.GetByIdAsync(id, cancellationToken);

            return Ok(commentResponseDTO);
        }

        //[EnableRateLimiting("slow")]
        [HttpPost("{symbol:alpha}")] // https://localhost:port/api/comment/{symbol} 
        // Ne sme [HttpPost("{symbol:string}")] jer gresku daje, obzirom da za string mora ili [HttpPost("{symbol:alpha}")] ili [HttpPost("{symbol}")] 
        [Authorize] // I da nisam stavio [Authorize], zbog User.GetUserName() moralo bi da se JWT prosledi sa Frontend prilikom gadjanja ovog Endpoint, ali treba staviti [Authorize] jer osigurava da userName!=null, jer forsira Frontend da salje JWT.
        public async Task<IActionResult> Create([FromRoute] string symbol, [FromBody] CreateCommentRequestDTO dto, CancellationToken cancellationToken)
        // U FE commentPostAPI funkciji, symbol kroz URL prosledim, a kroz Body saljem polja imenom i redosledom kao u CreateCommentRequestDTO (nisam stavio [FromBody] jer se to podrazumeva za complex type in POST request)
        {
            // ModelState pokrene validation za CreateCommentRequestDTO tj za zeljena CreateCommentRequestDTO polja proverava na osnovu onih annotation iznad polja koje stoje. ModelState se koristi za Writing to DB.
            if (!ModelState.IsValid)             
                return BadRequest(ModelState);
                // Frontendu ce biti poslato StatusCode=400 u Response Status, a ModelState ce biti poslat u Response Body sa CreateCommentRequestDTO poljima u "errors" delu of Response

            var userName = User.GetUserName(); // Pogledaj HttpContext.txt

            // Write to DB endpoint, pa mapiram CreateCommentRequestDTO u CommandModel - pogledaj Services.txt + DTO vs entity klase.txt 
            var command = new CreateCommentCommandModel
            {
                Title = dto.Title,
                Content = dto.Content
            };

            var resultPattern = await _commentService.CreateAsync(userName, symbol, command, cancellationToken);
            if (resultPattern.IsFailure)
                return BadRequest(resultPattern.Error);

            var commentDTOResponse = resultPattern.Value; 
             
            return CreatedAtAction(nameof(GetById), new { id = commentDTOResponse.Id }, commentDTOResponse); // Id property of Comment ima Value polje jer strongly-id type
            /* Prva 2 su route of GetById endpoint i endpoint's argument, jer GetById endpoint zahteva id argument.
               Frontendu ce biti poslato comment.ToCommentDTO() (tj CommentDTO objekat) u Response Body, StatusCode=201 u Response Status Line, a https://localhost:port/api/comment/{id} u Response Header.
            */
        }

        [HttpDelete("{id:int}")] // https://localhost:port/api/comment/{id}
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {   
            // Iako je write to DB, nema mapiranje, jer samo id je argument 

            // Zbog authorization u CQRS kako user moze samo svoj komentar brisati 
            var userName = User.GetUserName(); // Pogledaj HttpContext.txt

            var resultPattern = await _commentService.DeleteAsync(id, userName, cancellationToken);
            if (resultPattern.IsFailure)
                return BadRequest(resultPattern.Error);

            var commentDTOResponse = resultPattern.Value;

            return Ok(commentDTOResponse);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCommentRequestDTO dto, CancellationToken cancellationToken)
        // U FE, id uvek saljem in URL, dok complex type kroz Body kao sto znam
        {
            // ModelState pokrene validation za UpdateCommentRequestDTO tj za zeljena CreateCommentRequestDTO polja proverava na osnovu njihovih annotations. ModelState se koristi za Writing to DB.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a ModelState objekat  biti poslat u Response Body sa UpdateCommentRequestDTO poljima u "errors" delu of Response

            // Write to DB endpoint, pa mapiram UpdateCommentRequestDTO u CommandModel - pogledaj Services.txt + DTO vs entity klase.txt 
            var commandModel = new UpdateCommentCommandModel
            {
                Title = dto.Title,
                Content = dto.Content,
            };

            var resultPattern = await _commentService.UpdateAsync(id, commandModel, cancellationToken);
            if (resultPattern.IsFailure)
                return NotFound("Comment not found");
                // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a "Comment not found" u Response Body.

            var commentDTOResponse = resultPattern.Value;

            return Ok(commentDTOResponse);
            // Frontendu ce biti poslato comment.ToCommentDTO() (tj CommentDTO objekat) u Response Body, a StatusCode=200 u Response Status Line.
        }

        // CQRS endpoints

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllCqrs([FromQuery] CommentQueryObject commentQueryObject, CancellationToken cancellationToken)
        {
            // Nemam CommetGetAllRequest objekat, jer zelim da GetAllCqrs i GetAll endpoints budu istog zaglavlja + da ista GetAll Repository metoda opsluzi Service i CQRS! 
            var result = await _sender.Send(new CommentGetAllQuery(commentQueryObject), cancellationToken); // Aktivira samo Handler, jer nema validacija za Query
            var response = result.Adapt<CommentGetAllResponse>(); // Mapirace dobro i polje tipa CommentQueryObject unutar Result i Request objekata.

            return Ok(response.commentResponseDTOs);
        }

        [HttpGet("{id:int}")] // Ne moze ista route kao za GetById, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize]
        public async Task<IActionResult> GetByIdCqrs([FromRoute] int id, CancellationToken cancellationToken)
        {
            // Ne mapiram Request to Query, jer Request object nema potrebe zbog jednog argumenta primitivnog tipa da postoji, vec odma Query objekat pravim i saljem u MediatR pipeline
            var result = await _sender.Send(new CommentGetByIdQuery(id), cancellationToken);
            var response = result.Adapt<CommentGetByIdResponse>(); // Mapster auto mapira jer su polja istog imena i tipa u obe klase. Mogo sam i bez Response, ali cisto da vidite kako izgleda mapiranje sa Result pattern.

            return Ok(response.commentDTOResponse);
        }

        [HttpPost("{symbol:alpha}")] // Ne moze ista route kao za Create, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS 
        [Authorize]
        public async Task<IActionResult> CreateCqrs([FromRoute] string symbol, [FromBody] CreateCommentRequestDTO request, CancellationToken cancellationToken)
        {
            // Write to DB endpoint, pa mapiram CreateCommentRequestDTO u CommandModel da razdvojim slojeve lepo.
            var commandModel = new CreateCommentCommandModel
            {
                Title = request.Title,
                Content = request.Content
            };

            var userName = User.GetUserName(); // Pogledaj HttpContext.txt
            
            // Necu pravim novi CommentCreateRequest object da sadrzi symbol + polja iz CreateCommentRequestDTO, jer ocu da potpis bude isti kao u Create endpoint
            var command = new CommentCreateCommand(userName, symbol, commandModel);

            var resultPattern = await _sender.Send(command, cancellationToken); // Aktivira se prvo Validacija, pa Handler 
            if (resultPattern.IsFailure)
                return BadRequest(new { message = resultPattern.Error });

            var result = resultPattern.Value!;
            // Ne treba mi CommentCreateResponse 

            return CreatedAtAction(nameof(GetById), new { id = result.CommentDTOResponse.Id }, result.CommentDTOResponse);
        }

        [HttpDelete("{id:int}")] // Ne moze ista route kao za Create, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS 
        [Authorize]
        public async Task<IActionResult> DeleteCqrs([FromRoute] int id, CancellationToken cancellationToken)
        {
            // Iako je write to DB, nema mapiranje, jer samo id je argument 

            // Zbog authorization da user moze samo svoj komentar brisati 
            var userName = User.GetUserName(); 

            // Ne mapiram Request to Query, jer Request nema potrebe da postoji zbog jednog argumenta primitivnog tipa da postoji jer endpoint prima id direktno, vec odma Query objekat pravim i saljem u MediatR pipeline
            var resultPattern = await _sender.Send(new CommentDeleteCommand(id, userName)); // Prvo pokrene validaciju, pa Handler 
            if (resultPattern.IsFailure)
                return NotFound(new { message = resultPattern.Error });

            var result = resultPattern.Value; // CommentDeleteResult ima ista polja kao CommentDTOResponse 
            // Ne treba mi CommentDeleteResponse 

            return Ok(result);
        }

        [HttpPut("{id:int}")] // Ne moze ista route kao za Update, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize]
        public async Task<IActionResult> UpdateCqrs([FromRoute] int id, [FromBody] UpdateCommentRequestDTO request, CancellationToken cancellationToken)
        {
            // Write to DB endpoint, pa mapiram UpdateCommentRequestDTO u CommandModel da razdvojim slojeve lepo.
            var commandModel = new UpdateCommentCommandModel
            {
                Title = request.Title,
                Content = request.Content
            };

            // Necu pravim novi CommentUpdateRequest object da sadrzi symbol + polja iz CreateCommentRequestDTO, jer ocu da potpis bude isti kao u Create endpoint
            var command = new CommentUpdateCommand(id, commandModel);

            var resultPattern = await _sender.Send(command, cancellationToken);
            if (resultPattern.IsFailure)
                return NotFound(resultPattern.Error);

            var result = resultPattern.Value.CommentDTOResponse; // CommentDTOResponse
            // Ne treba mi CommentDeleteResponse

            return Ok(result);
        }
    }
}
