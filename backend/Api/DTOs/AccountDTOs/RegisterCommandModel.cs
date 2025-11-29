namespace Api.DTOs.AccountDTOs
{   // Ista polja kao Register DTO
    public class RegisterCommandModel
    {   // Nema annonations, jer ovo nije ulazi objekat u endpoint, vec kad se ulazni objekat validira, onda se on mapira u ovaj objekat
        public string? UserName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Password { get; set; }
    }
}
