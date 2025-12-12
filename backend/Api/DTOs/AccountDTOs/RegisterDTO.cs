using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    /* Ovog redosleda i imena navodim argumente u React request kad pozivam Register endpoint
       Mora imati annotations jer ovu klasu koristim za writing to DB Endpoint argument pa da ModelState moze da validira polja.
       Request DTO se koristi kad FE poziva endpoint, jer ne sme Models (entity) klasu koristiti u tom slucaju, jer ona sluzi samo za DB interaction u Repository
        
        U Program.cs sam definisao password zahteve (veliko/malo slovo i digit i min 8 duzina) gde UserManager/SignInManager proveravaju to, 
        pa nema potrebe ja ovde da proverim to, jer bolja praksa da provere oni, a ovde samo da proverim da l su prazna polja.
     */
    public class RegisterDTO
    {   // Ovog redosleda i imena navodim argumente kad gadjam Register endpoint 
        // [Required] ne ide uz "?", vec uz default vrednost 
        [Required]
        public string UserName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
