using System.Security.Claims;
using Api.Controllers;
using Api.DTOs.Account;
using Api.DTOs.AccountDTOs;
using Api.Exceptions_i_Result_pattern;
using Api.Interfaces;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace tests.UnitTests.ControllerTests
{   
    // Fake sve DI koje AccountController koristi u svom ctor
    public class AccountControllerTest
    {
        private static AccountController CreateController(IAccountService service, DefaultHttpContext? httpContext = null)
        {   // service ce biti fake 

            httpContext ??= new DefaultHttpContext();

            var controller = new AccountController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            return controller;
        }

        // Register endpoint bad situation
        [Fact]
        public async Task Register_returns_400_when_modelstate_invalid_and_does_not_call_service()
        {
            var service = A.Fake<IAccountService>();

            var sut = CreateController(service);

            sut.ModelState.AddModelError("UserName", "Required");

            var dto = new RegisterDTO
            {
                UserName = "",
                EmailAddress = "test@test.com",
                Password = "P@ssw0rd!"
            };

            var result = await sut.Register(dto);

            result.Should().BeOfType<BadRequestObjectResult>();

            A.CallTo(() => service.RegisterAsync(A<RegisterCommandModel>._))
                                  .MustNotHaveHappened();
        }

        // Register endpoint good situation
        [Fact]
        public async Task Register_sets_refresh_cookie_and_returns_ok_with_refreshToken_null()
        {
            var service = A.Fake<IAccountService>();
            var http = new DefaultHttpContext();
            var sut = CreateController(service, http);

            var dto = new RegisterDTO
            {
                UserName = "mike",
                EmailAddress = "mike@test.com",
                Password = "P@ssw0rd!"
            };

            const string refresh = "REFRESH_TOKEN_123";

            A.CallTo(() => service.RegisterAsync(
                    A<RegisterCommandModel>.That.Matches(c =>
                        c.UserName == dto.UserName &&
                        c.EmailAddress == dto.EmailAddress &&
                        c.Password == dto.Password)))
                .Returns(new NewUserDTO
                {
                    UserName = dto.UserName,
                    EmailAddress = dto.EmailAddress,
                    Token = "ACCESS",
                    RefreshToken = refresh
                });

            var result = await sut.Register(dto);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeOfType<NewUserDTO>().Subject;

            body.RefreshToken.Should().BeNull("controller must not return refresh token in response body");

            var setCookie = http.Response.Headers["Set-Cookie"].ToString();
            setCookie.Should().Contain($"refreshToken={refresh}");
            setCookie.Should().ContainEquivalentOf("httponly");
            setCookie.Should().ContainEquivalentOf("secure");
            setCookie.Should().ContainEquivalentOf("samesite=none");
            setCookie.Should().ContainEquivalentOf("path=/");

            A.CallTo(() => service.RegisterAsync(A<RegisterCommandModel>._))
                .MustHaveHappenedOnceExactly();
        }

        // Login endpoint bad situation
        [Fact]
        public async Task Login_returns_401_with_message_when_result_is_failure()
        {
            var service = A.Fake<IAccountService>();
            var sut = CreateController(service);

            var dto = new LoginDTO { UserName = "mike", Password = "bad" };

            A.CallTo(() => service.LoginAsync(A<LoginCommandModel>._))
                .Returns(Result<NewUserDTO>.Fail("Invalid credentials"));

            var result = await sut.Login(dto);

            var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorized.Value.Should().BeEquivalentTo(new { message = "Invalid credentials" });

            A.CallTo(() => service.LoginAsync(A<LoginCommandModel>._))
                .MustHaveHappenedOnceExactly();
        }

        // Login endpoint good situation
        [Fact]
        public async Task Login_sets_refresh_cookie_and_returns_ok_with_refreshToken_null_when_success()
        {
            var service = A.Fake<IAccountService>();
            var http = new DefaultHttpContext();
            var sut = CreateController(service, http);

            var dto = new LoginDTO { UserName = "mike", Password = "good" };
            const string refresh = "REFRESH_TOKEN_ABC";

            A.CallTo(() => service.LoginAsync(
                    A<LoginCommandModel>.That.Matches(c =>
                        c.UserName == dto.UserName &&
                        c.Password == dto.Password)))
                .Returns(Result<NewUserDTO>.Success(new NewUserDTO
                {
                    UserName = "mike",
                    EmailAddress = "mike@test.com",
                    Token = "ACCESS",
                    RefreshToken = refresh
                }));

            var result = await sut.Login(dto);

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            var body = ok.Value.Should().BeOfType<NewUserDTO>().Subject;

            body.RefreshToken.Should().BeNull();

            var setCookie = http.Response.Headers["Set-Cookie"].ToString();
            setCookie.Should().Contain($"refreshToken={refresh}");
            setCookie.Should().ContainEquivalentOf("httponly");
            setCookie.Should().ContainEquivalentOf("secure");
            setCookie.Should().ContainEquivalentOf("samesite=none");
            setCookie.Should().ContainEquivalentOf("path=/");
        }

        // RefreshToken endpoint bad situation
        [Fact]
        public async Task RefreshToken_returns_401_when_cookie_missing_and_does_not_call_service()
        {
            var service = A.Fake<IAccountService>();
            var http = new DefaultHttpContext(); // no Cookie header
            var sut = CreateController(service, http);

            var result = await sut.RefreshToken();

            var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorized.Value.Should().Be("No refresh token provided");

            A.CallTo(() => service.RefreshTokenAsync(A<string>._))
                .MustNotHaveHappened();
        }

        // RefreshToken endpoint good situation
        [Fact]
        public async Task RefreshToken_sets_new_cookie_and_returns_accessToken()
        {
            var service = A.Fake<IAccountService>();
            var http = new DefaultHttpContext();
            http.Request.Headers["Cookie"] = "refreshToken=OLD_REFRESH";

            var sut = CreateController(service, http);

            A.CallTo(() => service.RefreshTokenAsync("OLD_REFRESH"))
                .Returns(new AccessAndRefreshTokenDTO
                {
                    AccessToken = "NEW_ACCESS",
                    RefreshToken = "NEW_REFRESH"
                });

            var result = await sut.RefreshToken();

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(new { accessToken = "NEW_ACCESS" });

            var setCookie = http.Response.Headers["Set-Cookie"].ToString();
            setCookie.Should().Contain("refreshToken=NEW_REFRESH");
            setCookie.Should().ContainEquivalentOf("httponly");
            setCookie.Should().ContainEquivalentOf("secure");
            setCookie.Should().ContainEquivalentOf("samesite=none");
            setCookie.Should().ContainEquivalentOf("path=/");

            A.CallTo(() => service.RefreshTokenAsync("OLD_REFRESH"))
                .MustHaveHappenedOnceExactly();
        }

        // GoogleLogin ne mogu ovde da testiram 

        // SoftDeleteUser endpoint bad situation
        [Fact]
        public async Task SoftDeleteMe_returns_401_when_nameidentifier_claim_missing()
        {
            var service = A.Fake<IAccountService>();
            var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            var sut = CreateController(service, http);

            var result = await sut.SoftDeleteMe(CancellationToken.None);

            result.Should().BeOfType<UnauthorizedResult>();

            A.CallTo(() => service.SoftDeleteUserAsync(A<string>._, A<CancellationToken>._))
                .MustNotHaveHappened();
        }

        // SoftDelete endpoint good situation
        [Fact]
        public async Task SoftDeleteMe_calls_service_and_returns_204_when_claim_present()
        {
            var service = A.Fake<IAccountService>();

            var identity = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.NameIdentifier, "123")
            }, "jwt");

            var http = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            var sut = CreateController(service, http);

            var ct = new CancellationTokenSource().Token;

            A.CallTo(() => service.SoftDeleteUserAsync("123", ct))
                .Returns(Task.CompletedTask);

            var result = await sut.SoftDeleteMe(ct);

            result.Should().BeOfType<NoContentResult>();

            A.CallTo(() => service.SoftDeleteUserAsync("123", ct))
                .MustHaveHappenedOnceExactly();
        }
    }
}
