namespace Api.DTOs.Account
{
    public class AccessAndRefreshTokenDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
