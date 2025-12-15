using System.Threading.RateLimiting;
using Api.CQRS_and_Validation;
using Api.CQRS_and_Validation.Logging;
using Api.Data;
using Api.DTOs.AccountDTOs;
using Api.Extensions;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.MessageBroker;
using Api.Middlewares;
using Api.Models;
using Api.Repository;
using Api.Repository.BaseRepository;
using Api.Service;
using Api.Services;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

Env.Load(); // Loads ".env" from project root pre svega ostalog, jer mnogo toga (i van Program.cs) zavisi od Env.GetString(...)

// Registrovanje servisa u DI - pogledaj Dependency Injection.txt

builder.Services.AddControllers(); 
// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Add Authorize dugme za unos JWT u Swagger 
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {   
        { // Mora ovaj par zagrada, jer OpenApiSecurityRequirement nasledjuje recnik 
            new OpenApiSecurityScheme // Dictionary Key
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{} // Dictionary Value
        }
    });
});

// Add DbContext (AddScoped by default)
builder.Services.AddDbContext<ApplicationDBContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database"));
});
// Nakon ovoga, u Package Manager Console kucam "Add-Migration ime_migracije", pa "Update-Database" da se naprave sve tabele i FK-PK iz OnModelCreating 

// Add IdentityDbContext + definisem password kog oblika mora biti i skladistim ga u istu bazu sa Stocks i Comments, stoga mora AddEntityFrameworkStores
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{   // IdentityRole je AspNetRoles tabela. AddIdentity ce napraviti i povezati SVE Identity tabele iz IdentityDbContext, a ne samo ove 2.
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    options.User.RequireUniqueEmail = true;         // Da ne mogu dva usera da imaju isti Email prilikom njihovog dodavanja u AspNetUsers tabelu u Register endpoint

}).AddEntityFrameworkStores<ApplicationDBContext>() // AddEntityFrameworkStores tells Identity to use EF Core to store Identity data (users, roles, tokens, etc.) in the database using ApplicationDbContext.
  .AddDefaultTokenProviders();                      // Is needed for email confirmation in Register or password reset in ForgotPassword endpoint

// Nakon ova 2 registrovanja iznad, u Package Manager Console kucam "Add-Migration Identity", pa "Update-Database", da sa naprave sve Identity tabele (objasnjene u ApplicationDbContext.cs) 

// JWT Authentication when http request hits [Authorize] endpoint - pogledaj Authentication middleware.txt
builder.Services.AddAuthentication(options =>
{   
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Authenticate user via JwtBearerHandler to read JWT on when accessing HttpContext.User
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;    // Specifies the scheme to use when an unauthenticated user hits [Authorize] endpoint and return 401
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;       // User is authenticated but forbidden on specific endpoint and return 403
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;             // Fallback if other shcemas arent specified
    
}).AddJwtBearer(options =>
{   // AddJwtBearer je registrovanje JWT handler - pogledati Authentication middleware.txt
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"], 
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"]!))
    };
}).AddGoogle("Google", options =>
{   // Google auth handler for google login/register - pogledaj GoogleLogin.txt 
    options.ClientId = Env.GetString("GOOGLE_AUTH_CLIENT_ID");
    options.ClientSecret = Env.GetString("GOOGLE_AUTH_CLIENT_SECRET");

    // Include email and profile in id_token
    options.Scope.Add("email"); 
    options.Scope.Add("profile"); 

    options.CallbackPath = "api/account/login/google/callback";
});

// Objasnjeno u JSON engine.txt 
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore; // Kada endpoint vraca odgovor clientu, automatski se pravi JSON i ignorise circular reference
});

// Add MassTransit (MessageBroker + Outbox pattern) za RabbitMQ Publisher - mora pre repository u kom se koristi
builder.Services.AddMassTransitRabbitMQAndOutboxInbox<ApplicationDBContext>(builder.Configuration);

// Svaki servis ima interface zbog SOLID + lako se testira sa xUnit, FakeItEasy/Moq, FluentAssertions itd.

// Add StockService i IStockService
builder.Services.AddScoped<IStockService, StockService>(); 
// Add StockRepository i IStockRepository
builder.Services.AddScoped<IStockRepository, StockRepository>();
// Add StockRepositoryBase i IStockRepositoryBase
builder.Services.AddScoped<IStockRepositoryBase, StockRepositoryBase>();
// Add CachedStockRepository via Scrutor - pogledaj Redis, Proxy & Decorator patterns.txt i pogledaj CachedStockRepository
builder.Services.Decorate<IStockRepositoryBase, CachedStockRepository>(); // dok nisam napravio BaseRepository, koristio sam IStockRepository 
// Add IDistributedCache za CachedStockRepository from StackExchangeRedis NuGet kako bih povezao Redis with IDistributedCache
builder.Services.AddStackExchangeRedisCache(config =>
{   
    config.Configuration = builder.Configuration.GetConnectionString("Redis");
});


// Add AccountService i IAccountService 
builder.Services.AddScoped<IAccountService, AccountService>(); 
// Add CommentService i ICommentService
builder.Services.AddScoped<ICommentService, CommentService>();
// Add CommentRepository i ICommentRepository
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
// Add CommentRepositoryBase i ICommentRepositoryBase 
builder.Services.AddScoped<ICommentRepositoryBase, CommentRepositoryBase>();
// Add TokenService i ITokenService
builder.Services.AddScoped<ITokenService, TokenService>();
// Add EmailService as IEmailSender after Env.Load()
builder.Services.AddScoped<IEmailService, EmailService>();
// Add IPortfolioRepository i PortfolioRepository 
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();

// Add HttpClient for FinancialModelingPrepService 
builder.Services.AddHttpClient<IFinacialModelingPrepService, FinancialModelingPrepService>() // Pogledaj IHttpClientFactory, HttpClient, Resilience.txt
                .AddStandardResilienceHandler(); // Dodaje defaultne retry, timeout, circuit breaker. Pogledaj IHttpClientFactory, HttpClient, Resilience.txt
//builder.Services.AddFMPHttpClientWithCustomResilience();  - ako zelim custom Resilience

// Add FluentValidation AbstractValidator (FluentValidation.DependencyInjectionExtensions NuGet) for MediatR pipeline
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly); // Finds CommandValidator klase koje imeplementiraju AbstractValidator , da ValidationBehaviour moze da ih nadje tokom runtime
// Add FluentValidation for AccountService LoginAsync method which is not MediatR pipeline
builder.Services.AddScoped<IValidator<LoginCommandModel>, LoginCommandModelValidator>();

// Add Mediator - pogledaj Mediator.txt
builder.Services.AddMediatR(config =>
{   // Registruje Handler klase jer registruje IRequestHandler koga implementira ICommand/IQueryHandler
    config.RegisterServicesFromAssemblies(typeof(Program).Assembly); // Nadje sve klase koje implementiraju MediatR interface
    // Dodam ValidationBehavior to MediatR pipeline. Bitan je redosled registracije of behaviours
    config.AddOpenBehavior(typeof(ValidationBehaviour<,>)); 
    // Dodam LoggingBehavior to MediatR pipeline koje se pokrece automatski iz ValidationBehaviour
    config.AddOpenBehavior(typeof(LoggingBehaviour<,>));   
});

// Add Rate Limiter 
builder.Services.AddRateLimiter(options =>
{   // Nema globalni default rate limiter => Endpoints koji nemaju [EnableRateLimiting("slow/fast")] nece imati nikakav 
    options.AddFixedWindowLimiter("slow", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 5; // 2 Request per 1min . During 1min, if other requests comes after first 5, they will be put in queue, of size QueueuLimit, to wait in QueueProcessingOrder order. Others will be rejected.
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 3;  
    });

    options.AddFixedWindowLimiter("fast", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 7;
    });
});

// Custom middleware koji nasledi IMiddleware mora biti registrovan AddTransient u DI
builder.Services.AddTransient<IMiddleware, GlobalExceptionHandlingMiddleware>();

// Options pattern for MessageBrokerSettings
// Registruj u DI IOptions<MessageBrokerSettings> i proveri da l su polja dobro upisana u appsettings.json 
builder.Services.AddOptions<MessageBrokerSettings>()
                .Bind(builder.Configuration.GetSection("MessageBroker"))
                .ValidateDataAnnotations()   
                .ValidateOnStart();
// Zbog ovoga, u MassTransitExtensions.cs moze GetRequiredService<MessageBrokerSettings>, a ne mora GetRequiredService<IOptions<MessageBrokerSettings>>().Value
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<MessageBrokerSettings>>().Value);

// Localization - pogledaj Localization.txt
builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

var app = builder.Build();

// Dodavanje middleware u pipeline bitan redosled jer request ide nizvodno ovde, a response uzvodno - pogledaj Middleware.txt 

// Enable Swagger middleware both for Dev and Prod env
app.UseSwagger();
app.UseSwaggerUI(); // Default UI at /swagger

app.UseHttpsRedirection(); // Forces HTTP to become HTTPS ako FE posalje Request na http://localhsto:5110 umesto https://localhost:7045 jer je sigurnije

// Add custom middleware on top of pipeline da bi uhvatio sve greske iz middleware koji su ispod 
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// CORS (Browser sequrity feature that restrics pages from making request to different domain that one that served the page).  CORS middleware registrovan mora pre Authentication middleware.
app.UseCors(x => x.WithOrigins("https://localhost:3000") // Samo moj Fronted(https ili http) (od svih sajtova na netu) moze slati Request to my Api (Backend ovaj)
                 .AllowAnyMethod() // Allows every Request method (GET, POST, PUT, DELETE...)
                 .AllowAnyHeader() // Allows custom headers, Authorization headers za JWT...
                 .AllowCredentials()); // Da bi FE mogo, kad mu je Access Token blizu isteka , rekao Browseru posalji zahtev za novi Access Token via Cookie(Refresh Token)

/*
app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always, // Zbog ovoga, mora FE in HTTPS
    MinimumSameSitePolicy = SameSiteMode.None
}); // Jer RefreshToken Endpoint ovo zahteva. Ovo mora before UseAuthentication
Ako ovo imam, onda u AccountController ne pisem Append i ne navodim one parametre.*/

// Enable Authentication - pogledaj Authentication middleware.txt
app.UseAuthentication(); 
// Enable Authorization after Authentication - pogledaj Authorization middleware.txt 
app.UseAuthorization();  
// Enable using of Rate Limiter middleware on desired endpoints 
app.UseRateLimiter();

// Ubaci Controlers (+ Routing i Endpoint) middleware u pipeline. Za svaki [Http...("route..")] iznad Endpoint ASP.NET Core znace kako da ga mapira sa incoming request.
app.MapControllers();

await app.SeedAdminAsync(); // Admin user se dodaje iz BE uvek na pocetku svega

app.Run(); // Middleware dodat u pipeline pomocu app.Run() nema next() i on mora biti poslednji in pipeline
