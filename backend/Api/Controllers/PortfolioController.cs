using Api.CQRS_and_behaviours.Portfolio.AddPortfolio;
using Api.CQRS_and_behaviours.Portfolio.Delete;
using Api.CQRS_and_behaviours.Portfolio.GetUserPortfolios;
using Api.Extensions;
using Api.Interfaces;
using Api.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/portfolio")]
    [ApiController]
    public class PortfolioController : ControllerBase
    {    
        private readonly IPortfolioService _portfolioService;
        private readonly ISender _sender;

        public PortfolioController(IPortfolioService portfolioService, ISender sender)
        {
            _portfolioService = portfolioService;
            _sender = sender;
        }
        // Objasnjeno u Account i Comment controller

        // Service endpoints 

        [HttpGet]
        [Authorize] // U Swagger Authorize dugme moram uneti JWT from Login kako bih mogo da pokrenem ovaj Endpoint
        public async Task<IActionResult> GetUserPortfolios(CancellationToken cancellationToken)
        {   // I da nisam stavio [Authorize], zbog User.GetUserName() moralo bi da se JWT prosledi sa Frontend prilikom gadjanja ovog Endpoint, ali treba staviti [Authorize] jer osigurava da userName!=null, jer forsira Frontend da salje JWT.
            var userName = User.GetUserName();

            var stockDtoResponses = await _portfolioService.GetUserPortfoliosAsync(userName, cancellationToken); 
            
            return Ok(stockDtoResponses);
        }

        //[EnableRateLimiting("fast")] // Jer cesto koristim ovu metodu
        [HttpPost]
        [Authorize] // I da nisam stavio [Authorize], zbog User.GetUserName() moralo bi da se JWT prosledi sa Frontend prilikom gadjanja ovog Endpoint, ali treba staviti [Authorize] jer osigurava da userName!=null, jer forsira Frontend da salje JWT.
        public async Task<IActionResult> AddPortfolio([FromQuery] string symbol, CancellationToken cancellationToken)  // 1 Portfolio = 1 Stock, a glavna stvar Stock-a je Symbol polje
        {   // Da nema [FromQuery], obzirom da symbol je string, .NET bi prihvatamo i [FromRoute], [FromQuery] i [FromBody]. Zbog [FromQuery] u portfolioAddApi u FE moram poslati symbol nakon ? in URL
            var userName = User.GetUserName(); // User i GetUserName come from ControllerBase ClaimsPrincipal tj user info from request i zahteva od FE da posalje JWT jer su u njemu claims
            
            var resultPattern = await _portfolioService.AddPortfolioAsync(symbol, userName, cancellationToken);
            if (resultPattern.IsFailure)
                return BadRequest(new {message = resultPattern.Error});

            return Created();
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeletePortfolio([FromQuery] string symbol, CancellationToken cancellationToken) // 1 Portfolio = 1 Stock, a glavna stvar Stock-a je Symbol polje
        {   // Da nema [FromQuery], obzirom da symbol je string, .NET bi prihvatamo i [FromRoute], [FromQuery] i [FromBody]. Zbog [FromQuery] u portfolioDeleteApi u FE moram poslati symbol nakon ? in URL
            
            var userName = User.GetUserName(); 

            var resultPattern = await _portfolioService.DeletePortfolioAsync(symbol, userName, cancellationToken);
            if (resultPattern.IsFailure)
                return BadRequest(new { message = resultPattern.Error });

            return Ok();
        }

        // CQRS endpoints

        [HttpGet]  // Ne moze isti route kao GetUserPortfolios, jer nece moci u Postman testiranje, ali cisto da pokazem i CQRS zelim
        [Authorize]
        public async Task<IActionResult> GetUserPortfoliosCqrs(CancellationToken cancellationToken)
        {
            var userName = User.GetUserName();

            // Nema potrebe za Request object, jer nemam argument nijedan u endpoint
            var result = await _sender.Send(new PortfolioGUPQuery(userName), cancellationToken);

            return Ok(result.StockDTOResponses);
        }

        [HttpPost] // Ne moze isti route kao AddPortfolio, jer nece moci u Postman testiranje, ali cisto da pokazem i CQRS zelim
        [Authorize]
        public async Task<IActionResult> AddPortfolioCqrs([FromQuery] string symbol, CancellationToken cancellationToken)
        {
            var userName = User.GetUserName();

            // Nema potrebe za Request object, jer samo 1 argument prostog tipa u endpoint 
            var resultPattern = await _sender.Send(new PortfolioApCommand(symbol, userName), cancellationToken);
            if (resultPattern.IsFailure)
                return BadRequest(new { message = resultPattern.Error });

            var result = resultPattern.Value;
            return Ok(result.PortfolioDtoResponse);
        }

        [HttpDelete] // Ne moze isti route kao DeletePortfolio, jer nece moci u Postman testiranje, ali cisto da pokazem i CQRS zelim
        [Authorize]
        public async Task<IActionResult> DeletePortfolioCqrs([FromQuery] string symbol, CancellationToken cancellationToken)
        {
            var userName = User.GetUserName();

            var resultPattern = await _sender.Send(new PortfolioDeleteCommand(symbol, userName), cancellationToken);
            if (resultPattern.IsFailure)
                return BadRequest(new { message = resultPattern.Error });

            return Ok();
        }
    }
}
