using System.Net;
using Api.Exceptions;
using Api.Exceptions_i_Result_pattern.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middlewares
{
    /* 
     U pocetku, moj kod je radio ovako: Middleware- > Controller -> Service -> Repository.
     U Repository nisam explicitno bacio greske jer tu se podrazumevaju implicitne built-in + neam try-catch.
     U Service sam explicitno bacio greske + iz Repository implicitne su se propagirale u Service, ali neam try-catch u Service, pa se propagira u Controller.
     U Controller imao sam try-catch, pa ce sve greske iz Repository/Service da se propagiraju ovde i tu da se uhvate i da se klijentu posalje i odgovor i greska. 
     
     Bolja solucija je GlobalExceptionHandlingMiddleware koji ce da hvata greske, pa nema vise potrebe za try-catch nigde, stoga klijentu iz Controller saljem samo odgovor, 
    a gresku mu saljem iz GlobalExceptionHandlingMiddleware.
     
     Pogledaj Middleware.txt i Exception driven error handling.txt i Result pattern.txt
    */

    // Ovo moze i cesto je, ali se tesko testira, pa necu da koristim => U Program.cs: app.UseMiddleware<GlobalExceptionHandlingMiddlewareBezInterface>(); i middleware automatski registruje kao AddSingleton
    public class GlobalExceptionHandlingMiddlewareBezInterface
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddlewareBezInterface> _logger; // Singleton i zato moze kroz ctor da se ubaci 
        public GlobalExceptionHandlingMiddlewareBezInterface(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddlewareBezInterface> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {   
                _logger.LogError(ex, ex.Message);
                context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
            }
        }
    }

    // Ovo koristim, jer se lako testira zbog interface => u Program.cs moram prvo DI registrujem zbog interface, kao AddTransient, a onda da dodam middleware u pipeline regularno 
    public class GlobalExceptionHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        // .NET poziva InvokeAsync kada izvrsava ovaj middleware
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {   /* RequestDelegate je metoda koja prima HttpContext argument i predstavlja sledeci middleware u pipeline tj onaj koi je registrovan 
                 odma ispod GlobalExceptionHandlingMiddleware u Program.cs, a to je UseCors.
                   RequestDelegate nema definiciju explicitnu, vec .NET je definise prilikom pokretanja aplikacije. 

                   Ako zelim u request flow da ovaj middleware nesto uradi sa HttpContext.Request, onda pre "await next(context)" moram to napisati.
                */
                await next(context); // propusta request dalje u sledeci (nize registrovani middleware u Program.cs), pa sve do controllera, servisa ....

                // Ako zelim u response flow da ovaj middleware nesto uradi sa HttpContext.Response, onda posle "await next(context)" moram to napisati.
            }
            // Svaki custom exception je nasledio Exception i zato ce, kao i Exception, biti ovde uhvacen i kreiran odgovorajuci response 
            catch (Exception ex)
            {
                _logger.LogError(ex, "GlobalExceptionHandlerMiddleware uhvatio exception thrown from services or repository");

                // Ako neki middleware, registrovan (u Program.cs) ispod(kasnije u odnosu na request flow) GlobalExceptionHandlingMiddleware, krene slati odgovor pre nego ovde uhvati se greska - pogledaj Middleware.txt
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started in other middleware, cannot modify response.");
                    throw;
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {   
                    // AccountController:

                        // Register endpoint 
                    UserCreatedException or RoleAssignmentException => StatusCodes.Status500InternalServerError,

                        // Login endpoint 
                    //WrongPasswordException or WrongUsernameException => StatusCodes.Status401Unauthorized, - postalo Result pattern jer nije neocekivana greska systema, vec biznis logika

                        // ForgotPassword endpoint 
                    ForgotPasswordException => StatusCodes.Status200OK,

                        // ResetPassword endpoint 
                    ResetPasswordException => StatusCodes.Status200OK,

                        // RefreshToken endpoint  
                    RefreshTokenException => StatusCodes.Status401Unauthorized,
                        
                        // GoogleCallback endpoint
                    GoogleLoginException => StatusCodes.Status400BadRequest,

                    // CommentController:
                        // nema nistaa

                    // StockController:

                        // Update endpoint
                    StockNotFoundException => StatusCodes.Status404NotFound,

                    // PortfolioController:

                        // GetUserPortfolios endpoint 
                    UserNotFoundException => StatusCodes.Status404NotFound,

                    // ValidationBehaviour in MediatR pipeline 
                    ValidationException => StatusCodes.Status406NotAcceptable,

                    // Svaki endpoint, u bilo kom controlleru, je slao klijentu StatusCode 500 ako se desio implicitni error u service/repository/cqrs
                    _ => StatusCodes.Status500InternalServerError
                };

                // Pogledaj ProblemDetails.txt
                ProblemDetails problemDetails = new ProblemDetails 
                { 
                    Status = context.Response.StatusCode,
                    Title = ex switch
                    {   
                        // Account: 

                        UserCreatedException or RoleAssignmentException => "Implicit internal server error u service/repository",
                        ForgotPasswordException => "Saljem 200OK da zavaram trag i da si zamenio i da nisi password",
                        ResetPasswordException => "Saljem 200OK da zavaram trag i da si resetovao i da nisi password",
                        RefreshTokenException => "Unauthorized",
                        GoogleLoginException => "Greska prilikom google login",

                        // Comment:
                            // nema nistaa 

                        // Stock:
                        StockNotFoundException => "Stock not found",

                        // Portfolio: 
                        UserNotFoundException => "User not found ",

                        // ValidationBehaviour MediatR pipeline
                        ValidationException => "Validation problem",

                        _ => "Implicit internal server error u service/repository"
                    },
                    Detail = ex.Message,
                    Instance = context.Request.Path // Koji endpoint je izazvao gresku
                };
                
                await context.Response.WriteAsJsonAsync(problemDetails); // response.HasStarted = true
            }
        }
    }
}
