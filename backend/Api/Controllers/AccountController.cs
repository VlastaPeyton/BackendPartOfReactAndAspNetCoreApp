using System.Security.Claims;
using Api.DTOs.Account;
using Api.DTOs.AccountDTOs;
using Api.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace Api.Controllers
{

    [Route("api/account")] // https://localhost:port/api/account
    [ApiController]        // Zbog ovoga ne mora !ModelState.IsValid, [FromQuery], [FromBody] itd., jer je to implicitno, ali pisacu explicitno, jer citljiviji je kod sa ovim !
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService; // Pogledaj Services.txt i Dependency Injection.txt

        public AccountController(IAccountService accountService) => _accountService = accountService;

        /* 
         Pogledaj Controller.txt 

         Pogledaj Endpoint.txt 
         
         Ne koristim CancellationToken ni u jednom endpoint, jer nemam pure async endpoint iako pise async, zato sto built-in UserManager/SignInManager await metode ne prihvataju CancellationToken. 
          Mogu da im uradim extension, ali nema poente.
         
         Rate Limiter objasnjen u Program.cs
         
         Race conditions regarding AppUser je automatski odradjen u Register/Login/ResetPassword/RefreshToken endpoint, jer IdentityUser sadrzi ConcurrencyStamp polje koje se automatski menja svaki put 
        kad azuriram user ( AspNetUsers tabela) tako sto _userManage.UpdateAsync/ResetPasswordAsync automatski zameni ConcurrencyStamp kolonu i sprecava da se u "isto vreme" zameni neko user polje - pogledaj Race Conditions.txt
         
         Race conditions regarding Refresh Token in RefreshToken endpoint je sredjen rucno - pogledaj Race Conditions.txt

         Access Token and Refresh Token objasnjeni u SPA Security Best Practice.txt 
         
         Claims objasnjeno u Authentication middleware.txt

         Koristim Result pattern za ocekivane (biznis) greske i GlobalExceptionHandlingMiddleware za neocekivane greske - pogledaj Result pattern.txt i GlobalExceptionHandlingMiddleware.txt 
        
         AccountController nece imati CQRS endpointe jer me mrzi da ih iskucam, dok ostali controllers oce. 

         Nijedan endpoint ne sme imati [Authorize], jer za njih nemoguce je poslati JWT u Authorization header of request, dok endpointi ostalih controllera imaju [Authorize]
         */

        //[EnableRateLimiting("fast")] - nesto nije htelo kad sam imao ovaj ratelimiter ukljucen
        [HttpPost("register")] // https://localhost:port/api/account/register
        // Ne ide [Authorize], jer ovo je Register endpoint koji mora biti svim userima dostupan
        public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
        {   /* Pre ove metode, pokrenuto je OnModelCreating iz ApplicationDBContext i napunjena je AspNetRoles tabela prilikom Migracije.
               
               Kad novi user ukuca Email i Password, AspNetCoreIdentity ga upise u DB kroz ApplicationDbContext tj
            pretrazuje AspNetUsers tabelu i poredi uneti password sa stvarnim u toj tabeli. Moze da return i podatke iz Identity tabela ako treba. 
            
               Kad pozivam iz React FE ovaj endpoint, moram polja da nazovem i prosledim redosledom kao u RegisterDTO, jer RegisterDTO je tip input argumenta, a zbog [FromBody] moram u body of POST Request ih staviti.
             */

            // ModelState pokrene validation za RegisterDTO tj za zeljena RegisterDTO polja proverava na osnovu njihovih annotations.
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a ModelState objekat bice poslat u Response Body sa RegisterDTO poljima (EmailAddress, UserName i Password) u "errors" delu i bice auto JSON serializovan

            // Write to DB endpoint, pa mapiram RegisterDTO u CommandModel - pogledaj Services.txt + DTO vs entity klase.txt
            var command = new RegisterCommandModel
            {
                UserName = registerDTO.UserName,
                EmailAddress = registerDTO.EmailAddress,
                Password = registerDTO.Password,
            };
            // Odavde saljem dobre/lose odgovore vezane za Result pattern, a neocekivane greske se propagiraju u GlobalExceptionHandlingMiddleware odakle se salju klijentu

            var newUserDTO = await _accountService.RegisterAsync(command);

            string refreshToken = newUserDTO.RefreshToken!;

            // Refresh Token (not hashed!) is sent to Browser(not to FE) from Controller via highly-secured Cookie to prevent CSRF attack. 
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Mora zbog SameSite=None, ali FE mora u HTTPS
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7), // Mora isto kao appUser.RefreshTokenExpiryTime
                Path = "/", // Allow cookie to be sent to all endpoints, not just to refresh-token endpoint. If FE axios instance tries to send the refresh request from an interceptor that was triggered by a different protected endpoint (npr /api/stocks), the browser might not send the cookie
                IsEssential = true, // Ne kapiram zasto ali ovo nekad mora.
            });

            // Ne saljem refreshToken to FE i zato ga brisem iz NewUserDTO
            newUserDTO.RefreshToken = null;

            return Ok(newUserDTO);
            // Frontendu ce biti poslato NewUserDTO u Response Body, a StatusCode=200 u Response Status Line.
        }

        [HttpPost("login")] // https://localhost:port/api/account/login
        // Ne ide [Authorize], jer je ovo Login endpoint
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {   /* Kad existing user ukuca Email i Password, AspNetCoreIdentity ga nadje u bazi kroz ApplicationDbContext : IdentityDbContext<AppUser> tj
            pretrazuje AspNetUsers tabelu i poredi uneti password sa stvarnim u toj tabeli. Moze da return i podatke iz Identity tabela ako treba. 
            
            Kad pozivam iz ReactTS Frontenda ovaj Endpoint, moram polja da nazovem i prosledim redosledom kao u LoginDTO jer LoginDTO je tip input argumenta. Zbog [FromBody] moram u body of Request ih staviti.
            */

            // ModelState pokrene validaiton iz LoginDTO tj za zeljena LoginDTO polja proverava na osnovu njihovih annotations. - koristim FluentValidation umesto ovoga
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);

            // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a ModelState objekat bice poslat u Response Body sa LoginDTO poljima (UserName i Password) u "errors" delu poruke

            // Write to DB endpoint, pa mapiram LoginDTO u CommandModel - pogledaj Services.txt + DTO vs entity klase.txt
            var command = new LoginCommandModel
            {
                UserName = loginDTO.UserName,
                Password = loginDTO.Password,
            };
            // Odavde saljem dobre/lose odgovore vezane za Result pattern, a neocekivane greske se propagiraju u GlobalExceptionHandlingMiddleware odakle se salju klijentu

            var result = await _accountService.LoginAsync(command); // result pattern 
            if (result.IsFailure)
                return Unauthorized(new { message = result.Error }); // Salje objekat da bi se automatski napravio u JSON jer response body uvek JSON treba biti

            var appUser = result.Value!; // NewUserDTO

            string refreshToken = appUser.RefreshToken!; 

            // Refresh Token (not hashed !) is sent from Controller to Browser(not to FE) via highly-secured Cookie to prevent CSRF attack
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Mora zbog SameSite=None, ali FE mora u HTTPS
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7), // Mora da odgovara appUser.RefreshTokenExpiryTime
                Path = "/", // Allow cookie to be sent to all endpoints, not just refresh-token. if axios instance tries to send the refresh request from an interceptor that was triggered by a different protected endpoint (npr /api/stocks), the browser might not send the cookie
                IsEssential = true, // Ne kapiram zasto ali ovo nekad mora.
            });

            // Ne saljem refreshToken to FE i zato ga brisem iz NewUserDTO
            appUser.RefreshToken = null; 

            return Ok(appUser);
            // Frontendu ce biti poslato StatusCode=200 u Response Status Line, a NewUserDTO u Response Body.     
        }

        [HttpPost("forgotpassword")] // https://localhost:port/api/account/forgotpassword
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO forgotPasswordDTO)
        // Email se obicno salje u Request Body from FE (moze i FromQuery, ali nije dobra praksa)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a polje EmailAddress iz ModelState tj iz ForgotPasswordDTO ce biti prosledjeno u "errors" delu of Request sa objasnjenjem - u handleError smo uhvatili ovaj tip greske 

            // Ako user namerno unese email koji nije u bazi, BE vraca Ok("Reset password link is sent to your email") zbog sigurnosti jer onda je user ustvari napadac i da ne provali da taj mejl ne postoji pa da ne predje na drugi email koji mozda postoji. Ovako zbunimo napadaca.

            // Odavde saljem dobre/lose odgovore vezane za Result pattern, a neocekivane greske se propagiraju u GlobalExceptionHandlingMiddleware odakle se salju klijentu

            // Nema modifikacija baze, pa ne mapiram DTO u CommandModel klasu, jer je read endpoint

            await _accountService.ForgotPasswordAsync(forgotPasswordDTO);

            return Ok("Reset password link is sent to your email");
        }

        [HttpPost("reset-password")] // https://localhost:port/reset-password
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO resetPasswordDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            // Frontendu ce biti poslato StatusCode=400 u Response Status Line, a polje EmailAddress iz ModelState tj iz ResetPasswordDTO ce biti prosledjeno u "errors" delu of Request sa objasnjenjem - u handleError smo uhvatili ovaj tip greske 

            // Always return success to prevent email enumeration attack ! But only actually reset if user exists

            // Odavde saljem dobre/lose odgovore vezane za Result pattern, a neocekivane greske se propagiraju u GlobalExceptionHandlingMiddleware odakle se salju klijentu

            // Write to DB endpoint, pa mapiram ResetPasswordDTO u CommandModel - pogledaj Services.txt + DTO vs entity klase.txt
            var command = new ResetPasswordCommandModel
            {
                NewPassword = resetPasswordDTO.NewPassword,
                ResetPasswordToken = resetPasswordDTO.ResetPasswordToken,
                EmailAddress = resetPasswordDTO.EmailAddress
            }; 
            await _accountService.ResetPasswordAsync(command);
            // Always return the same response regardless of whether user exists or reset succeeded as this prevents timing attacks and email enumeration
            return Ok("If the email exists in our system, the password has been reset.");
        }

        /* Pre nego FE posalje Request to protected endpoint, FE automatski odredi da li je trenutni JWT(Access Token) blizu isteka, pa ako jeste, taj request ceka da Axios interceptor kaze Browseru da pozove ovaj endpoint i prosledi Cookie (koji sadrzi Refresh Token)
         da bi BE, za tog usera, napravi novi Refresh Token, invalidirao stari RefreshToken i kreira novi JWT. Nema argumenta, jer Browser(ne FE) mu salje Cookie. Onda BE posalje new Cookie to browser i new JWT to FE, pa Axios nastavi poziv protected endpointa 
         koji je poceo pre poziva ovog endpointa, sada sa new JWT.
        */
        [HttpGet("refresh-token")]
        //[EnableRateLimiting("slow")] // To prevent attacks
        // Nema [Authorize], jer kad istekne JWT, user vise ne moze da se prepozna u BE i onda nikad RefreshToken endpoint ne bi mogo da se aktivira
        public async Task<IActionResult> RefreshToken()
        {   
            // Access Cookies sent by the Browser( when client wanted it). Isti ovaj Cookie je Login/Register Endpoint poslao Browseru
            if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken)) // U Login/Register sam nazvao refreshToken i zato ovde ga ocitavam
                return Unauthorized("No refresh token provided");

            // Odavde saljem dobre/lose odgovore vezane za Result pattern, a neocekivane greske se propagiraju u GlobalExceptionHandlingMiddleware odakle se salju klijentu

            var newAccessAndRefreshTokenDTO = await _accountService.RefreshTokenAsync(refreshToken);

            string newAccessToken = newAccessAndRefreshTokenDTO.AccessToken;
            string newRefreshToken = newAccessAndRefreshTokenDTO.RefreshToken;

            // Salje secured Cookie to Browser koji mora imati isti key "refreshToken" kao Cookie poslat iz Login/Register kako bi na pocetku ovog Endpoint mogo uvek da dohvatim refresh token nebitno da l je novi ili inicijalni
            Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Mora zbog SameSite=None, ali FE mora u HTTPS
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7), // Mora ito kao appUser.RefreshTokenExpiryTime
                Path = "/", // Allow cookie to be sent to all endpoints, not just refresh-token. if axios instance tries to send the refresh request from an interceptor that was triggered by a different protected endpoint (npr /api/stocks), the browser might not send the cookie
                IsEssential = true, // Ne kapiram zasto ali ovo nekad mora.
            });

            return Ok(new { accessToken = newAccessToken }); // Access Token se salje kroz Response Body i bas mora kao anonimna klasa, a ne samo string, jer se klasa automatski pretvori u JSON i FE onda lakse pristupa tome
        }

        // Pogledaj GoogleLogin.txt  
        [HttpGet("login/google")]
        public IActionResult GoogleLogin()
        {   
            var properties = new AuthenticationProperties
            {   // After Google login completes, redirect user to this endpoint
                RedirectUri = "/login/google/callback"
            };

            return Challenge(properties, "Google");
        /* Creates OAuth request using "Google" auth scheme from Program.cs. 
        Browser is redirected to https://accounts.google.com/o/oauth2/v2/auth?client_id=...&redirect_uri=...
        */
        }

        // Pogledaj GoogleLogin.txt
        [HttpGet("login/google/callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            NewUserDTO newUserDto = await _accountService.GoogleLoginRegisterAsync();

            // Set RefreshToken in cookie as in Login/Register endpoint
            Response.Cookies.Append("refreshToken", newUserDto.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7),
                Path = "/",
                IsEssential = true,
            });

            // Ne saljem refreshToken to FE i zato ga brisem iz newUserDTO
            newUserDto.RefreshToken = null;

            return Ok(newUserDto);
        }
    }
}
