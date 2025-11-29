import axios from "axios";
import { CommentPost } from "../Models/CommentPost";
import { handleError } from "../ErrorHandler/ErrorHandler";
import { CommentGetFromBackend } from "../Models/CommentGetFromBackend";
import apiBackendWithJWT from "../Axios/AxiosWithJWTForBackend"; 
import { getInMemoryToken } from "../Context/useAuthContext";

// Objasnjeno u AuthService.tsx

const apiEndpoint = `${process.env.REACT_APP_BASE_BACKEND_API}comment/`;
  
/*  Koristim apiBackendWithJWT (custom axios), a ne obican axios, jer ove funkcije pozivaju protected Endpoints (koji zahtevaju JWT) + postoji RefreshEndpoint koji se poziva iz interceptor za apiBackendWithJWT

    U BE Endpoints, koje gadjaju ove funkcije pomocu axios, postoji cancellationToken, ali bez =default. Kada sam na page, koja koristi ove funkcije, i dok kad pokrenem neki Endpoint kliktanjem 
po njoj, ako user navigates away or closes app, BE ce autoamtski, zbog HTTP mehanizma, provaliti da sam prekinuo poziv, i prestace sa radom tako sto ce dodeliti RequestAborted vrednost za cancellationToken.
Da sam u Endpoint imao cancellationToken = default, u slucaju kad user navigates away or closes the app, u axios moram proslediti "signal:controller.signal" ali to nije Header or Body i jos uraditi neki cudni return 
koji ce aktivira controller.abort(). 
*/

// For StockComment.tsx. 
export const commentPostAPI = async (title: string, content: string, symbol: string) => {
    // try-catch zbog axios. Try se izvrsi ako StatusCode=2XX from backend. Catch se izvrsi ako StatusCode!=2XX from backend.
    try{ 
        // Potreban je JWT poslati iz Frontend to CommentController Create Endpoint, jer endpoint sadrzi User.GetUserName() koja zahteva JWT da se posalje from FE bez obzira da l .endpoint sadrzi [Authorize] ili ne
        const token = getInMemoryToken(); // U Context, token nije skladisten u localStorage vise, jer nesigurno je, vec je u in-memory variable - pogledaj SPA Security Best Practictes.txt
        // Moram navesti <CommentPost> kao POTENCIJALNI return ako backend Endpoint ne vrati error, jer uspesan CommentController Create metod vraca CommentDTO objekat sa poljima navedenim u CommentPost - objasnjeno u CommentPost.
        
        /*
         const response = await axios.post<CommentPost>(apiEndpoint + `${symbol}`, 
             // Mora `${symbol}, jer https://localhost:7045/api/comment/{symbol} Endpoint trazi symbol kroz URL path
             // Body of POST request jer ovaj Endpoint zahteva, pored symbol in URL, Title i Content ( tj CreateCommentRequestDTO) kroz Request Body.
             {
             Title: title,
             Content: content
             // Moraju ovaj redosled i imena polja kao u CreateCommentRequestDTO.
             },
             // Header of request jer "https://localhost:7045/api/comment/{symbol} u .NET ima ili [Authenticate] ili/i User.GetUserName(), pa zahteva JWT
             {
            headers:{
                 Authorization: `Bearer ${token}`
                 }
             }
         )
        */
        // Dok nisam ubacio RefreshToken Endpoint, mogo sam raditi kao iznad sa obicnim axios, ali sad ne moze, jer sam u apiBackendWithJWT dodao interceptors za RefreshToken + imam ugradjen JWT u custom axios
        const response = await apiBackendWithJWT.post<CommentPost>(apiEndpoint + `${symbol}`,
             {  
                Title: title,
                Content: content
             }
        )

        return response; // type = AxiosResponse<CommentPost> ako sve u redu ili undefined ako CommentController Create Endpoint vrati gresku jer onda u catch block ispod idem.
                         // Zbog ovakvog full response (AxiosResponse<CommentPost>) u StockComment moram da uradim response.data ako zelim da dohvatim payload.
                         
    } catch (error){
        handleError(error);
        // Nema return u catch blok, pa ako error, u try block response=undefined, pa u StockComment moramo da proverimo da li je result non-undefined pomocu if (result) ili result? 
    }
}

// For StockComment.tsx
export const commentsGetAPI = async (symbol: string) => {
    // try-catch zbog axios. Try se izvrsi ako StatusCode=2XX from backend. Catch se izvrsi ako StatusCode!=2XX from backend.
    try{ 
        // Potreban je JWT poslati iz Frontend to CommentController GetAll Endpoint, jer endpoint sadrzi User.GetUserName() koja zahteva JWT da se posalje from FE bez obzira da l .endpoint sadrzi [Authorize] ili ne
        const token = getInMemoryToken(); // U Context, token nije skladisten u localStorage vise, jer nesigurno je, vec je u in-memory variable - pogledaj SPA Security Best Practictes.txt
        // Moram navesti <CommentGetFromBackend[]> kao POTENCIJALNI return ako backend ne vrati error, jer uspesan CommentController GetAll metod u vraca commentDTOs niz ciji element(CommentDTO) ima polja navedena u CommentGetFromBackend - objasnjeno u CommentGetFromBackend.
        // Ako mi treba samo jedan element iz ovog Endpoint, moram <CommentGetFromBackend[]>, pa onda response.data[i] za zeljeni element.

        /* 
         const response = await axios.get<CommentGetFromBackend[]>(apiEndpoint + `?Symbol=${symbol}&IsDescending=true`,
             // U Axios GET Request, samo Header moze, a Body ne moze i zato ovaj Endpoint u .NET ima [FromQuery], a ne [FromBody]
             // Header of request jer " GET https://locahost:7045/api/comment/" u .NET ima ili [Authenticate] ili/i User.GetUserName(), pa zahteva JWT
             {
             headers:{
                 Authorization: `Bearer ${token}`
                 }
             }
         )
        */
        // Dok nisam ubacio RefreshToken Endpoint, mogo sam raditi kao iznad sa obicnim axios, ali sad ne moze, jer sam u apiBackendWithJWT dodao interceptors za RefreshToken + imam ugradjen JWT u custom axios
        const response = await apiBackendWithJWT.get<CommentGetFromBackend[]>(apiEndpoint + `?Symbol=${symbol}&IsDescending=true`,
            // U Axios GET Request, samo Header moze, a Body ne moze i zato ovaj Endpoint u .NET ima [FromQuery], a ne [FromBody]. Kroz Query Parameters (posle ? in URL) prosledjujem i moram pisati imena i redosled polja kao u CommentQueryObject klasi u BE.
            // Header of request jer " GET https://locahost:7045/api/comment/" u .NET ima ili [Authenticate] ili/i User.GetUserName(), pa zahteva JWT koji automatski je ubacen putem apiBackendWithJWT
        )

        return response; // type = AxiosResponse<CommentGetFromBackend[]> ili undefined ako CommentController GetAll Endpoint vrati gresku jer onda u catch blok ispod idem.
                         // Zbog ovakvog full response (AxiosResponse<CommentGetFromBackend[]>) u StockComment moram da uradim response.data ako zelim da dohvatim payload.

    } catch (error){
        handleError(error);
    }
    // Nema return u catch blok, pa ako error, response=undefined, pa u StockComment moramo da proverimo da li je result non-undefined pomocu if (result) ili result? 
}

// Poredi ovo sa api.tsx jer tamo se salje Request za Public API sa neta i drugacije je malo.