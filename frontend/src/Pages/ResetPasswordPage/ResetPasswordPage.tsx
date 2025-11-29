import * as Yup from "yup";
import { useAuth } from "../../Context/useAuthContext";
import { yupResolver } from "@hookform/resolvers/yup";
import { useForm } from "react-hook-form";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";

/* Ako sam zaboravio password, u LoginPage kliknem "Forgto password" => odvede me na ForgotPasswordPage => unesem email i ako je tacan, stigne poruka na adresu
sa linkom oblika "http://localhost:3000/reset-password?token=Q2ZESjhENHVhLzRwRTl0TXFZcWlKbnVaN&email=user%40gmail.com" sto predstavlja ResetPasswordPage + Query Parameters after ?. 
Ali bez obzira na query parameters otvara ResetPasswordPage (localhost:3000/reset-password). %40 = @ i to FE automatski konvertuje.

Napadac moze sam napisati localhost:3000/reset-password?token=123asd&email=user%40gmail.com, i otvorice mu se ResetPasswordPage tj nju moze i samo preko localhost:3000/reset-password otvoriti
i ukucati bilo koji novi password za zeljeni email, ali u BE se proverava da li taj token odgovara tom mejlu !
*/

type Props = {}

type ResetPasswordForm = {
    newPassword: string;
    confirmPassword: string;
}

// Yup za validation. Za ovo postoji validacija i u BE, jer to je prava validacija bitna za reset password.
const validation = Yup.object().shape({
    /*ResetPasswordPage mora slati newPassowrd,resetPasswordToken  i email to ResetPassword Endpoint.*/
    newPassword: Yup.string().required("Password is required"),
    confirmPassword: Yup.string().required("Confirm password is required").oneOf([Yup.ref("newPassword")], "Passwords must match"), // Jer moraju isti biti oba passworda
    // Mora ista imena i redosled polja kao u ResetPasswordForm
});

const ResetPasswordPage = (props: Props) => {

    const queryParams = new URLSearchParams(window.location.search); // Ne moze useParams jer to vazi samo za localhost:port/route/:parameter da dohvatim parameter 
    const resetPasswordToken = queryParams.get("token");  // Iz token=Q2ZESjhENHVhLzRwRTl0TXFZcWlKbnVaN izvadi Q2ZESjhENHVhLzRwRTl0TXFZcWlKbnVaN
    const email = queryParams.get("email");  // Iz email=user%40gmail.com izvadi user%40gmail.com i pretvori u user@gmail.com
    // queryParams.get moze da vrati string or  null 
    
    const navigate = useNavigate();

    // U useAuthContext.tsx dodajem forgotPassword funkciju. 
    const {resetPassword} = useAuth(); // Pogledaj LoginPage objasnjeno je tamo kako radi useAuthContext.

    const {
            // Ova 3 objekta useForm vraca i zato moraju ovako da se zovu, jer su im to built-in imena.
            register,      // Zbog ...register(email) u html mi je potrebno ovo
            handleSubmit,  /* Zbog onSubmit={handleSubmit(handleForgotPassword)} u html mi je potrebno ovo. Prevents default automatski. Uzme sve iz forme u obliku ResetPasswordForm i prosledi u handleForgotPassword.
                            Takodje, validira formu using yurResolver. Ako invalid forma, napravi formState.errors. Ako valid, napravi ForgotPasswordForm objekat sa poljima iz forme i pozove handleForgotPassword.*/
            formState: {errors}, // Destruktuira formState koji sadrzi samo 1 polje, pa mi samo treba errors objekat sa poljima email. Ovo je isto kao formState.errors, pa moze errors.email
    } = useForm<ResetPasswordForm>({resolver: yupResolver(validation)}); // Koristim yupResolver za validaciju forme
    
    const handleResetPassword = async (form: ResetPasswordForm) => {
      // Jer queryParams.get moze da vrati string or null i onda osiguram da resetPassword se poziva kad resetPasswordToken i email su string
      if (!resetPasswordToken || !email) return;
    
      const success = await resetPassword(form.newPassword, resetPasswordToken, email); // Waits for API call jer ako nema await on pokrene resetPassword i odma ide ispod bez da success znamo sta je 
    
      if (success) 
        navigate("/login"); // Only navigates AFTER password is successfully reset
    
    }

  return (
    <div className="p-6 space-y-4 md:space-y-6 sm:p-8">
        <h1 className="text-xl font-bold leading-tight tracking-tight text-gray-900 md:text-2xl dark:text-white">
            Reset password form 
        </h1>

        <form className="space-y-4 md:space-y-6" onSubmit={handleSubmit(handleResetPassword)}> {/* Ne treba (e) => handleSubmit jer on sam ima prevent default u sebi + on sam izdvoji sve iz forme i prosledi u handlelogin */}
            <div>
                <label
                  htmlFor="newPassword" 
                  className="block mb-2 text-sm font-medium text-gray-900 dark:text-white"
                >
                  NewPassword
                </label>
                <input
                  type="password"
                  id="newPassword"
                  className="bg-gray-50 border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                  placeholder="NewPassword"
                  {...register("newPassword")}
                />
                {errors.newPassword ? <p>{errors.newPassword.message}</p> : ""}
            </div>

            <div>
                <label
                  htmlFor="confirmPassword" 
                  className="block mb-2 text-sm font-medium text-gray-900 dark:text-white"
                >
                  NewPassword
                </label>
                <input
                  type="password"
                  id="confirmPassword"
                  className="bg-gray-50 border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                  placeholder="Confirmassword"
                  {...register("confirmPassword")}
                />
                {errors.confirmPassword ? <p>{errors.confirmPassword.message}</p> : ""}
            </div>

            <button
              type="submit"
              className="w-full bg-blue-600 text-white rounded p-2 hover:bg-blue-700"
            >
              Submit new password
            </button>

        </form>

    </div>
  )
}

export default ResetPasswordPage