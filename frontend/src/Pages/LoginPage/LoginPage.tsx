import * as Yup from "yup";
import { useAuth } from '../../Context/useAuthContext';
import { useForm } from 'react-hook-form';
import { yupResolver} from "@hookform/resolvers/yup";
import { Link } from "react-router-dom";

type Props = {}

type LoginFormInputs = {
    userName: string;
    password: string;
}

// Yup je za validaciju. Za ovo postoji validacija i u BE, jer to je prava validacija koja je najbitnija za login.
const validation = Yup.object().shape({
    userName: Yup.string().required("Username is required"), // Yup.string() - jer userName:string in LoginFormInputs
    password: Yup.string().required("Password is required")
    // Mora ista imena i redosled polja kao u LoginFormInputs
});

// Ovo je child Component of <UserProvider>, jer u App.tsx je smestena inside <UserProvider> tj unutar <Outlet>, jer u Routes.tsx <LoginPage> je child route za <App>
const LoginPage = (props: Props) => {

    const {loginUser} = useAuth();  // loginUser mora, jer se tako zove unutar useAuthContext.tsx. Pogledaj useAuthContext.tsx tamo objasnjeno sve
    const {
        // Ova 3 objekta useForm vraca i zato moraju ovako da se zovu, jer su im to built-in imena.
        register,      // Zbog ...register(userName/password) u html mi je potrebno ovo
        handleSubmit,  /* Zbog onSubmit={handleSubmit(handleLogin)} u html mi je potrebno ovo. Prevents default automatski. Uzme sve iz forme u obliku LoginFormInput i prosledi u handleLogin
                      Takodje, validira formu using yurResolver. Ako invalid forma, napravi formState.errors. Ako valid, napravi LoginFormInputs objekat sa poljima iz forme i pozove handleLogin.*/
        formState: {errors}, // Destruktuira formState koji sadrzi vise polja, pa mi samo treba errors objekat koji sadrzi userName i password polja. Ovo je isto kao formState.errors. Moze errors.userName/password.message
    } = useForm<LoginFormInputs>({resolver: yupResolver(validation)}) // Koristim yupResolver za validaciju of React Hook Form 

    // Ne treba await loginUser ovde jer logicki mi ne treba
    const handleLogin = (loginForm: LoginFormInputs) => {
        loginUser(loginForm.userName, loginForm.password); // loginUser zahteva ovaj redosled argumenata.
    }

    return (
    <section className="bg-gray-50 dark:bg-gray-900">

      <div className="flex flex-col items-center justify-center px-6 py-8 mx-auto md:h-screen lg:py-0">
        
        <div className="w-full bg-white rounded-lg shadow dark:border md:mb-20 sm:max-w-md xl:p-0 dark:bg-gray-800 dark:border-gray-700">
          
          <div className="p-6 space-y-4 md:space-y-6 sm:p-8">
            
            <h1 className="text-xl font-bold leading-tight tracking-tight text-gray-900 md:text-2xl dark:text-white">
              Sign in to your account
            </h1>
            
            <form className="space-y-4 md:space-y-6" onSubmit={handleSubmit(handleLogin)}> {/* Ne treba (e) => handleSubmit jer on sam ima prevent default u sebi + on sam izdvoji sve iz forme i prosledi u handlelogin */}
              
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
                <Link
                  to="/forgot-password"
                  className="text-sm font-medium text-primary-600 hover:underline dark:text-primary-500"
                >
                  Forgot password?
                </Link>
              </div>

              {/* type=submit => Kliknuti na Sign in aktivira handleLogin gore naveden u onSubmit u zaglavlju <form...> */}
              <button
                type="submit" 
                className="w-full text-white bg-lightGreen hover:bg-primary-700 focus:ring-4 focus:outline-none focus:ring-primary-300 font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-primary-600 dark:hover:bg-primary-700 dark:focus:ring-primary-800"
              >
                Sign in
              </button>

              <p className="text-sm font-light text-gray-500 dark:text-gray-400">
                Don’t have an account yet?{" "}
                <Link to="/register" className="font-medium text-primary-600 hover:underline dark:text-primary-500"
                >
                  Sign up
                </Link>
              </p>

            </form>

          </div>
        </div>
      </div>
    </section>
    )
}

export default LoginPage;
// "/forgot-password", jer to je route za ForgotPasswordPage, koja nije child route of current route http://localhost:3000/login (LoginPage) 

/* U Hero.tsx sam objasnio da <section> mora id imati, ali i nije moranje ako nemam <a href>...
  <section className="bg-gray-50 dark:bg-gray-900"> : 
     bg-gray-50 = za Light mode u Chrome pozadina je ove boje.
     dark:bg-gray-900 = za Dark mode u Chrome pozadina je ove boje.
    
  <div className="flex flex-col items-center justify-center px-6 py-8 mx-auto md:h-screen lg:py-0"> za :
     flex = Mora zbog flex-col
     flex-col = Stack children verticali (Username i Password polja vertikalna)
     items-center =  Po sred ovog <div> da budu Username i Password polja.
     
  <label htmlFor="username"> povezano sa <input id="username"> 

  register("userName") daje objekat tipa 
                                        {
                                        name: "userName",
                                        onChange: function,
                                        onBlur: function,
                                        ref: function
                                      }
  a onda 
  <input 
     ....,
     {...register("username")}
  />   
  daje 
  <input
    ....,
    name="userName"
    onChange={...}
    onBlur={...}
    ref={...}
  />

*/