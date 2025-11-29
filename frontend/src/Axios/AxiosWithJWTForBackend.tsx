import axios from "axios";
import { getInMemoryToken, setInMemoryToken } from "../Context/useAuthContext";
import {jwtDecode} from "jwt-decode";
import { toast } from "react-toastify";
import { NewAccessToken } from "../Models/NewAccessToken";
 

/* BE endpoint moze biti protected ([Authenticate]) i/ili sadrzati User.GetUserName (jer koristim ASP.NET Identity). U oba slucaja FE mora poslati JWT, ali ovde su bitni protected endpoints, 
jer se oni odnose na kreiranje, razmenu i skladistenje JWT i Refresh Token -pogledaj SPA Security Best Practices.txt
Create custom axios instance koju koristim za svaki protected endpoint([Authenticate]) jer on zahteva JWT, a to su endpoints za CommentService.tsx i PortfolioService.tsx
*/
const apiBackendWithJWT = axios.create({
    baseURL: process.env.REACT_APP_BASE_BACKEND_API, // Bolja praksa, nego da hardcodujem vrednost https://localhost:7045/api/.  Mora baseURL se bas ovako zvati u axios.create
    withCredentials: true // Required to send Refresh Token via cookie to BE
})

// Create a separate axios instance for refreshing token to avoid interceptor deadlock as this instance does not have interceptors attached to it 
const axiosRefreshToken = axios.create({
    baseURL: process.env.REACT_APP_BASE_BACKEND_API,
    withCredentials: true // Required to send Refresh Token via cookie to BE
});

/* Ako je vise requests poslato to protected endpoint, a Acces Token is about to expire (jer svi requests koriste isti JWT), 
osiguravam da samo jedan Request pozove RefreshToken Endpoint, dok ostalih 9 cekaju (subscribeTokenRefresh) da FE dobije novi JWT i kad ga dobiju, nastavljaju
sa novim tokenom. */
type TokenCallback = (token: string) => void; // U React, funkcija moze biti type. Ovo je pandan "delegate void TokenCallBack(string token) u C#". Ovo je samo potpis funkcije, a telo definisem u interceptoru u subscribeTokenRefresh liniji
let isRefreshing = false;
let refreshSubscribers: TokenCallback[] = [];
function subscribeTokenRefresh(callback: TokenCallback): void {
    refreshSubscribers.push(callback); // push add one or more elems(TokenCallback functions) to end of refreshSubscribers array
}
function onRefreshed(newToken: string): void {
    refreshSubscribers.forEach(callback => callback(newToken)); // Svaku funkciju iz niza poziva sa newToken argumentom
    refreshSubscribers = [];
}

// Function to refresh the access token (which is gonna be called by only first request made to protected Endpoint)
async function refreshAccessToken() {

    console.log("Calling refresh...");

    try {
         // Use the SAME axios instance to ensure cookies are sent properly - LOSE
         // Use DIFFERENT axios instance da ne bude self-intercept deadlock - objasnjeno iznad 
        const response = await axiosRefreshToken.get<NewAccessToken>( // NewAccessToken je POTENCIJALNI rezultat ako BE vrati StatusCode=2XX
            'account/refresh-token', // Relative URL since we have baseURL for axiosRefreshToken
            {
                withCredentials: true // Explicitly ensure cookies (which carry refresh token) are sent by the browser
            }
        );
        
        const newToken = response.data.accessToken; // jer NewAccessToken ima ovo polje

        console.log(`New access token ${newToken}`);
        setInMemoryToken(newToken); 
        toast.success("New token set");

        return newToken;

    // Ako u BE dodje do greske 
    } catch (error) {
        console.warn("Token refresh failed", error);  
        toast.warn("Nije refreshovan token");
        setInMemoryToken(null);
        
        return null;
    }
}

/* Axios ima Request i Response interceptor koji je zajednicki za sve Request koji se posalju ka BE. Stoga, kad ga kodiram, moram paziti na "race condition" of Request.
   Nije napravljen interceptors za obican axios koji koristim u api.tsx za FinancialModelingPrep, vec za BE pozive tj za apiBackendWithJWT axios custom instance. 
   Request interceptor pokrece se pre nego sto request poslat to BE. 
   Response interceptor pokrece se nakon sto BE vrati odgovor. 
*/

// Ako imam 10 request i svima naravno isti token istice uskoro, i svaki request ide kroz request interceptor pre nego sto biva poslat u BE
apiBackendWithJWT.interceptors.request.use(
    /* Arrow function called before Request is sent. Config is automatically passed to arrow function by the Axios interceptor and it's the request configuration object that Axios creates before sending the HTTP request.
    Ovime modifikujem config objekat, pre nego sto Axios i posalje Request to BE.*/
    async (config) => {
        config.withCredentials = true; // Explicitly ensure this jer ako ne stavim ,due to Promises oce se izgubi ovo iako definisano u kreiranju apiBackendWithJWT
        let token = getInMemoryToken();
        if (token){
            try{
                const decoded = jwtDecode(token);
                const now = Date.now() / 1000;

                if (decoded.exp && typeof decoded.exp === 'number'){
                    //const timeUntilExpiry = decoded.exp ? decoded.exp - now : null; // Mora ovako jer decoded.exp moze biti undefined
                    const timeUntilExpiry = decoded.exp - now; 
                    console.log(`Token expiers in ${timeUntilExpiry}`);

                    if( timeUntilExpiry <= 30){
                        // Kada prvi Request naidje, isRefreshing===false, i ide u else, ali ostali request udju ovde jer je njima isRefreshing===true
                        if (isRefreshing){
                            // Ostali Requests cekaju until resolve(config) (zbog Promise), dok se onRefreshed(newToken) ne zavrsi i tek tada nastavljaju sa azuriranim tokenom
                            // Promise is same as Task in ASP.NET Core. Promise blokira slanje request dok se ne pozove resolve ili reject i axios ceka Promise da zavrsi tj dok ne stignu novi JWT i refresh token
                            return new Promise((resolve, reject) => { // interceptor vraca Promise u axios, a axios interno resolvuje ili rejectuje Promise tj izvadi payload tj config pomocu promise.then
                                // i svaki od njih pozove subscribeTokenRefresh kome prosledi telo funkcije koju jos uvek nije aktivirao
                                subscribeTokenRefresh((newToken: string) =>{ //newToken iz onRefreshed(newToken)
                                    config.withCredentials = true; // When you use new Promise() in your interceptors, you need to explicitly preserve withCredentials
                                    config.headers.Authorization = `Bearer ${newToken}`;
                                    resolve(config); // Unblocks paused Requests and sends it with newToken. Resolve je built-in funkcija koja oznacava uspesan zavrsetak Promise
                                });
                            });
                        }
                        else{
                            // Only first request enter here and start refresh process as isRefreshing=false in that moment jer isRefreshing=false inicijalno
                            isRefreshing = true;
                            // Try-catch zbog await axios
                            try{
                                const newToken = await refreshAccessToken();
                                if (newToken){
                                    token = newToken;
                                    onRefreshed(newToken); // subscribeTokenRefresh je napunjen lambda funkcijama svih Requests koji su cekali, i svaka se sada poziva sa newToken argumentom
                                }
                                else{
                                    // Refresh failed, let reqeust proceed with old expired token as Response interceptor will handle 401
                                    console.log("Refresh token failed in request interceptor, proceeding with old token");
                                }
                            } catch(error){
                                toast.warn("Failed to refresh token in request interceptor");
                                // Token refresh failed, let the request proceed with old token
                                // The response interceptor will handle the 401 error
                            } finally{
                                isRefreshing = false;
                            }
                        }
                    }
                }
            } catch (error){
                toast.warn("error decoding token");
                setInMemoryToken(null);
            }
        }
        // Set authorization header with current token (could be refreshed or original)
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        // DOUBLE CHECK: Always ensure withCredentials is true
        config.withCredentials = true;

        return config; // interceptor ovo vrati u axios samo ako JWT nije blizu isteka (>30s ostalo), u suprotnom vrati iz Promise gore
    },
    // Arrow function called if error is present, while modifying config object, before Request is sent. Error is automatically passed to arrow function by the Axios interceptor
    (error) =>{
        return Promise.reject(error);
    }

)

/* Add Response Interceptor to handle 401 errors as fallback, jer moze se desi da FE nije poslao nijedan Request (user nije kliknuo nista) a token expired i onda request interceptor izbacice gresku jer protected Endpoints ne mogu pozvati bez tokena
 Npr ako sam poslao 10 Requests, a token vec istekao, svih 10 ce da dobije 401 status pre toga, ali samo prvi ce da pozove refreshAccessToken,a ostalih 9 cekaju da se dobije novi token*/
apiBackendWithJWT.interceptors.response.use(
    // Success handler after axios finishes successfuly. Response is automatically passed by Axios interceptor.
    (response) => response,  // response is returned to the original caller of request (axios instance). Interceptor ne modifikuje response vec samo ga vrati tj ode u then block of axios call jer je sve u redu.
    // Error handler. If Response is 401 tj ako axios.interceptors.request dobije 401 jer je token expired. Error is automatically passed by the Axios interceptor
    async (error) => {

        const originalRequest = error.config;
        
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true; // Prevent infinite loops. Request sent with access token expired -> 401 -> retried request -> if fails, dont try again
            originalRequest.withCredentials = true;  //  Preserve withCredentials in original request iz istog razloga kao request interceptor jer dealing wiht promise oce da izbrise withCredential
            
            // Ako isRefreshing=true 
            if (isRefreshing) {
                // If already refreshing (1st Request activated refreshAccessToken), pause all waiting requests until the new token is set
                // Promise blokira slanje request dok se ne pozove resolve ili reject i axios ceka Promise da zavrsi tj dok ne stignu novi JWT i refresh token
                return new Promise((resolve, reject) => { // interceptor vraca Promise u axios, a axios interno izvadi payload tj apiBackendWithJWT(originalRequest) iz promise pomocu promise.then 
                    subscribeTokenRefresh((newToken: string) => {
                        // CRITICAL: Preserve withCredentials when retrying .When you use new Promise() in your interceptors, you need to explicitly preserve withCredentials
                        originalRequest.withCredentials = true;
                        originalRequest.headers.Authorization = `Bearer ${newToken}`;
                        resolve(apiBackendWithJWT(originalRequest)); // Unblock paused reqeust and send it again with new token. Resolve je built-in funkcija koja oznacava uspesan zavrsetak Promise
                    });
                });
            }
            
            isRefreshing = true;
            // Zbog axios await mora try-catch
            try {
                const newToken = await refreshAccessToken();
                if (newToken) {
                    originalRequest.withCredentials = true;  // CRITICAL: Preserve withCredentials when retrying
                    originalRequest.headers.Authorization = `Bearer ${newToken}`;
                    onRefreshed(newToken);  // subscribeTokenRefresh je napunjen lambda funkcijama svih Requests koji su cekali, i svaka se sada poziva sa newToken argumentom
                    return apiBackendWithJWT(originalRequest); // Retry the original request
                   
                }
            } catch (refreshError) {
                toast.warn("token refresh failed in interceptor");
                // Redirect to login handled in refreshAccessToken function
            } finally {
                isRefreshing = false;
            }
        }
        
        return Promise.reject(error); // Ako nije vratio Promise gore, znaci da nije 401 ili je _retry=true, pa vrati gresku u axios call koji je pozvao apiBackendWithJWT
    }
);

// Response interceptor for 401 as fallback for request interceptor 
export default apiBackendWithJWT; 