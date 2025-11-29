
using Api.CQRS_and_behaviours.Stock.Create;
using Api.CQRS_and_behaviours.Stock.Delete;
using Api.CQRS_and_behaviours.Stock.GetAll;
using Api.CQRS_and_behaviours.Stock.GetById;
using Api.CQRS_and_behaviours.Stock.Update;
using Api.CQRS_and_Validation.Comment;
using Api.DTOs.Stock;
using Api.DTOs.StockDTOs;
using Api.Helpers;
using Api.Interfaces;
using Api.Mapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{   
    [Route("api/stock")] // https://localhost:port/api/stock
    [ApiController]
    public class StockController : ControllerBase
    {   
        private readonly IStockService _stockService;
        private readonly ISender _sender;
        public StockController(IStockService stockService, ISender sender)
        {
            _stockService = stockService;
            _sender = sender;
        }

        /* 
           Objasnjeno u Account/CommentController.
           U ReactTS zbog [Authorize] moram proslediti JWT u Request Header. 
           CachedStockRepository sa Redis objasnjen u Redis, Proxy & Decorator patterns.txt => _stockRepository se odnosi na CachedStockRepository
        */

        // Service endpoints

        [HttpGet]   // https://localhost:port/api/stock/
        [Authorize] // Mora u Swagger Authorize dugme da unesemo JWT token koji sam dobio prilikom login/register da bih mogo da pokrenem ovaj Endpoint + FE mora poslati JWT u Authorization header of request
        public async Task<IActionResult> GetAll([FromQuery] StockQueryObject query, CancellationToken cancellationToken) 
        {   // Mora [FromQuery], jer GET Axios Request u ReactTS ne moze da ima body, vec samo Header, pa ne moze [FromBody]. Kroz Query Parameters u FE (posle ? in URL) moram proslediti vrednosti za svako polje iz QueryObject (ako ne prosledim, bice default vrednosti) redosledom i imenom iz QueryObject
            
            var stockDTOs = await _stockService.GetAllAsync(query, cancellationToken); 
            
            return Ok(stockDTOs); 
            // Frontendu ce biti poslato StatusCode=200 u Response Status Line, a stockDTOs u Response Body.
        }

        [HttpGet("{id:int}")] // https://localhost:port/api/stock/{id}
        [Authorize]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken) 
        // Mora bas "id" kao u liniji iznad i moze [FromRoute] jer id obicno prosledim kroz URL, a ne kroz Request body (JSON)
        {
            var result = await _stockService.GetByIdAsync(id, cancellationToken);
            if (result.IsFailure)
                return NotFound(new { message = result.Error });
                // Frontendu ce biti poslato StatusCode=404 u Response Status Line, a Response Body je JSON "message":"Nije pronadjen Stock by zeljeni id"

            var stockDTOResponse = result.Value;

            return Ok(stockDTOResponse); 
            // Frontendu ce biti poslato StatusCode=200 u Response Status Line, a stock.ToStockDTO (tj StockDTO objekat) u Response Body.
        }

        [HttpPost] // https://localshost:port/api/stock
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateStockRequestDTO dto, CancellationToken cancellationToken)
        // Mora [FromBody] jer ne prosledjujem argumente kroz URL (posto ih ima vise od 1 non primary tipa), vec kroz Postman body (JSON).
        // Mora CreateStockRequestDTO, jer Request gadja Endpoint, stoga u Request body kucam polja iz CreateStockRequestDTO imenom i redosledom kao u CreateStockRequestDTO
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Nakon validiranja dto, mapiram ga u CreateStockCommandModel - pogledaj CommentController
            var command = new CreateStockCommandModel
            {
                Symbol = dto.Symbol,
                CompanyName = dto.CompanyName,
                Purchase = dto.Purchase,
                Dividend = dto.Dividend,
                Industry = dto.Industry,
            };

            var stockResponseDto = await _stockService.CreateAsync(command, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = stockResponseDto.Id }, stockResponseDto);
            /* Iz nekog razloga id ne prati redosled, ali radi kako treba. 
               Prva 2 su route i route argument, jer GetById zahteva id argument.
               Frontendu ce biti poslato stockResponseDto u Response Body, StatusCode=201 u Response Status Line, a https://localhost:port/api/stock/{id} u Response Header.
            */
        }

        // Update entire Stock Endpoint i zato PUT, a ne PATCH (jer PATCH je update samo za zeljena polja)
        [HttpPut("{id:int}")] // https://localhost:port/api/stock/{id}
        [Authorize]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateStockRequestDTO dto, CancellationToken cancellationToken)
        // Id kroz URL obicno saljem iz FE i zato [FromRoute], dok objekte (JSON) moram kroz Body (ili kroz Query Parameters ako je Axios.GET u pitanju jer one podrzava Body).
        {
            // ModelState pokrene validation za UpdateStockRequestDTO tj za zeljena UpdateStockRequestDTO polja proverava na osnovu onih annotation iznad polja koje stoje.ModelState se koristi za writing to DB.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a ModelState objekat sa poljima EmailAddress, UserName i Password i svakom polju pisace koja je greska u njemu u Response Body u "errors" delu.

            var command = new UpdateStockCommandModel
            {
                Symbol = dto.Symbol, 
                CompanyName = dto.CompanyName, 
                Dividend = dto.Dividend, 
                Industry = dto.Industry, 
                MarketCap = dto.MarketCap, 
                Purchase = dto.Purchase,  
            };

            var resultPattern = await _stockService.UpdateAsync(id, command, cancellationToken);
            if (resultPattern.IsFailure)
                return NotFound(new { message = resultPattern.Error });

            var stockDtoResponse = resultPattern.Value;
            return Ok(stockDtoResponse);
        }

        [HttpDelete("{id:int}")] // https://localhost:port/api/stock/{id}
        [Authorize]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        // Id kroz URL se obicno salje iz FE i zato [FromRoute]
        {
            // Iako je write to DB, nema mapiranje, jer samo id je argument 

            var stockDtoResponse = await _stockService.DeleteAsync(id, cancellationToken);

            return NoContent();
            // Frontendu ce biti poslato StatusCode=204 u Response Status Line, a Response Body prazan.
        }

        // CQRS endpoints

        [HttpGet]   // Ne moze ista route kao za Update, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize] 
        public async Task<IActionResult> GetAllCqrs([FromQuery] StockQueryObject query, CancellationToken cancellationToken)
        {
            // Nemam StocktGetAllRequest objekat, jer zelim da GetAllCqrs i GetAll endpoints budu istog zaglavlja + da ista GetAll Repository metoda opsluzi Service i CQRS! 
            var result = await _sender.Send(new StockGetAllQuery(query), cancellationToken);
            // Ne treba StockGetAllResponse objekat 

            return Ok(result.StockDTOResponses);
        }

        [HttpGet("{id:int}")] // Ne moze ista route kao za GetAll, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize]
        public async Task<IActionResult> GetByIdCqrs([FromRoute] int id, CancellationToken cancellationToken)
        {
            // Ne mapiram Request to Query, jer Request object nema potrebe zbog jednog argumenta primitivnog tipa da postoji, vec odma Query objekat pravim i saljem u MediatR pipeline
            var resultPattern = await _sender.Send(new StockGetByIdQuery(id), cancellationToken);
            if (resultPattern.IsFailure)
                return NotFound(new { message = resultPattern.Error });

            var result = resultPattern.Value!;

            return Ok(result.StockDTOResponse);
        }

        [HttpPost]    // Ne moze ista route kao za GetById, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize]
        public async Task<IActionResult> CreateCqrs([FromBody] CreateStockRequestDTO dto, CancellationToken cancellationToken)
        {
            // Write to DB endpoint, pa mapiram CreateStockRequestDTO u CommandModel da razdvojim slojeve lepo.
            var commandModel = new CreateStockCommandModel
            {
                Symbol = dto.Symbol,
                CompanyName = dto.CompanyName,
                Purchase = dto.Purchase,
                Dividend = dto.Dividend,
                Industry = dto.Industry,
                MarketCap = dto.MarketCap,
            };

            var command = new StockCreateCommand(commandModel);

            var result = await _sender.Send(command);

            var stockDtoResponse = result.StockDTOResponse;

            return CreatedAtAction(nameof(GetById), new { id = stockDtoResponse.Id }, stockDtoResponse);
        }

        [HttpPut("{id:int}")] // Ne moze ista route kao za Create, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize]
        public async Task<IActionResult> UpdateCqrs([FromRoute] int id, [FromBody] UpdateStockRequestDTO dto, CancellationToken cancellationToken)
        {
            // Write to DB endpoint, pa mapiram UpdateStockRequestDTO u CommandModel da razdvojim slojeve lepo.
            var commandModel = new UpdateStockCommandModel
            {
                Symbol = dto.Symbol,
                CompanyName = dto.CompanyName,
                Purchase = dto.Purchase,
                Dividend = dto.Dividend,
                Industry = dto.Industry,
                MarketCap = dto.MarketCap,
            };

            var command = new StockUpdateCommand(id, commandModel);

            var resultPattern = await _sender.Send(command, cancellationToken);
            if (resultPattern.IsFailure)
                return NotFound(new { message = resultPattern.Error });

            var result = resultPattern.Value; 
            // Ne treba mi StockUpdateResponse object 

            return Ok(result.StockDTOResponse);
        }

        [HttpDelete("{id:int}")] // Ne moze ista route kao za Delete, jer nece moci se testirati u Postman, ali ovo je samo moja nadmoc da pokazem da znam i CQRS
        [Authorize]
        public async Task<IActionResult> DeleteCqrs([FromRoute] int id, CancellationToken cancellationToken)
        {
            // Iako je write to DB, nema mapiranje, jer samo id je argument 

            var result = await _sender.Send(new StockDeleteCommand(id), cancellationToken);
            // ne treba Resposne object

            return Ok(result.StockDTOResponse);
        }
    }
}
