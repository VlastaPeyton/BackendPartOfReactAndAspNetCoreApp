using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Api.Data;
using Api.DTOs.AccountDTOs;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Interfaces.IRepositoryBase;
using Api.Localization;
using Api.Models;
using Api.Services;
using FakeItEasy;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


public class AccountServiceTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ApplicationDBContext _db;

    public AccountServiceTests()
    {
        // SQLite in-memory supports transactions (EF InMemory provider does not).
        _conn = new SqliteConnection("Filename=:memory:");
        _conn.Open();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseSqlite(_conn)
            .Options;

        _db = new ApplicationDBContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }

    // -------------------------
    // LoginAsync
    // -------------------------

    [Fact]
    public async Task LoginAsync_returns_Fail_when_validator_fails()
    {
        // Arrange
        var userManager = CreateFakeUserManager();
        var signInManager = CreateFakeSignInManager(userManager);

        var tokenService = A.Fake<ITokenService>();
        var emailService = A.Fake<IEmailService>();
        var logger = A.Fake<ILogger<AccountService>>();
        IValidator<LoginCommandModel> validator = new LoginCommandModelValidator(); // real validator
        var localizer = A.Fake<IStringLocalizer<Resource>>();

        var sut = new AccountService(
            userManager, signInManager, tokenService, logger, emailService, _db, validator, localizer,
            A.Fake<IUserRepository>(), A.Fake<ICommentRepositoryBase>(), A.Fake<IPortfolioRepository>());

        var invalid = new LoginCommandModel { UserName = "", Password = "" };

        // Act
        var result = await sut.LoginAsync(invalid);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();

        A.CallTo(() => userManager.FindByNameAsync(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => signInManager.CheckPasswordSignInAsync(A<AppUser>._, A<string>._, A<bool>._))
            .MustNotHaveHappened();
        A.CallTo(() => tokenService.CreateAccessToken(A<AppUser>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task LoginAsync_returns_Fail_when_user_not_found()
    {
        // Arrange
        var (sut, userManager, signInManager, tokenService) = CreateSutForLogin();

        A.CallTo(() => userManager.FindByNameAsync("mike"))
            .Returns((AppUser?)null);

        var cmd = new LoginCommandModel { UserName = "mike", Password = "good" };

        // Act
        var result = await sut.LoginAsync(cmd);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();

        A.CallTo(() => signInManager.CheckPasswordSignInAsync(A<AppUser>._, A<string>._, A<bool>._))
            .MustNotHaveHappened();
        A.CallTo(() => tokenService.CreateAccessToken(A<AppUser>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task LoginAsync_throws_UserDeletedException_when_user_IsDeleted_true()
    {
        // Arrange
        var (sut, userManager, _, _) = CreateSutForLogin(localizerSetup: l =>
        {
            A.CallTo(() => l["UserDeletedException"])
                .Returns(new LocalizedString("UserDeletedException", "User is deleted"));
        });

        var deletedUser = new AppUser
        {
            UserName = "mike",
            Email = "mike@test.com",
            IsDeleted = true
        };

        A.CallTo(() => userManager.FindByNameAsync("mike"))
            .Returns(deletedUser);

        // Act
        Func<Task> act = () => sut.LoginAsync(new LoginCommandModel { UserName = "mike", Password = "good" });

        // Assert
        await act.Should().ThrowAsync<UserDeletedException>();
    }

    [Fact]
    public async Task LoginAsync_returns_Fail_when_password_incorrect()
    {
        // Arrange
        var (sut, userManager, signInManager, _) = CreateSutForLogin();

        var user = new AppUser { UserName = "mike", Email = "mike@test.com", IsDeleted = false };

        A.CallTo(() => userManager.FindByNameAsync("mike")).Returns(user);
        A.CallTo(() => signInManager.CheckPasswordSignInAsync(user, "wrong", false))
            .Returns(SignInResult.Failed);

        // Act
        var result = await sut.LoginAsync(new LoginCommandModel { UserName = "mike", Password = "wrong" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("Invalid password");
    }

    [Fact]
    public async Task LoginAsync_returns_Success_and_updates_refresh_token_fields()
    {
        // Arrange
        var (sut, userManager, signInManager, tokenService) = CreateSutForLogin();

        var user = new AppUser { UserName = "mike", Email = "mike@test.com", IsDeleted = false };

        A.CallTo(() => userManager.FindByNameAsync("mike")).Returns(user);
        A.CallTo(() => signInManager.CheckPasswordSignInAsync(user, "good", false))
            .Returns(SignInResult.Success);

        A.CallTo(() => tokenService.CreateAccessToken(user)).Returns("ACCESS");
        A.CallTo(() => tokenService.CreateRefreshToken()).Returns("REFRESH");
        A.CallTo(() => tokenService.HashRefreshToken("REFRESH")).Returns("HASHED_REFRESH");

        A.CallTo(() => userManager.UpdateAsync(A<AppUser>._))
            .Returns(IdentityResult.Success);

        // Act
        var result = await sut.LoginAsync(new LoginCommandModel { UserName = "mike", Password = "good" });

        // Assert (result pattern)
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
        result.Value.Should().NotBeNull();

        result.Value!.UserName.Should().Be("mike");
        result.Value.EmailAddress.Should().Be("mike@test.com");
        result.Value.Token.Should().Be("ACCESS");
        result.Value.RefreshToken.Should().Be("REFRESH");

        // Assert (mutated user fields)
        user.RefreshTokenHash.Should().Be("HASHED_REFRESH");
        user.RefreshTokenExpiryTime.Should().NotBeNull();
        user.RefreshTokenExpiryTime!.Value.Should().BeAfter(DateTime.UtcNow.AddDays(6.9));
        user.LastRefreshTokenUsedAt.Should().BeNull();

        // Assert (persistence call)
        A.CallTo(() => userManager.UpdateAsync(user))
            .MustHaveHappenedOnceExactly();
    }

    // -------------------------
    // SoftDeleteUserAsync
    // -------------------------

    [Fact]
    public async Task SoftDeleteUserAsync_rolls_back_and_returns_when_user_not_found()
    {
        // Arrange
        var userManager = CreateFakeUserManager();
        var signInManager = CreateFakeSignInManager(userManager);

        var tokenService = A.Fake<ITokenService>();
        var emailService = A.Fake<IEmailService>();
        var logger = A.Fake<ILogger<AccountService>>();
        IValidator<LoginCommandModel> validator = new LoginCommandModelValidator();
        var localizer = A.Fake<IStringLocalizer<Resource>>();

        var userRepo = A.Fake<IUserRepository>();
        var commentRepo = A.Fake<ICommentRepositoryBase>();
        var portfolioRepo = A.Fake<IPortfolioRepository>();

        var sut = new AccountService(
            userManager, signInManager, tokenService, logger, emailService, _db, validator, localizer,
            userRepo, commentRepo, portfolioRepo);

        var ct = new CancellationTokenSource().Token;

        A.CallTo(() => commentRepo.DeleteByUserIdAsync("123", A<DateTime>._, ct)).Returns(Task.CompletedTask);
        A.CallTo(() => portfolioRepo.SoftDeleteByUserIdAsync("123", A<DateTime>._, ct)).Returns(Task.CompletedTask);
        A.CallTo(() => userRepo.SoftDeleteAsync("123", A<DateTime>._, ct)).Returns(0);

        // Act
        Func<Task> act = () => sut.SoftDeleteUserAsync("123", ct);

        // Assert
        await act.Should().NotThrowAsync();

        A.CallTo(() => commentRepo.DeleteByUserIdAsync("123", A<DateTime>._, ct))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => portfolioRepo.SoftDeleteByUserIdAsync("123", A<DateTime>._, ct))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => userRepo.SoftDeleteAsync("123", A<DateTime>._, ct))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SoftDeleteUserAsync_commits_when_user_found()
    {
        // Arrange
        var userManager = CreateFakeUserManager();
        var signInManager = CreateFakeSignInManager(userManager);

        var tokenService = A.Fake<ITokenService>();
        var emailService = A.Fake<IEmailService>();
        var logger = A.Fake<ILogger<AccountService>>();
        IValidator<LoginCommandModel> validator = new LoginCommandModelValidator();
        var localizer = A.Fake<IStringLocalizer<Resource>>();

        var userRepo = A.Fake<IUserRepository>();
        var commentRepo = A.Fake<ICommentRepositoryBase>();
        var portfolioRepo = A.Fake<IPortfolioRepository>();

        var sut = new AccountService(
            userManager, signInManager, tokenService, logger, emailService, _db, validator, localizer,
            userRepo, commentRepo, portfolioRepo);

        var ct = new CancellationTokenSource().Token;

        A.CallTo(() => commentRepo.DeleteByUserIdAsync("123", A<DateTime>._, ct)).Returns(Task.CompletedTask);
        A.CallTo(() => portfolioRepo.SoftDeleteByUserIdAsync("123", A<DateTime>._, ct)).Returns(Task.CompletedTask);
        A.CallTo(() => userRepo.SoftDeleteAsync("123", A<DateTime>._, ct)).Returns(1);

        // Act
        Func<Task> act = () => sut.SoftDeleteUserAsync("123", ct);

        // Assert
        await act.Should().NotThrowAsync();

        A.CallTo(() => userRepo.SoftDeleteAsync("123", A<DateTime>._, ct))
            .MustHaveHappenedOnceExactly();
    }

    // -------------------------
    // Helpers
    // -------------------------

    private (AccountService sut, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService)
        CreateSutForLogin(Action<IStringLocalizer<Resource>>? localizerSetup = null)
    {
        var userManager = CreateFakeUserManager();
        var signInManager = CreateFakeSignInManager(userManager);

        var tokenService = A.Fake<ITokenService>();
        var emailService = A.Fake<IEmailService>();
        var logger = A.Fake<ILogger<AccountService>>();
        IValidator<LoginCommandModel> validator = new LoginCommandModelValidator();
        var localizer = A.Fake<IStringLocalizer<Resource>>();

        localizerSetup?.Invoke(localizer);

        var sut = new AccountService(
            userManager, signInManager, tokenService, logger, emailService, _db, validator, localizer,
            A.Fake<IUserRepository>(), A.Fake<ICommentRepositoryBase>(), A.Fake<IPortfolioRepository>());

        return (sut, userManager, signInManager, tokenService);
    }

    private static UserManager<AppUser> CreateFakeUserManager()
    {
        var store = A.Fake<IUserStore<AppUser>>();

        // Create a fake UserManager with a real ctor. We fake its virtual methods (FindByNameAsync, UpdateAsync, etc.)
        return A.Fake<UserManager<AppUser>>(o => o.WithArgumentsForConstructor(() =>
            new UserManager<AppUser>(
                store,
                A.Fake<IOptions<IdentityOptions>>(),
                A.Fake<IPasswordHasher<AppUser>>(),
                new List<IUserValidator<AppUser>>(),
                new List<IPasswordValidator<AppUser>>(),
                A.Fake<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                A.Fake<IServiceProvider>(),
                A.Fake<ILogger<UserManager<AppUser>>>()
            )));
    }

    private static SignInManager<AppUser> CreateFakeSignInManager(UserManager<AppUser> userManager)
    {
        var accessor = A.Fake<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext();

        var claimsFactory = A.Fake<IUserClaimsPrincipalFactory<AppUser>>();

        var options = A.Fake<IOptions<IdentityOptions>>();
        A.CallTo(() => options.Value).Returns(new IdentityOptions());

        var schemeProvider = A.Fake<IAuthenticationSchemeProvider>();
        var confirmation = A.Fake<IUserConfirmation<AppUser>>();

        return A.Fake<SignInManager<AppUser>>(o => o.WithArgumentsForConstructor(() =>
            new SignInManager<AppUser>(
                userManager,
                accessor,
                claimsFactory,
                options,
                A.Fake<ILogger<SignInManager<AppUser>>>(),
                schemeProvider,
                confirmation
            )));
    }

}
