using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Data;
using Api.DTOs.Account;
using Api.DTOs.AccountDTOs;
using Api.Exceptions;
using Api.Exceptions_i_Result_pattern;
using Api.Exceptions_i_Result_pattern.Exceptions;
using Api.Interfaces;
using Api.Models;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    // Pogledaj Services.txt

    public class AccountService : IAccountService
    {
        // Interface za sve klase zbog DI, dok u Program.cs registrujem da prepozna interface kao tu klasu + zbog testabilnosti - pogledaj Dependency Injection.txt
        private readonly UserManager<AppUser> _userManager;     // Ovo moze jer AppUser:IdentityUser 
        private readonly SignInManager<AppUser> _signInManager; // Ovo moze jer AppUser:IdentityUser 
        // Zbog AppUser updating via UserManager i SignInManager, automatski je implementiran Race Condition using ConcurrencyStamp kolonu of IdentityUser
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ApplicationDBContext _dbContext; 
        private readonly ILogger<AccountService> _logger;

        public AccountService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, ILogger<AccountService> logger, IEmailService emailService,
                              ApplicationDBContext dbContext)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _logger = logger;
            _emailService = emailService;
            _dbContext = dbContext;
        }
        /* UserManager i SignInManager metode ne prihvataju cancellationToken i zato ga nema u endpoints ni ovde.
           Metode nemaju try-catch, pa se exception propagira u Controller odakle se dalje propagira u GlobalExceptionHandlingMidleware - pogledaj Services.txt
           Ako koristim Service, ne koristim CQRS i obratno. Service proverava i baca excpetion ili vraca result pattern, dok Repository samo vraca null ako nije naso u bazi.
           Service prima/vraca DTO u controller, mapira DTO u Entity i obratno, a Repository prima/vraca Entity u Service !
        */
        public async Task<NewUserDTO> RegisterAsync(RegisterCommandModel command)
        {
            AppUser appUser = new AppUser
            {
                UserName = command.UserName,
                Email = command.EmailAddress
            };

            // Transaction: kreiranje korisnika + dodavanje role + cuvanje refresh token 
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {   
                // Kreiranje korisnika 
                var createdUser = await _userManager.CreateAsync(appUser, command.Password!); // Dodaje novog usera u AspNetUsers (IdentityUser) tabelu 
                /* Dodaje AppUser polja (UserName i Email) i command.Password (koga automatski hash-uje) u AspNetUsers tabelu tj kreira novi User u toj tabeli
                    CreateAsync sprecava kreiranje 2 usera sa istim UserName ili Email(za email sam morao u Program.cs da definisem rucno u AddIdentity)
                    CreateAsync behing the scenes attaches appUser to EF Core and populates every column of AspNetUsers table regarding ConcurrencyStamp column koja sprecava race conditions
                */
                if (!createdUser.Succeeded)
                    throw new UserCreatedException($"User creation failed in _userManager: {createdUser.Errors}");

                // Dodavanje role 
                var roleResult = await _userManager.AddToRoleAsync(appUser, "User"); // Mogu samo User ili Admin upisati za Role, jer samo te vrednosti su seedovane migracijom u OnModelCreating u AspNetRoles tabelu
                if (!roleResult.Succeeded)
                    throw new RoleAssignmentException($"Role assignment failed in _userManager: {roleResult.Errors}");

                // Generise token 
                var accessToken = _tokenService.CreateAccessToken(appUser);
                var refreshToken = _tokenService.CreateRefreshToken();
                var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken); // Hash, jer u DB samo hash refresh token stavljam

                appUser.RefreshTokenHash = hashedRefreshToken; // U DB ide hash, dok u Cookie ide non-hashed refresh token
                appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token is long lived 
                appUser.LastRefreshTokenUsedAt = new DateTime(2000, 1, 1); // Prilikom register, user nije nijednom jos uvek koristio Refresh Token, pa mu dodajem neku idiotsku vrednost koja simulira nikad korisceno

                // Moram azurirati appUser u bazi zbog RefreshToken
                var updateResult = await _userManager.UpdateAsync(appUser); // ConcurrencyStamp column of IdentityUser prevents overwriting if another request wanna update the same user right after registration => race condition prevented
                if (!updateResult.Succeeded)
                    throw new UserCreatedException("Failed to update refresh token");

                await transaction.CommitAsync();

                // Service ne radi nista sa Cookies (refreshToken), vec to ostaje odgovornost of AccountController. Service samo prosledi not-hashed refreshToken, jer to ide u Cookie.
                return new NewUserDTO { UserName = appUser.UserName, EmailAddress = appUser.Email, Token = accessToken, RefreshToken = refreshToken };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw; // Propagira uzvodno u stack do prvog catch (GlobalExceptionHandlingMiddleware)
            } 
        }

        public async Task<Result<NewUserDTO>> LoginAsync(LoginCommandModel command)
        {   
            
            var appUser = await _userManager.FindByNameAsync(command.UserName); // Bolji i sigurniji nacin nego 2 linije iznad i takodje pretrazuje AspNetUsers tabelu da nadjemo AppUser by UserName
                                                                                 // appUser je ocitao sve kolone iz zeljene vrste AspNetUsers tabele, medju kojima je i ConcurrencyStamp koji sprecava race conditions
            if (appUser is null)
                //throw new WrongUsernameException($"Invalid username"); - umesto exception, koristim Result pattern jer nije neocekivana greska systema, vec biznis logika
                return Result<NewUserDTO>.Fail("Invalid username");

            // Ako UserName dobar, proverava password tj hashes it and compares it with PasswordHash column in AspNetUsers jer ne postoji Password kolona u AspNetUsers vec samo PasswordHash zbog sigurnosti
            var result = await _signInManager.CheckPasswordSignInAsync(appUser, command.Password, false);
            if (!result.Succeeded)
                //throw new WrongPasswordException($"Invalid password"); - umesto exception, koristim Result pattern jer nije neocekivana greska systema, vec biznis logika
                return Result<NewUserDTO>.Fail("Invalid password");

            var accessToken = _tokenService.CreateAccessToken(appUser);
            var refreshToken = _tokenService.CreateRefreshToken();
            var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken); // Before putting it in DB dobro je hashovati ga

            appUser.RefreshTokenHash = hashedRefreshToken; // U DB ide hash, dok u Cookie ide obican refresh token
            appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Refresh token is long lived. 
            appUser.LastRefreshTokenUsedAt = new DateTime(2000, 1, 1); // Prilikom login, user nije nijednom jos uvek koristio Refresh Token, pa mu dodajem neku idiotsku vrednost koja simulira nikad korisceno
            // Moram azurirati appUser u bazi zbog Refresh Token
            await _userManager.UpdateAsync(appUser); // ConcurrencyStamp column of IdentityUser stops a race where two logins for the same account try to update refresh token fields at the same time - race condition sprecen. 

            // Service ne radi nista sa Cookies (refreshToken), vec to ostaje odgovornost of AccountController. Service samo prosledi not-hashed refreshToken, jer to ide u Cookie.
            var newUserDTO = new NewUserDTO { UserName = appUser.UserName, EmailAddress = appUser.Email, Token = accessToken, RefreshToken = refreshToken };

            return Result<NewUserDTO>.Success(newUserDTO);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDTO forgotPasswordDTO)
        {   
            var user = await _userManager.FindByEmailAsync(forgotPasswordDTO.EmailAddress);
            // If email is not found, just send "success" mesage to FE da zavaramo trag napadacu.
            // user sadrzi celu vrstu iz AspNetUsers tabele medju kojom je ConcurrencyStamp kolona koja sprecava race conditions 
            if (user is null)          
                throw new ForgotPasswordException("Reset password link is sent to your email ali ne znas da l je uspesno ili nije jer fora je da zavaram trag");
            
            // If email found, generate a secure & time-limited (by default to 1day) reset token, send email and success message to FE
            var passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(user); // token je samo za ovog usera validan i nije skladisten u bazi 
            // Encode token to safely include in URL later
            var encodedToken = WebUtility.UrlEncode(passwordResetToken); // Encode before putting in URL
            // Create frontend reset-password url 
            string frontendBaseUrl = Env.GetString("frontendBaseURL"); // U Program.cs sam Env.Load() i zato ovo moze.
            var resetUrl = $"{frontendBaseUrl}/reset-password?token={encodedToken}&email={user.Email}"; // Ne treba email biit poslat, rizicno je !
            // frontendBaseUrl/reset-password mora da postoji u FE kao route da bi ovo moglo !
            // Send email to user.Email containing "Reset Password" subject and message containing reset-password url 
            await _emailService.SendEmailAsync(user.Email, "Reset Password", $"Click <a href='{HtmlEncoder.Default.Encode(resetUrl)}'>here</a> to reset your password.");
            // URL je oblika http://localhost:port/reset-password?token=sad8282s9&email=adresa@gmail.com. Iako imam ovaj Query Parameter, ReactTS svakako otvara ResetPassword endpoint tj http://localhost:port/reset-password (ResetPasswordPage)         
            // Kada .NET sends reset password url, odma zaboravi kakav je token, jer token nije skladisten nigde osim u varijabli. Onda user klikne na link u mejlu, cime aktivira ResetPassword endpoint, a .NET ima mehanizam u ResetPasswordAsync, koji decodes token iz linka i vidi user credentials u tokenu
        }

        public async Task ResetPasswordAsync(ResetPasswordCommandModel command)
        {   
            // Find user by email
            var user = await _userManager.FindByEmailAsync(command.EmailAddress); // Ocitane sve kolone ove vrste, pa i ConcurrencyStamp koja sprecava race conditions
            if (user is null)
            {
                _logger.LogError("User not found during password reset ali posalji 200 klijentu da zavara trag");
                throw new ResetPasswordException("If the email exists in our system, the password has been reset");
            }

            var result = await _userManager.ResetPasswordAsync(user, command.ResetPasswordToken, command.NewPassword);
            /* Kada user kliknuo Forgot Password, dobio je reset password link u email, a kad kliknuo na link on pokrenuo je ovaj endpoint.
                ResetPasswordAsync ima mehanizam da decodes token i da izvadi sve iz njega i provedi da li je to isto kao kad je ForgotPassword endpoint encodovao token.
                Proveri da l je za ovaj user generisan resetPasswordToken u ForgotPassword endpoint i da li NewPassword se slaze sa zahtevima u Program.cs
                Due to ConcurrencyStamp column of IdentityUser, ResetPasswordAsync azurira tu kolonu cime prevents overwriting if another request has updated the user since the reset token was issued - race condition sprecen
            */
            if (!result.Succeeded)
            {
                _logger.LogError("Password reset failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new ResetPasswordException("If the email exists in our system, the password has been reset");
            }
        }

        public async Task<AccessAndRefreshTokenDTO> RefreshTokenAsync(string? refreshToken)
        {   
            _logger.LogInformation("BE primio refreshtoken cookie");

            var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken); // Jer u bazi skladistim Hash Refresh Token, dok u Cookie je obican Refresh Token

            var appUser = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokenHash == hashedRefreshToken);

            if (appUser is null || appUser.RefreshTokenExpiryTime <= DateTime.UtcNow) 
                throw new RefreshTokenException("User not found during refreshtoken or invalid refreshToken");
            
            // Prevent double use:  poredim sa DateTime(2000,1,1) jer je to za LastRefreshTokenUsedAt u Login/Register postavljeno kao inicijalna vrednost oznavajuci da RefreshToken nije koriscen ni jednom do tada
            if (appUser.LastRefreshTokenUsedAt > new DateTime(2000, 1, 1) && (DateTime.UtcNow - appUser.LastRefreshTokenUsedAt)?.TotalSeconds < 10)
            {   /* Uslov je 10s za real-world apps koji osigurava da ne moze unutar 10s 2 ili vise puta da ovaj endpoint bude pozvan. Sprecavam abuse ovim. 
                    Ovo je u skladu sa 30s JWT expiry time u AxiosWithJWTForBackend.tsx u FE, jer ako nije, onda problem. */
                throw new RefreshTokenException("RefreshToken used too frequentyl");
            }

            // Token rotation
            var newAccessToken = _tokenService.CreateAccessToken(appUser);
            var newRefreshToken = _tokenService.CreateRefreshToken();
            var newHashedRefreshToken = _tokenService.HashRefreshToken(newRefreshToken);

            // Opciona transakcija, ne mora jer ima jedna operacija, ali zbog konzistentnosti 
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                appUser.RefreshTokenHash = newHashedRefreshToken;
                appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                appUser.LastRefreshTokenUsedAt = DateTime.UtcNow;
                // Update appUser in DB
                var updateResult = await _userManager.UpdateAsync(appUser); // Prevents a race condition, jer azurira automatski ConcurrencyStamp kolonu, wheen two refresh requests try to update refresh token field simultaneously.
                if (!updateResult.Succeeded)
                    throw new RefreshTokenException("Failed to update refresh token");

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            
            return new AccessAndRefreshTokenDTO { AccessToken = newAccessToken, RefreshToken = newRefreshToken };
        }

        public async Task<NewUserDTO> GoogleLoginRegisterAsync()
        {
            // Ocita informacija o Google login koje je Google Auth Handler smestio u HttpContext
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info is null)
                throw new GoogleLoginException("Error loading external login information");

            // Izvlaci provider podatke 
            var loginProvider = info.LoginProvider; // "Google"
            var providerKey = info.ProviderKey;     // Unique Google User ID

            // Izvlaci email iz Google claims
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                throw new GoogleLoginException("Google account dont provide an email");

            // Provera da li korisnik vec postoji i to povezan sa Google nalogom
            var appUser = await _userManager.FindByLoginAsync(loginProvider, providerKey);
            if (appUser is null)
            {
                // Korisnik nije povezan sa Google nalog
                // Provera da li postoji korisnik sa istim email u bazi (nebitno kako je registrovan)
                appUser = await _userManager.FindByEmailAsync(email);

                // Transaction: kreiranje korisnika + dodavanje role + linking Google naloga
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    if (appUser is null)
                    {
                        // Kreiranje novi korisnik, jer ne postoji niko u bazi sa ovim email
                        appUser = new AppUser { UserName = email, Email = email, EmailConfirmed = true};

                        var createResult = await _userManager.CreateAsync(appUser);
                        if (!createResult.Succeeded)
                            throw new GoogleLoginException("Failed to create user");

                        // Dodavanje role 
                        var roleResult = await _userManager.AddToRoleAsync(appUser, "User");
                        if (!roleResult.Succeeded)
                            throw new GoogleLoginException("Failed to assign a role");
                    }

                    // Povezuje Google nalog sa korisnikom
                    var addLoginResult = await _userManager.AddLoginAsync(appUser, info);
                    if (!addLoginResult.Succeeded)
                        throw new GoogleLoginException("Failed to link google account");

                    // Commit transaction 
                    await transaction.CommitAsync();
                }
                catch
                {
                    // Rollback 
                    await transaction.RollbackAsync();
                    throw; // Prosledi uzvodno u stack do narednog catch (GlobalExceptionHandlingMiddleware)
                }
            }

            // JWT AccessToken i RefreshToken 
            var accessToken = _tokenService.CreateAccessToken(appUser);
            var refreshToken = _tokenService.CreateRefreshToken();
            var hashedRefreshToken = _tokenService.HashRefreshToken(refreshToken);

            // Transaction: azuriranje refresh token
            using var tokenTransaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                appUser.RefreshTokenHash = hashedRefreshToken;
                appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                appUser.LastRefreshTokenUsedAt = new DateTime(2000, 1, 1);

                var updateResult = await _userManager.UpdateAsync(appUser);
                if (!updateResult.Succeeded)
                    throw new GoogleLoginException("Failed to update refresh token");

                await tokenTransaction.CommitAsync();
            }
            catch
            {
                await tokenTransaction.RollbackAsync();
                throw;
            }

            return new NewUserDTO
            {
                UserName = appUser.UserName,
                EmailAddress = appUser.Email,
                Token = accessToken,
                RefreshToken = refreshToken,
            };
        }
    }
}
