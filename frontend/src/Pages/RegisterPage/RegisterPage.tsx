import * as Yup from "yup";
import { yupResolver} from "@hookform/resolvers/yup";
import { useAuth } from '../../Context/useAuthContext';
import { useForm } from 'react-hook-form';

type Props = {}

type RegisterFormInputs = {
    userName: string,
    email: string,
    password: string
}

// Yup za validation. Za ovo postoji validacija i u BE, jer to je prava validacija bitna za register.
const validation = Yup.object().shape({
    userName: Yup.string().required("Username is required"), // Yup.string() - jer userName:string in RegisterFormInputs
    email: Yup.string().required("Email is required"),
    password: Yup.string().required("Password is required")
    // Mora ista imena i redosled polja kao u RegisterFormInputs
});

// Ovo je child Component of <UserProvider>, jer u App.tsx je smestena inside <UserProvider> tj unutar <Outlet>, jer u Routes.tsx <RegisterPage> je children za <App>
const RegisterPage = (props: Props) => {
    
    const {registerUser } = useAuth(); // registerUser mora, jer se tako zove unutar useAuthContext.tsx. Pogledaj useAuthContext.tsx tamo objasnjeno sve
    const {
        // Ova 3 objekta useForm vraca i zato moraju ovako da se zovu, jer su im to built-in imena.
        register,      // Zbog ...register(userName/password) u html mi je potrebno ovo
        handleSubmit,  /* Zbog onSubmit={handleSubmit(handleRegister)} u html mi je potrebno ovo. Prevents default automatski. Uzme sve iz forme u obliku RegisterFormInput i prosledi u handleRegister.
                      Takodje, validira formu using yurResolver. Ako invalid forma, napravi formState.errors. Ako valid, napravi RegisterFormInputs objekat sa poljima iz forme i pozove handleRegister.*/
        formState: {errors}, // Destruktuira formState koji sadrzi vise polja, pa mi samo treba errors objekat sa poljima userName i password. Ovo je isto kao formState.errors, pa moze errors.userName/password.message
    } = useForm<RegisterFormInputs>({resolver: yupResolver(validation)}); // Koristim yupResolver za validaciju forme

    // Ovde nema await reigsterUser jer nam ne treba
    const handleRegister = (form: RegisterFormInputs) => {
        registerUser(form.email, form.userName, form.password); // registerUser zahteva ovaj redosled argumenata.
    }

  // Ovaj HTML + Tailwind je objasnjen u LoginPage.tsx
  return (
    <section className="bg-gray-50 dark:bg-gray-900">

      <div className="flex flex-col items-center justify-center px-6 py-8 mx-auto md:h-screen lg:py-0">

        <div className="w-full bg-white rounded-lg shadow dark:border md:mb-20 sm:max-w-md xl:p-0 dark:bg-gray-800 dark:border-gray-700">

          <div className="p-6 space-y-4 md:space-y-6 sm:p-8">

            <h1 className="text-xl font-bold leading-tight tracking-tight text-gray-900 md:text-2xl dark:text-white">
              Sign up 
            </h1>

            <form className="space-y-4 md:space-y-6" onSubmit={handleSubmit(handleRegister)}> {/* Ne treba (e) => handleSubmit jer on sam ima prevent default u sebi + sam izdvoji sve iz handlelogin */}
              
              <div>
                <label
                  htmlFor="email"
                  className="block mb-2 text-sm font-medium text-gray-900 dark:text-white"
                >
                  Email
                </label>
                <input
                  type="text"
                  id="email"
                  className="bg-gray-50 border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                  placeholder="Email"
                  {...register("email")}
                />
                {errors.email ? <p>{errors.email.message}</p> : ""}
              </div>

              <div>
                <label
                  htmlFor="username"
                  className="block mb-2 text-sm font-medium text-gray-900 dark:text-white"
                >
                  Username
                </label>
                <input
                  type="text"
                  id="username"
                  className="bg-gray-50 border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                  placeholder="Username"
                  {...register("userName")}
                />
                {errors.userName ? <p>{errors.userName.message}</p> : ""}
              </div>

              <div>
                <label
                  htmlFor="password"
                  className="block mb-2 text-sm font-medium text-gray-900 dark:text-white"
                >
                  Password
                </label>
                <input
                  type="password"
                  id="password"
                  placeholder="••••••••"
                  className="bg-gray-50 border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                  {...register("password")}
                />
                {errors.password ? <p>{errors.password.message}</p> : ""}
              </div>

              <div className="flex items-center justify-between">
              </div>

              {/* type=submit => Kliknuti na Sign up da aktivira handleRegister gore naveden u onSubmit u zaglavlju <form...> */}
              <button
                type="submit"
                className="w-full text-white bg-lightGreen hover:bg-primary-700 focus:ring-4 focus:outline-none focus:ring-primary-300 font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-primary-600 dark:hover:bg-primary-700 dark:focus:ring-primary-800"
              >
                Sign up
              </button>

            </form>

          </div>
        </div>
      </div>
    </section>
  )
}

export default RegisterPage;