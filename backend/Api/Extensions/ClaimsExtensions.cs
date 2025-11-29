using System.Security.Claims;

namespace Api.Extensions
{   // Mora uvek static kad extensiom method pravim
    public static class ClaimsExtensions
    {
        public static string GetUserName(this ClaimsPrincipal user)
        {   // ClaimsPrincipal je AppUser koji je currently logged in
            // Claims sam definisao prilikom JWT creation u TokenService i sadrzi Email i UserName(GivenName)
            return user.Claims.SingleOrDefault(x => x.Type == ClaimTypes.GivenName).Value; // UserName uzme from AppUser
        }
    }
}
