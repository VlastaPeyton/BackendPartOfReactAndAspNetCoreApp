using FluentValidation;

namespace Api.DTOs.AccountDTOs
{   
    // Objasnjeno u RegisterCommandModel
    public class LoginCommandModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    /* Umesto ModelState u AccountController, koristim FluentValidation u AccountService, jer je bolje.
       Validacija registrovana u Program.cs da bi moglo tokom runtime da se prepozna, jer ovo nije MediatR kao u CQRS sto je.  */

    public class LoginCommandModelValidator : AbstractValidator<LoginCommandModel>
    {    // AbstractValidator implements IValidator
        public LoginCommandModelValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required");

            /* U Program.cs sam definisao password zahteve (veliko/malo slovo i digit i min 8 duzina) gde UserManager/SignInManager proveravaju to, 
             * pa nema potrebe ja ovde da proverim to, jer bolja praksa da provere oni, a ovde samo da proverim da l su prazna polja. */
        }
    }
}
