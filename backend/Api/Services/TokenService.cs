using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Api.Interfaces;
using Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Api.Service
{   
    /* 
       Metode su sync, jer nema potrebe da budu async posto metode ne komuniciraju sa bazom.
       
       Creating Access Token(short-lived) tj CreateToken method, je stateless tj JWT se ne unosi u bazu za korisnika, zato sto user claims are encoded in JWT. 
       Creating Refres Token(long-lived) tj GenerateRefreshToken method, nije stateless tj Refres Token se unosi u bazu za korisnika. 
       HashRefreshTOken jer before persisting Refresh Token in DB for corresponding user, needs to be hashed. 
     */
    public class TokenService : ITokenService 
    {   
        // Interface za sve klase zbog DI, dok u Program.cs registrujem da prepozna interface kao tu klasu + zbog testabilnosti - pogledaj Dependency Injection.txt
        private readonly IConfiguration _configuration;    // Za pristup svemu iz appsettings. _configuration je isto kao builder.Configuration u Program.cs
        private readonly SymmetricSecurityKey _signingKey; // Symetric - jer ocu sa istim kljucem to Sign and Verify JWT. SHA256 moram zbog ovoga koristiti.
        public TokenService(IConfiguration configuration) 
        { 
            _configuration = configuration;
            _signingKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["JWT:SigningKey"]!)); 
        }

        // Generate JWT(short-lived Access Token) after new User successfully register/login in AccountController.
        // Access Token is stateless tj ne upisuje se u bazu za zeljenog usera, vec u JWT (in-memory variable)
        public string CreateAccessToken(AppUser appUser)
        {   
            // User info (UserName/Password) je Claim - pogledaj Authentication middleware.txt
            var claims = new List<Claim>
            {   
                new Claim(JwtRegisteredClaimNames.Email, appUser.Email!),
                new Claim(JwtRegisteredClaimNames.GivenName, appUser.UserName!),
                new Claim(JwtRegisteredClaimNames.Sub, appUser.Id), // U ClaimsExtension ovo dohvatim sa NameIdentifier jer Sub se mapira u NameIdentifier
                // U ClaimsExtension.cs samo mogu dohvatiti Email/ GivenName/ NameIdentifier jer samo sam njih ovde setovao
            };

            // Signing credentials = type of Encryption
            var signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),    // Moram od List<Claim> napraviti ClaimsIdentity
                Expires = DateTime.UtcNow.AddMinutes(2), // Objasnjeno u "SPA Security Best Practice.txt"
                SigningCredentials = signingCredentials,
                Issuer = _configuration["JWT:Issuer"],   // From appsettings
                Audience = _configuration["JWT:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token); // Signed JWT string that is sent to Client
        }
        
        // Ne moze kreiranje RefreshToken u CreateAccessToken, jer AccessToken je stateless (in JWT), dok RefreshToken je stateful (in DB)
        public string CreateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            } 
            // Automatski close resources zbog using
        }
        
        public string HashRefreshToken(string refreshToken)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
