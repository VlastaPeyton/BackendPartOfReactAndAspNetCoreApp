import axios from "axios";
import { CompanyBalanceSheet, CompanyCashFlow, CompanyIncomeStatement, CompanyKeyMetrics, CompanyProfile, CompanySearch, CompanyTenK } from "../Types/company"; // Jer nema default export u company.d.ts, pa mora {}
import { SearchResponse } from "../Models/SearchResponse";

/*  
  Axios korisitm umesto Fetch, jer je bolji i more user-friendly than Fetch.

  Obzirom da ovde pozivam FinancialModelingPrep API koji je public na netu tj nisam ga ja pravio, u catch bloku ne koristim handleError jer je ta funkcija pravljena za moj licni backend.
  Zbog FMP API public, ne saljem JWT u pozivima.
  Ne znam u kom jeziko je FMP API pisan, ali svakako nema potrebe za slanjem controller.signal kao cancellationToken da sluzi - ovo je izazov za moje BE Endpoint pa je tamo, a i u Services folderu, detaljnije objasnjeno zasto cancellationToken nema "=default"

  Kada u search ukucam npr "aap" dobijem sve firme koje u imenu ticker imaju "aap", ali firma moze biti Stock ili ETF. Ovaj API ne garantuje da je Stock, vec moze biti i ETF,
zato ako ne mogu da dodam npr AAPU u my portfolio jer Endpoint, koga portfolioAddApi funkcija gadja, koristi drugaciji FMP API koji samo Stock pretrazuje. Sto znaci
da AAPU nije Stock nego ETF pa necemo moci da ga dodamo u portfolio.

  Koristim obican axios, jer mi ne treba apiBackendWithJWT (custom axios), jer pozivam FMP public endpoints koji ne traze JWT.
  Axios GET request ne moze imati Body, vec samo Header (u koji ide JWT ako mi zatreba).
*/

// For SearchPage.tsx
export const searchCompanies = async (query: string) => {
    // try-catch zbog axios. Try se izvrsi ako StatusCode=2XX from backend. Catch se izvrsi ako StatusCode!=2XX from backend.
    try{
        // Nije potrebno JWT slati, jer ovo je API sa neta i public je. 
        // Moram navesti <SearchResponse> kao POTENCIJALNI return ako backend ne vrati error, jer uspesan ovaj Endpoint sa FinancialModelingPrep vraca niz ComapnySearch tipa, a meni trebaju sva polja iz CompanySearch - objasnjeno u SearchResponse. 
        // Ako mi treba samo jedan element, moram opet <SearchResponse>, pa onda response.data[i] za zelejeni element.
        const response = await axios.get<SearchResponse>(`https://financialmodelingprep.com/api/v3/search-ticker?query=${query}&limit=10&exchange=NASDAQ&apikey=${process.env.REACT_APP_API_KEY}`); 
        //const response = await axios.get<CompanySearch[]>(`https://financialmodelingprep.com/api/v3/search-ticker?query=${query}&limit=10&exchange=NASDAQ&apikey=${process.env.REACT_APP_API_KEY}`);  Ovo je lakse jer mi trebaju sva polja iz CompanySearch
        // U Axios GET Request, samo Header moze, a Body ne moze. Nemam header, jer mi header tj JWT ne treba, jer ovo je public API sa neta.
        
        return response; // Full response AxiosResponse<SearchResponse> tipa ako bude dobro ili undefinded ako FMP Endpoint vrati error jer onda idem u catch block.
                         // Ostavio sam full response ovako, a ne response.data => u SearchPage.tsx onSearchSubmit uradio result.data
    
    } catch (err){
        /* Obzirom da ovde gadjam FinancialModelingPrep API koji je public na netu tj nisam ga ja pravio, u catch bloku ne koristim handleError jer je ta funkcija pravljena za moj licni backend. 
        vec koristim obican isAxiosError koji moze biti Network/HTTP/Unexpected . */
        
        // HTTP/Network error imaju err.message
        if (axios.isAxiosError(err)){  
            console.log("Err message:", err.message)
            return err.message;       // String - bitno zbog SearchPage onSearchSubmit kad handlujem result
        } 
        // Unexpected error koji nema err.message
        else{                         
            console.log("Unexpected error", err);
            return "Unexpected error";  // String - bitno zbog SearchPage onSearchSubmit kad handlujem result
        }
        // Zbog return string u catch, u SearchPage.tsx onSearchSubmit moram da proverim typeof(return) == "string", a zbog try block mora provera Array.isArray(return.data)... 
    }
};

// For CompanyPage.tsx
export const getCompanyProfile = async (query: string) => {
    // Objasnjeno iznad
    try{
        const response = await axios.get<CompanyProfile[]>(`https://financialmodelingprep.com/api/v3/profile/${query}?apikey=${process.env.REACT_APP_API_KEY}`) 

        return response; // Full response AxiosResponse<CompanyProfile[]> tipa ako sve u redu ili undefinded ako FMP Endpoint vrati error jer onda idem u catch block. 
                         // Ostavio sam full response ovako => u CompanyPage morati response.data
    } catch (err) {
        //Objasnjeno iznad 

        if (axios.isAxiosError(err)){ 
            console.log("Err message:", err.message)
        } 
        else{                        
            console.log("Unexpected error", err);
        }
        // Nema return u catch i zato u CompanyPage.tsx getProfileClient mora result?.data[i] (ili veliki if kao u useAuthContext.tsx loginUser), jer ako bude error onda response=undefined u try block.
    }
}

// For CompanyProfile.tsx
export const getKeyMetrics = async (query: string) => {
    // Objasnjeno iznad
    try{
        const response = await axios.get<CompanyKeyMetrics[]>(`https://financialmodelingprep.com/api/v3/key-metrics-ttm/${query}?apikey=${process.env.REACT_APP_API_KEY}`)

        return response; // Full response AxiosResponse<CompanyKeyMetrics[]> tipa ako bude u redu ili undefinded ako backend Endpoint vrati error jer onda idem u catch block.
                         // Ostavio sam full response => u CompanyProfile.tsx video kako se obradjuje ovakav slucaj kad ovde nije return response.data
    } catch (err) {
        // Objasnjeno iznad

        if (axios.isAxiosError(err)){  
            console.log("Err message:", err.message)
        } 
        else{                        
            console.log("Unexpected error", err);
        }
        //Nema return u catch i zato u CompanyProfile.tsx getCompanyKeyMetrics mora result?.data[i] (ili velik if kao u useAuthContext.tsx loginUser), jer ako bude error onda response=undefined u try block.
    }
}

// For IncomeStatemen.tsx
export const getIncomeStatement = async (query: string) => {
    // Objasnjeno iznad
    try{
        const response = await axios.get<CompanyIncomeStatement[]>(`https://financialmodelingprep.com/api/v3/income-statement/${query}?limit=40&apikey=${process.env.REACT_APP_API_KEY}`)

        return response; // Full response AxiosResponse<CompanyIncomeStatement[]> tipa ako bude u redu ili undefinded ako backend Endpoint vrati error jer onda idem u catch block. 
                         // Ostavio sam full response => u IncomeStatement.tsx video kako se obradjuje ovakav slucaj kad ovde nije return response.data ovde
    } catch (err) {
        // Objasnjeno iznad

        if (axios.isAxiosError(err)){ 
            console.log("Err message:", err.message)
        } 
        else{                        
            console.log("Unexpected error", err);
        }
        //Nema return u catch i zato u IncomeStatement.tsx fetchIncomeStatement mora result?.data (ili veliki if kao u useAuthContext.tsx loginUser), jer ako bude error onda response=undefined u try block.
    }
}

// For BalanceSheet.tsx
export const getBalanceSheet = async (query: string) => {
    // Objasnjeno iznad
    try{
        const response = await axios.get<CompanyBalanceSheet[]>(`https://financialmodelingprep.com/api/v3/balance-sheet-statement/${query}?limit=40&apikey=${process.env.REACT_APP_API_KEY}`)

        return response; // Full response AxiosResponse<CompanyBalanceSheet[]> tipa ako bude u redu ili undefinded ako backend Endpoint vrati error jer onda idem u catch block. 
                         // Ostavio sam namerno bez response.data ovde, da bih BalanceSheet.tsx video kako se obradjuje ovakav slucaj kad ovde nije return response.data
    } catch (err) {
        // Objasnjeno iznad

        if (axios.isAxiosError(err)){ 
            console.log("Err message:", err.message)
        } 
        else{                        
            console.log("Unexpected error", err);
        }
        //Nema return u catch i zato u BalanceSheet.tsx fethBalanceSheet mora result?.data (ili veliki if kao u useAuthContext.tsx loginUser), jer ako bude error onda response=undefined u try-block.
    }
}

// For Cashflow.tsx
export const getCashflow = async (query: string) => {
    // Objasnjeno iznad
    try{
        const response = await axios.get<CompanyCashFlow[]>(`https://financialmodelingprep.com/api/v3/cash-flow-statement/${query}?limit=40&apikey=${process.env.REACT_APP_API_KEY}`)

        return response; // Full response AxiosResponse<CompanyCashFlow[]> tipa ako bude u redu ili undefinded ako FMP Endpoint vrati error jer onda idem u catch block. 
                         // Ostavio sam namerno bez response.data ovde, da bih Cashflow.tsx video kako se obradjuje ovakav slucaj kad nije return response.data
    } catch (err) {
        // Objasnjeno iznad

        if (axios.isAxiosError(err)){ 
            console.log("Err message:", err.message)
        } 
        else{                        
            console.log("Unexpected error", err);
        }
        //Nema return u catch i zato u Cashflow.tsx fetCashFlow mora result?.data (ili veliki if kao u useAuthContext.tsx loginUser), jer ako bude error onda response=undefined u try-block.
    }
}

// For u TenK.tsx - godisnji 10-K izvestaj koji US Sec trazi
export const getTenK = async (query: string) => {
    // Objasnjeno iznad
    try{
        const response = await axios.get<CompanyTenK[]>(`https://financialmodelingprep.com/api/v3/sec_filings/${query}?type=10-k&page=0&apikey=${process.env.REACT_APP_API_KEY}`)

        return response; // Full response AxiosResponse<CompanyTenK[]> tipa ili undefined ako backend Endpoint vrati error jer onda idem u catch block. 
                         // Ostavio sam namerno bez response.data ovde, da bih ComparableCompany.tsx video kako se obradjuje ovakav slucaj kad nije ovde return response.data
    } catch (err) {
        // Objasnjeno iznad

        if (axios.isAxiosError(err)){ 
            console.log("Err message:", err.message)
        } 
        else{                     
            console.log("Unexpected error", err);
        }
        //Nema return u catch i zato u TenK.tsx fetchTenK mora result?.data (ili veliki if kao u useAuthContext.tsx loginUser), jer ako bude error onda response=undefined u try-block.
    }
}

