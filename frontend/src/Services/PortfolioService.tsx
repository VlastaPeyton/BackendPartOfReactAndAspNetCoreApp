import { PortfolioGetFromBackend } from "../Models/PortfolioGetFromBackend";
import { handleError } from "../ErrorHandler/ErrorHandler";
import { PortfolioAddDelete } from "../Models/PortfolioPost";
import apiBackendWithJWT from "../Axios/AxiosWithJWTForBackend"; 
import { getInMemoryToken } from "../Context/useAuthContext";

// Objasnjeno u AuthService.tsx i CommentService.tsx

const apiEndpoint = `${process.env.REACT_APP_BASE_BACKEND_API}portfolio/`;

// Potreban je JWT i zato koristim custom axios, jer svi endpoints osim Login i Register u .NET zahtevaju JWT zbog [Authenticate] ili User.GetUserName

export const portfolioAddApi = async (symbol: string) => {
    try{
        const token = getInMemoryToken(); // U Context, token nije skladisten u localStorage vise, jer nesigurno je, vec je u in-memory variable - pogledaj SPA Security Best Practictes.txt
        // Odgovor u AddPortfolio Endpoint je Created() i to fakticki bez oblika ali mora stojati neki oblik kao POTENCIJALNI return type ovde
        
        const response = await apiBackendWithJWT.post<PortfolioAddDelete>(apiEndpoint + `?symbol=${symbol}`, {})

        return response; 
        
    } catch (error){
        handleError(error);
    }
}

export const portfolioDeleteApi = async (symbol: string) => {
    try{
        const token = getInMemoryToken(); // U Context, token nije skladisten u localStorage vise, jer nesigurno je, vec je u in-memory variable - pogledaj SPA Security Best Practictes.txt
        // Odgovor u DeletePortfolio Endpoint je Ok() i to fakticki bez oblika ali mora stojati neki oblik ovde kao POTENCIJALNI return type ovde
        
        const result = await apiBackendWithJWT.delete<PortfolioAddDelete>(apiEndpoint + `?symbol=${symbol}`)

        return result; 
        
    } catch (error){
        handleError(error);
    }
}

export const portfolioGetApi = async () => {
    try{
        const token = getInMemoryToken(); 
        
        const result = await apiBackendWithJWT.get<PortfolioGetFromBackend[]>(apiEndpoint); 

        return result; 
        
    } catch (error){
        handleError(error);
    }
}