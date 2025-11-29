import { createContext } from "react";
import React from "react";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import { UserProfile } from "../Models/UserProfile";
import { registerAPI, loginAPI, forgotPasswordAPI, resetPasswordAPI } from "../Services/AuthService";

// Context je global state which allows me to share data across components (koje se nalaze inside <UserProvider> in App.tsx) without passing props manually to them 

type UserContextType = {
    user: UserProfile | null; 
    //token: string | null; // When you are NOT logged in this is NULL. Ne treba mi ovo, jer component nikad ne zavise direktno od njega, vec od ovih funkcija ispod navedenih.
    registerUser: (email: string, username: string, password: string) => void; // Register metoda u .NET zahteva ove parametre
    loginUser: (username: string, password: string) => void;                   // Login metoda u .NET zahteva ove parametre 
    logout: () => void;
    isLoggedIn: () => boolean;
    forgotPassword: (email: string) => void;                                   // ForgotPassword metoda u .NET zahteva ovaj parametar
    resetPassword: (newPassword: string, confirmPassword: string, email: string) => Promise<boolean>;
    // Ova imena polja mora da se poklope sa imenima u UserProvider + u metodama mora da se poklopi redosled argumenata kao u UserProvider.
    // Sve navedeno ovde, moram da posaljem kroz value u UserContext.Provider ispod na dnu koda.
}

type Props = {children: React.ReactNode} // Zbog components koje ce biti unutar <UserProvider> u App.tsx, jer React.ReactNode je 0,1 ili vise components koje mogu biti unutar <UserProvider>

const UserContext = createContext<UserContextType>({} as UserContextType); // Ovo u zagradi mora zbog TS da se ne buni. 
// createContext create empty box that holds shared data onih components koje ce u App.tsx biti unutar <UserProvider> + omoguci UserContext.Provider 

// Dobra praksa da JWT(Access Token) bude in-memory, a ne u localStorage - pogledaj SPA Security Best Practice.txt. Ovo koristim u axiosWithJWTForBackend interceptor
let inMemoryToken: string | null = null;
export const getInMemoryToken = () => inMemoryToken; // export obezbedi da mogu ovome da pristupim globalno
export const setInMemoryToken = (newToken: string | null) => {
    inMemoryToken = newToken;
};

// This Component mounts on app start up as it is placed in App.tsx
export const UserProvider = ({children} : Props) => {

    const navigate = useNavigate(); 
    const [token, setToken] = useState<string | null>(null); // null ako se nismo logovali jos 
    const [user, setUser] = useState<UserProfile | null>(null); // null ako se nismo logovali jos
    const [isReady, setIsReady] = useState<boolean>(false);

    useEffect(() => {
        const user = localStorage.getItem("user"); 
        //const token = localStorage.getItem("token");  ne treba mi vise jer nije dobro imati token u localStorage => i u ostalim fajlovima, ovo brisem i menjam sa getInMemoryToken
        
        // Ako user i token nisu null (svaki put nakon very first time login) onda izvrsi ovo ispod, a to se desi samo ako smo ostali ulogovani i startovali app opet, jer on 7dana pamti login valjda
        if (user && token) { 
            setUser(JSON.parse(user)); // UserProfile je objekat, ne string, pa JSON.parse mora, jer ispod je skladisteno kao JSON.Stringify(user) u localStorage, posto localStorage samo string prihvata
            setToken(token); // token je string, pa JSON.parse ne mora, jer ispod je skladisteno kao string u localStorage, jer localStorage samo string prihvata.
            inMemoryToken = token; 
        }               
        setIsReady(true); 
    }, []); /* Ne sme da bude [user, token], jer setUser/setToken je unutar useEffect i onda ce uci u infinite loop.
             useEffect se pokrece samo kad UserProvider is mounted (on app start up). */

    const registerUser = async (email: string, username: string, password: string) => {
        // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u registerAPI koji je Frontend, a ne zbog BE kad vrati StatusCode!=200, jer tu gresku u registerAPI sam uhvatio
        await registerAPI(email, username, password).then( (result) => { 
            // result.data je tipa UserProfileToken koji ima userName, emailAddress i token polja, samo ako backend vratio StatusCode=2XX, jer u registerAPI smo onda u try block otisli
            // result je tipa undefined, pa ne moze result.data, ako backend vratio error (StatusCode!=2XX), jer onda u loginAPI u catch blok smo otisli gde nema return 
            if ( result && result.data && typeof result.data.userName === 'string' && typeof result.data.emailAddress === 'string' && typeof result.data.token === 'string') 
                { 
                /* if result is not undefined tj if result is UserProfileToken.
                   if result.data is not undefined tj if result.data is object tipa UserProfileToken.
                   if result.data.userName/emailAddress/token is string jer UserProfileToken ima userName/emailAddress/token:string
        
                
                 U BE, JWT nije poslat ka FE kroz Cookie, vec obicnim (non-XSS secured putem), i zato ga moram skladistiti u localStorage. To nije dobra praksa, jer napadac ga moze naci. Bolje je kroz Cookie.
                localStorage.setItem("token", result.data.token); -  skladisteno kao string, jer localStorage samo string prihvata. Ne treba mi vise jer nije dobro imati token u localStorage, vec u varijabli - pogledaj SPA Security Best Practices.txt
                
                result? - jer result moze biti i undefined ako backend vratio error, pa u registerAPI otisao u catch blok
                result?.data - jer registerAPI vraca AxiosResponse<UserProfileToken>, a nas payload(tipa UserProfileToken) je data.
                result?.data.token/userName/emailAddress - jer payload ima userName, emailAddress i token polja
                Ove upitnike vise ne koristim, jer sam u if iznad stavio sve uslove.
                */
                
                // user mora imati ISTA imena i redosled polja kao UserProfile inace nece hteti setUser(user) zbog useState<UserProfile...>
                const user = {
                    userName: result.data.userName, 
                    emailAddress: result.data.emailAddress 
                }

                localStorage.setItem("user", JSON.stringify(user)); // Moram UserProfile objekat da pretvorim u string, jer localStorage samo string prihvata.
                setToken(result.data.token);  // U FE uvek mora da sacuva JWT from BE kako bi u Authorization Header ga slao to BE za Endpoints koji zahtevaju to. React re-renders this component zbog set.
                setUser(user);                // React re-renders this component zbog set.
                // Posto su ovde 2 uzastopna set, a svaki set re-renders app, react odradi oba, pa tek re-renders component.
                inMemoryToken = result.data.token; // Necu vise koristiti localStorage za JWT jer je nesigurno, vec in-memory varijable - pogledaj SPA Security Best Practices.txt
                toast.success("Register successfull"); // Prikaze mali pop-up u gornji desni ugao ekrana 
                navigate("/search"); // Baca nas na SearcPage, jer u Routes.tsx definisano da SearchPage je u "/search" route.
            }
            // Nema else, jer nema potrebe obzirom da u registerAPI imamo handleError u catch (with toastify). Da to nemam, ovde bih else imao da toast.warning prikaze nesto.

        }).catch((err) => toast.warn("Frontend error in registerAPI")); // Prikaze mali pop-up u gornji desni ugao ekrana ako bude frontend greska u registerAPI (ne u backendu)
    }

    const loginUser = async (username: string, password: string) => {
        // Then-catch isto kao da sam pisao try-catch. Catch se radi ako dodje do greske u loginAPI koji je Frontend, a ne zbog BE StatusCode!=200, jer tu gresku u loginAPI sam uhvatio
        await loginAPI(username, password).then((result) => { 
            // result.data je tipa UserProfileToken koji ima userName, emailAddress i token polja, samo ako backend vratio StatusCode=2xx, jer u loginAPI smo u try block otisli
            // result je tipa undefined, pa ne moze result.data, ako backend vratio error (StatusCode!=2XX), jer onda u loginAPI u catch blok smo otisli gde nema return 
            if ( result && result.data && typeof result.data.userName === 'string' && typeof result.data.emailAddress === 'string' && typeof result.data.token === 'string') 
              { 
                // Objasnjeno iznad u registerUser metodi

                const user = {
                    userName: result.data.userName,
                    emailAddress: result.data.emailAddress
                }
                localStorage.setItem("user", JSON.stringify(user)); 
                setToken(result.data.token); 
                setUser(user); 
                inMemoryToken = result.data.token; 
                toast.success("Login successfull");
                navigate("/search"); 
            }
            // Nema else, jer nema potrebe obzirom da u loginAPI imamo handleError u catch (with toastify). Da to nemam, ovde bih else imao da toast.warning prikaze nesto.

        }).catch((err) => toast.warn("Frontend error in loginAPI"));  // Prikaze mali pop-up u gornji desni ugao ekrana ako bude frontend greska neka u loginAPI (ne u backendu)
    }

    const isLoggedIn = () => {
        return !!user; // Bolje ovo nego "if user === null", jer ovim pokrivam uslove da user nije null/undefined/false/0... Mada moj user ima UserProfile | null, pa moze "if user !=== null".
    }

    const logout = () => {
        //localStorage.removeItem("token"); necu vise koristiti localStorage za JWT jer nesigurno - pogledaj SPA Security Best Practices.txt
        localStorage.removeItem("user"); 
        setUser(null);
        setToken("");
        inMemoryToken = null; // Necu vise koristiti localStorage za JWT jer nesigurno 
        navigate("/"); // Baca me na homepage after logout
    }

    // Objasnjenje za ovu funkciju je isto kao za sve iznad. 
    const forgotPassword = async (email: string) => {
        await forgotPasswordAPI(email).then((result) => {
            if (result && typeof result.data === 'string'){
                toast.success("Email mozda postoji ili ne u bazi, reset password link je poslat na vasu adresu."); // Ovo samo za Dev sluzi, dok za Prod brise jer necu da me odaje
            }
        }).catch((err) => toast.warn("Frontend error in forgotPasswordAPI"));
    }

    // Objasnjenje kao iznad za ostale funkcije
    const resetPassword = async (newPassword: string, resetPasswordToken: string, email: string) => {
        try{
            const result = await resetPasswordAPI(newPassword, resetPasswordToken, email);
            if (result && typeof result.data === 'string') {
                toast.success("Uspesno ste resetovali password. Bravo majmune");
                return true; // Success
            }
            return false; // Failed (result= undefined in resetPasswordAPI)
        } catch (err){
            toast.warn("Frontend error in resetPasswordAPI");
            return false;
        }
    }

    return (
        /* 
         UserContext.Provider allows me to share data (loginUser, user, logout, isLoggedIn, registerUser) between components without having to manually pass props down the component tree. 
         Ovo se odnosi u App.tsx na children components (<Navbar>, <Outlet> i <ToastContainer>) unutar <UserProvider>.
         UserContext = createContext<UserContextType>, a UserContextType ima loginUser, user, logout itd. polja i zato moramo u value argumentu sve da ih navedem. 
         isReady ? children : null znaci da ako isReady=true renderovace u App.tsx children components unutar <UserProvider>, a children samo preko useAuth mogu da pristupe svemu iz UserContextType, stoga 
        moram definisati, van UserProvider, useAuth custom Hook. U child component unutar <UserProvider> u App.tsx  da bih pristupio necemu iz UserContext moram bas to ime da navedem (npr: loginUser)
        */
        <UserContext.Provider value={{ loginUser, user, logout, isLoggedIn, registerUser, forgotPassword, resetPassword}}> {isReady ? children : null}</UserContext.Provider>
        // Prosledim vrednosti za polja u UserContextType zbog UserContext = createContext<UserContextType> i onda mogu preko useAuth custom Hook da pristupim ovome bez ponavljanja koda u children componentama unutar <UserProvider> u App.tsx
    )
}

// Custom Hook, jer koristi bar 1 built-in Hook (useContext). Gives access to everything stored in UserContext to children components (npr: const {loginUser} = useAuth() in child Component).
export const useAuth = () => React.useContext(UserContext); 

