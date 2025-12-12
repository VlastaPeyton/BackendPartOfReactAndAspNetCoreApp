namespace Api.DTOs.AccountDTOs
{   
    // Objasnjeno u RegisterCommandModel
    public class ResetPasswordCommandModel
    {
        public string NewPassword { get; set; } = null!;
        public string ResetPasswordToken { get; set; } = null!;
        public string EmailAddress { get; set; } = null!;
    }
}
