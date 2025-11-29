using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Account
{
    /* Ovog redosleda i imena navodim arugmente u React request kad pozivam Login endpoint
       Mora imati annotations jer ovu klasu koristim za writing to DB Endpoint argument pa da ModelState moze da validira polja.
       Request DTO se koristi kad FE poziva endpoint, jer ne sme Models (entity) klasu koristiti u tom slucaju, jer ona sluzi samo za DB interaction u Repository
    */
    public class LoginDTO
    {   
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
