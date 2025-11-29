import * as Yup from "yup";
import { useAuth } from "../../Context/useAuthContext";
import { yupResolver } from "@hookform/resolvers/yup";
import { useForm } from "react-hook-form";



/* ForgotPasswordPage mora biti posebna strana tj da ne bude u okviru LoginPage. Kada u LoginPage user klikne "Forgot password", otvori se ova
stranica koja sadrzi input email form gde user unosi email za koji je zaboravio password. AKo napadac unese pogresnu email slucajno, stize poruka da je poruka na mejl poslata
sa reset-password URL jer to je dobta praksa da on ne svali da email adresa nije u bazi. AKo je email adresa u bazi, svakako toast.success, ali i poruka na mejl.
*/
type Props = {}

type ForgotPasswordForm = {
    email: string
}

// Yup za validation. Za ovo postoji validacija i u BE, jer to je prava validacija bitna za forgot password.
const validation = Yup.object().shape({
    email: Yup.string().required("Email is required"), // Yup.string() - jer email:string u ForgotPasswordForm
    // Mora ista imena i redosled polja kao u ForgotPasswordForm
});

const ForgotPasswordPage = (props: Props) => {
    // U useAuthContext.tsx dodajem forgotPassword funkciju. 
    const {forgotPassword} = useAuth(); // Pogledaj LoginPage objasnjeno je tamo kako radi useAuthContext. 
    
    const {
            // Ova 3 objekta useForm vraca i zato moraju ovako da se zovu, jer su im to built-in imena.
            register,      // Zbog ...register(email) u html mi je potrebno ovo
            handleSubmit,  /* Zbog onSubmit={handleSubmit(handleForgotPassword)} u html mi je potrebno ovo. Prevents default automatski. Uzme sve iz forme u obliku ResetPasswordForm i prosledi u handleForgotPassword.
                          Takodje, validira formu using yurResolver. Ako invalid forma, napravi formState.errors. Ako valid, napravi ForgotPasswordForm objekat sa poljima iz forme i pozove handleForgotPassword.*/
            reset,     // Nakon unosa u formu da se isprazni polje 
            formState: {errors}, // Destruktuira formState koji sadrzi samo 1 polje, pa mi samo treba errors objekat sa poljima email. Ovo je isto kao formState.errors, pa moze errors.email
    } = useForm<ForgotPasswordForm>({resolver: yupResolver(validation)}); // Koristim yupResolver za validaciju forme
    
    const handleForgotPassword = (form: ForgotPasswordForm) => {
        forgotPassword(form.email); // forgotPassword zahteva ovaj argument
        reset(); // Isprazni se input polje nakon submmit 
    }

  return (
    <div className="p-6 space-y-4 md:space-y-6 sm:p-8">
        <h1 className="text-xl font-bold leading-tight tracking-tight text-gray-900 md:text-2xl dark:text-white">
            Forgot password form 
        </h1>
        <form className="space-y-4 md:space-y-6" onSubmit={handleSubmit(handleForgotPassword)}> {/* Ne treba (e) => handleSubmit jer on sam ima prevent default u sebi + on sam izdvoji sve iz forme i prosledi u handlelogin */}
            <div>
                <label
                  htmlFor="email" 
                  className="block mb-2 text-sm font-medium text-gray-900 dark:text-white"
                >
                  Email
                </label>
                <input
                  type="email"
                  id="email"
                  className="bg-gray-50 border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                  placeholder="Email"
                  {...register("email")}
                />
                {errors.email ? <p>{errors.email.message}</p> : ""}
            </div>

            <button
              type="submit"
              className="w-full bg-blue-600 text-white rounded p-2 hover:bg-blue-700"
            >
              Submit email 
            </button>
        </form>
    </div>
  )
}

export default ForgotPasswordPage;