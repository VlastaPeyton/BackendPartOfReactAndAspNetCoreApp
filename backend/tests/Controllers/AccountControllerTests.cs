using Api.Controllers;
using Api.DTOs.Account;
using Api.Interfaces;
using Api.Models;
using Api.Services;
using DotNetEnv;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace tests.Controllers
{
    public class AccountControllerTests
    {
        // Moramo kreirati originalni controller koji testiram
        private readonly AccountController _controller; 

        // Testiranje controllera zahteva da dependencies uvezemo kao u originalnom controlleru, jer cu ih mockovati (Fake-ovati)
        private readonly UserManager<AppUser> _fakeUserManager;
        private readonly SignInManager<AppUser> _fakeSignInManager;
        private readonly ITokenService _fakeTokenService;
        private readonly IEmailService _fakeEmailService;
        private readonly ILogger<AccountController> _fakeLogger;
        private readonly IAccountService _fakeAccountService;

        // Ctor nema argumente za razliku od originalnog
        public AccountControllerTests()
        {
            _fakeUserManager = A.Fake<UserManager<AppUser>>();
            _fakeSignInManager = A.Fake<SignInManager<AppUser>>();
            //_fakeTokenService = A.Fake<ITokenService>(); moram drugacije da testiram jer sam EmailService injectovao kroz [FromServices] direkt u endpoint
            _fakeEmailService = A.Fake<IEmailService>();
            _fakeLogger = A.Fake<ILogger<AccountController>>();
            _fakeAccountService = A.Fake<AccountService>();

            _controller = new AccountController(_fakeAccountService);

        }
        // Async Endpoint u AccountController mora i ovde biti async, a tip mora biti Task i bez RegisterDTO argumenta jer cu ga kreirati u metodi
        [Fact] 
        public async Task Register_WithValidData_ShouldReturnOkWithNewUserDTO() // Register Endpoint kada vrati OK(newUserDTO)
        {
            // Arrange
            var registerDto = new RegisterDTO
            {   // Obrati paznju na annotations u RegisterDTO jer zelim da ova polja imaju korektne vrednosti
                EmailAddress = "test@example.com",
                UserName = "testuser",
                Password = "Password123!"
            };

            var appUser = new AppUser { Email = registerDto.EmailAddress, UserName = registerDto.UserName };

            // Configure sve metode iz Endpoint koje ce se odraditi u slucaju da ModelState is valid tj da RegisterDTO ima dobre argumente i da sve uspe, a u Assert delu cu ih pozvati ako zelim ali ne moram
            A.CallTo(() => _fakeUserManager.CreateAsync(A<AppUser>._, registerDto.Password)).Returns(Task.FromResult(IdentityResult.Success)); 
            // A<AppUser>._ = Any AppUser. CreateAsync returns Task<IdentityResult> i zato ovakav Returns mora. Task.FromResut is synchronous way to create already complete async task jer za testing ne zelim da cekam kao kad u realnosti on upisuje u bazu.
            A.CallTo(() => _fakeUserManager.AddToRoleAsync(A<AppUser>._, "User")).Returns(Task.FromResult(IdentityResult.Success));            
            // Isto objasnjenje i ovde
            A.CallTo(() => _fakeTokenService.CreateAccessToken(A<AppUser>._)).Returns("fake-jwt-token");   
            // CreateToken returns string i zato moze ovakav Returns

            // Act - pozivam Endpoint koji testiram sa ovim fake registerDTO argumentom
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>(); // Jer Register Endpoint vraca Ok(newUserDTO) ako je uspesno
            var okResult = result as OkObjectResult;
            var newUserDto = okResult.Value as NewUserDTO;

            newUserDto.Should().NotBeNull();
            newUserDto.EmailAddress.Should().Be(registerDto.EmailAddress);
            newUserDto.UserName.Should().Be(registerDto.UserName);
            newUserDto.Token.Should().Be("fake-jwt-token");
        }

        // Isto objasnjenje kao iznad
        [Fact]
        public async Task Register_WithInvalidModelState_ShouldReturnBadRequest() // Register Endpoint when returns BadRequest(ModelState) kad ModelState fails
        {
            // Arrange
            var registerDto = new RegisterDTO(); // Empty RegisterDTO to trigger validation errors
            // Nema Configure (a ni poziva u Assert) nijedne metode koje Endpoint koristi, jer RegisterDTO argument nije dobar pa i ne stigne da pozove CreateAsync, AddToRoleAsync i CreateToken
            
            // Moram rucno dodati za svako polje of registerDTO, jer validation does not run automatically in unit test
            _controller.ModelState.AddModelError("EmailAddress", "The Email field is required.");
            _controller.ModelState.AddModelError("Password", "The Password field is required.");
            _controller.ModelState.AddModelError("UserName", "The UserName field is required.");
            // Act 
            var result = await _controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>(); // Zbog 400 status code mora ovo, a ne ObjectResult
            var badRequestResult = result as BadRequestObjectResult;
        }
        
        [Fact]
        public async Task Register_WhenUserCreationFails_ShouldReturnStatusCode500() // Register Endpoint when returns StatusCode(500) kad CreateAsync fails
        {
            // Arrange
            var registerDTO = new RegisterDTO
            {   // Obrati paznju na annotations u RegisterDTO jer zelim da ova polja imaju korektne vrednosti
                EmailAddress = "test@example.com",
                UserName = "testuser",
                Password = "Password123!"
            };

            var identityErrors = new[] { new IdentityError { Code = "DuplicateUserName", Description = "Username already exists" } };
            var failedResult = IdentityResult.Failed(identityErrors);
            
            // Configure metode nakon ModelState valid koje se pozivaju tj sve metode pre CreateAsync i nju samu jer u njoj fails
            A.CallTo(() => _fakeUserManager.CreateAsync(A<AppUser>._, registerDTO.Password)).Returns(Task.FromResult(failedResult));

            // Act
            var result = await _controller.Register(registerDTO);

            // Assert
            result.Should().BeOfType<ObjectResult>(); // Za razliku od 400 status code, 500 nema konkretan tip, vec ObjectResult mora
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(identityErrors);
        }

        [Fact]
        public async Task Register_WhenAddToRoleFails_ShouldReturnStatusCode500() // Register Endpoint when returns StatusCode(500) kad AddToRoleAsync fails
        {
            // Arrange
            var registerDTO = new RegisterDTO
            {
                UserName = "testuser",
                EmailAddress = "test@example.com",
                Password = "Password123!"
            };

            var roleErrors = new[] { new IdentityError { Code = "RoleNotFound", Description = "Role not found" } };
            var roleFailedResult = IdentityResult.Failed(roleErrors);

            // Configure sve metode koje se pokrenu pre AddToRoleAsync i nju samu jer u njoj fails
            A.CallTo(() => _fakeUserManager.CreateAsync(A<AppUser>._, registerDTO.Password)).Returns(Task.FromResult(IdentityResult.Success));
            A.CallTo(() => _fakeUserManager.AddToRoleAsync(A<AppUser>._, "User")).Returns(Task.FromResult(roleFailedResult));

            // Act
            var result = await _controller.Register(registerDTO);

            // Assert
            result.Should().BeOfType<ObjectResult>(); 
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(roleErrors);
        }
    }
}
