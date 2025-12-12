using Api.DTOs.Account;
using Api.DTOs.AccountDTOs;
using Api.Exceptions_i_Result_pattern;

namespace Api.Interfaces
{   
    // Odgovoran za AccountController endpoints - pogledaj Services.txt 
    public interface IAccountService // Postoji built-in IAuthenticationService, ali necu njega jer ne znam ga, vec pravim svoj custom
    {   // Servis prima DTO iz kontroler ako je read endpoint
        // Controller mapira DTO u command i salje servisu, ako je write endpoint, a onda servis mapira command model u entity ako treba i salje u repository
        Task<NewUserDTO> RegisterAsync(RegisterCommandModel command);
        Task<Result<NewUserDTO>> LoginAsync(LoginCommandModel command);
        Task ForgotPasswordAsync(ForgotPasswordDTO forgotPasswordDTO);
        Task ResetPasswordAsync(ResetPasswordCommandModel command);
        Task<AccessAndRefreshTokenDTO> RefreshTokenAsync(string refreshToken);
        Task<NewUserDTO> GoogleLoginRegisterAsync();
    }
}
