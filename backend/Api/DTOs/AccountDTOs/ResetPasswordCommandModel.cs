namespace Api.DTOs.AccountDTOs
{   
    // Objasnjeno u RegisterCommandModel
    public class ResetPasswordCommandModel
    {
        public string NewPassword { get; set; }
        public string ResetPasswordToken { get; set; }
        public string EmailAddress { get; set; }
    }
}
