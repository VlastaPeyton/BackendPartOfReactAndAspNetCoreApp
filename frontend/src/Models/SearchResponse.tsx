import { CompanySearch } from "../Types/company"

export type SearchResponse = {
    data: CompanySearch[]
}
/* Uspesan https://financialmodelingprep.com/api/v3/search-ticker?query=${query}&limit=10&exchange=NASDAQ&apikey=${process.env.REACT_APP_API_KEY} 
vraca niz objekata CompanySearch tipa, a nama treba svako polje iz CompanySearch zato je SearchResponse ovakav. Da su mi trebala samo odredjena polja iz CompanySearch, 
napravio bih klasu CompanySearchPotrebaPolja i u njoj naveo "istim" imenom naveo imena zeljenih polja kao u CompanySearch. 
  
   Nisam ovo morao praviti ovaj fajl, vec mogo u searchCompanies u api.tsx, uraditi kao u commentsGetAPI u CommentService.tsx jer lakse je. */