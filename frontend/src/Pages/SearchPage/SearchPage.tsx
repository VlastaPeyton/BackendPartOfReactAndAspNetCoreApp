import { SyntheticEvent, useEffect, useState, useCallback } from 'react'
import { CompanySearch } from '../../Types/company';
import { searchCompanies } from '../../Axios/api';
import Hero from '../../Components/Hero/Hero';
import Search from '../../Components/Search/Search';
import ListPortfolio from '../../Components/Portfolio/ListPortfolio/ListPortfolio';
import CardList from '../../Components/CardList/CardList';
import { PortfolioGetFromBackend } from '../../Models/PortfolioGetFromBackend';
import { portfolioAddApi, portfolioDeleteApi, portfolioGetApi } from '../../Services/PortfolioService';
import { toast } from 'react-toastify';

type Props = {}

const SearchPage = (props: Props) => {
  /* Za proste tipove, moze kao u JS useState(defaultValue), jer ce iz defaultValue da svali koji je tip, ali za 
  Reference types, mora useState<tip>(defaultValue), stoga i za proste tipove cu pisati kao za Reference type, jer je dobra praksa. */
  const [search, setSearch] = useState<string>(""); 
  
  const [searchResults, setSearchResults] = useState<CompanySearch[]>([])

  const [serverError, setServerError] = useState<string | null>(null); // Za gresku u axios koja moze biti string(ako bilo greske) ili null(ako nema greske)

  // Svaki put kad dodam Card u Portfolio, prilikom page loading ocu da mi stoji sve iz portfolio + novi ovaj dodati. I kad ugasim app ovo ce da stoji kad se vratim.
  // const [portfolioValues, setPortfolioValues] = useState<string[]>(() => {
  //   const saved = localStorage.getItem("portfolioValuesLocalStorage");
  //   return saved ? JSON.parse(saved) : [];
  // }); -ovo je bilo kad sam Card ocitavao sa neta, a sad ocu iz Portfolios tabele u backend.
  const [portfolioValues, setPortfolioValues] = useState<PortfolioGetFromBackend[] | null>([]);
  
  // Save portfolioValues to localStorage whenever it changes kako bi se sacuvale sve Card(Portfolios) koje sam addovao i kad odem na neku drugu stranicu
  // useEffect(() => {
  //   localStorage.setItem("portfolioValuesLocalStorage", JSON.stringify(portfolioValues));
  // }, [portfolioValues]);
  // Ovo je bilo kad nisam imao backend,pa sve sam morao cuvati u localStorage, a sad koristim getPortfoliosFromBackend da ocita iz backend.

  useEffect(() => {
    getPortfoliosFromBackend();
  },[]); // On loading da ocita listu tj my portfolios sa svim cards dodatim prosli put kad sam bio u app

  // Mora useCallback jer je def u ovoj metodi kao i onPortfolioDelete koja je prop za ListPortfolio tj CardPortfolio koji ima React.memo - videti workflow.
  const getPortfoliosFromBackend = useCallback(async () => {
    // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u portfolioGetApi koji je Frontend.
    await portfolioGetApi().then((result)=>{
      // result.data je tipa PortfolioGetFromBackend[] ako backend vratio StatusCode=2XX jer onda u portfolioGetAPI izvrsio try block
      // result je undefined, pa ne moze result.data, ako backend vratio StatusCode!=2XX (error) jer onda u portfolioGetAPI otiso u catch block
      if(result?.data){
        setPortfolioValues(result?.data); // Set triggers re-render of this component
      }
    }).catch((err)=> toast.warning("Couldnt get portfolios")); // Prikaze mali pop-up u gornji desni ugao ekrana ako bude frontend greska u portfolioGetApi (ne u backendu)
  },[]);  // Nema dependency jer ne poziva funkciju def unutar ovog component niti ocitava nesto externo niti portfolioGetApi prima argument

  /* Ova metoda pokrece se iz Search.tsx preko onChange, a onChange ne trazi e.preventDefault() u funkciji koju referencira,
  stoga ovde nema e.preventDefault() i zato sam prosledio direktno e.target.value (non Event type), a ne e (Event type).
  Ova funkcija je prosledjena to child component (Search) koji ima React.memo, i zato mora useCallback - objasnjeno u workflow.txt */
  const handleSearchChange = useCallback((eTargetValue: string) : void => {
    setSearch(eTargetValue); // search dobije vrednost koju sam uneo kroz formu in Search.tsx 
  },[]); // Nema dependency jer ne poziva funkciju def unutar ovog component niti ocitava nesto externo 

  /* Moze SyntheticEvent koji hvata vecinu Eventa razlicitog tipa, a mogo je i neki konkretni Event type biti.
    Ova metoda se pokrece iz Search.tsx preko <form onSubmit> i zato mora da ima e.preventDefault() kako browser
    ne bi reload ovu stranicu i da izgubim state variable serverError i searchresults, stoga argument je Event type.
      Ova funkcija je prosledjena to child component (Search) koji ima React.memo, i zato mora useCallback - objasnjeno u workflow.txt */
  const onSearchSubmit = useCallback(async (e: SyntheticEvent) => {
    e.preventDefault(); 
    // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u searchCompanies koji je Frontend.
    await searchCompanies(search).then((result) => {
      // result.data je CompanySearch[] tipa ako backend vratio StatusCode=2XX jer onda u searchCompanies try block
      // result je tipa string ako backend vratio StatusCode!=2XX jer onda u searchCompanies catch block 
      // Zato sto catch block u searchCompanies vraca nesto, ovde moramo proveriti sve moguce result tipove definisane u return u try/catch u searchCompanies
      if (typeof result === "string") // Ako u searchCompanies backend poslao StatusCode!=2XX (error) i otiso u catch block onda vraca string 
        setServerError(result);  // React re-renders this component when set is done
      else if (Array.isArray(result.data))  // Ako u searchCompanies backend poslao StatusCode=2XX onda try block izvrsio
        setSearchResults(result.data); // React re-renders this component when set is done
 
    }).catch((err) => toast.warn("Greska u searchCompanies frontend")) // Gore desno iskoci mali pop-up windows sa ovom porukom ako searchCompanies error in frontend
  }, [search]); // Mora, jer onSearchSubmit zavisi od search obzirom da samo tu externu vrednost CITA. Ne zavisi od serverError/searchResult jer njih UPISUJE

  /* Ova metoda se pokrece iz AddPortfolio.tsx (preko CardList.tsx tj Card.tsx) preko <form onSubmit> i zato mora da ima e.preventDefault() kako browser
  ne bi reload ovu stranicu i da izgubim state variables serverError i searchresults. Jer onSubmit u formi zahteva da njegova funkcija ima e.preventDefault ako prima argument Event typa - objasnio u Search.tsx
    Iako u AddPortfolio.tsx, pise SyntheticEvent za tip argumenta, ovde mora "e: any" (ne moze "e: SyntheticEvent"), jer ovo su slucajevi kad moram da iskljucim TS kako bi moglo e.target.elements.symbol.value
    
    useCallback jer Card je React.memo - pogledaj u workflow.txt objasnjenje*/
  const onPortfolioCreate = useCallback((e: any) => {
    e.preventDefault(); // Prevent page reload when submitting a form in AddPortfolio.tsx

    // Ovo ispod markiram, jer vise ne citam sa sajta, vec iz baze 
    /*const formData = new FormData(e.target); // e.target je cela Forma iz AddPortfolio.tsx
    const symbol = formData.get("symbol");  // jer <input name="symbol"> u formi iz AddPortfolio.tsx 
    // Mora provera, jer symbol mozda bude i null, a ne moze da doda null to postojeci portfolioValues jer useState<string[]> a nije useState<string[] | null>  
    if (typeof(symbol) === "string"){
      const updatedPorfolio = [...portfolioValues, symbol]; // Dodat novi portfolio u postojeci portfolioValues
      setPortfolioValues(updatedPorfolio); // Sada ListPortfolio nije prazan i prikazace se companija koju sam dodao
    }
    */

    /* e.target.elements.symbol.value predstavlja value={symbol} u <input> iz <form> from AddPortfolio. Jer e.target je <form>, a e.target.elements je <input> ili <button> u <form>, 
    a e.target.elements.symbol je <input> zato sto <input name="symbol"...>, a e.target.elements.symbol.value je value iz <input>. 
       
       e.target.elements.symbol.value = e.target[0].value
    */
    const symbol = e.target.elements.symbol.value; 

    // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u portfolioAddApi Frontend.
    portfolioAddApi(symbol).then((result) => {
      // result.data moze biti PortfolioAddDelete tipa ako backend vratio StatusCode=2XX jer onda portfolioAddApi u try block otiso
      // result moze biti undefined,pa result.data ne moze, ako backend vratio StatusCode!=2XX (error) jer onda portfolioAddApi u catch block otiso
      if(result?.status === 204){
        toast.success("Stock added to portfolio");
        getPortfoliosFromBackend(); // Da ocita opet listu gde ima jedan vise portoflio nakon dodavanja
      }
    }).catch((err)=> toast.warning("Couldnt create portfolio")); // Gore desno iskoci mali pop-up window zbog greske u portfolioAddApi frontend
  },[getPortfoliosFromBackend]);  // Jer getPortfoliosFromBackend def u ovoj component, dok symbol je def u funkciji pa zato ne moze biti dependency

  // const onPortfolioDelete = (indexToDelete: number) => {
  //   // filter pravi listu kao i map 
  //   const updated = portfolioValues.filter((_, index) => index !== indexToDelete);
  //   setPortfolioValues(updated);
  // }; -ovo koristio sam dok sam sa neta imao portfolio a sad cu iz baze

  // Koristim useCallback jer je ovo props function for CardPortfolio a CardPortfolio uses React.memo- objasnjenje u workflox.txt
  const onPortfolioDelete = useCallback(async (symbol: string) => {
    // Then-catch je isto try-catch. Catch mora ako dodje do greske u portfolioDeleteApi  frontend.
    portfolioDeleteApi(symbol).then((result) => {
      // result.data moze biti PortfolioAddDelete tipa ako backend vratio StatusCode=2XX jer odna portfolioDeleteApi u try block otiso
      // result moze biti undefined, pa ne moze result.data, ako backend vratio StatusCode!=2XX (error) jer onda portfolioDeleteApi u catch block otiso
      if(result?.status === 200){
        toast.success("Stock deleted from portfolios");
        getPortfoliosFromBackend(); // Da ocita opet listu gde ima jedan manje nakon brisanja. Obzirom da getPortfoliosFromBackend je def u ovom component, mora i ona biti useCallback i u dependnecy za onPortfolioDelete
      }
    }).catch((err) => toast.warn(err)); // Gore desno mali pop-up window zbog greske u portfolioDeleteApi frontend
  },[getPortfoliosFromBackend]); // Zato sto getPortfoliosFromBackend je def u ovom component i pozivam je u onPortfolioDelete. Ne moze symbol jer ga ne cita externo jer je to argument funkcije


  return (
    <div className="App">
      <Hero />
      <Search onSearchSubmit={onSearchSubmit} search={search} handleSearchChange={handleSearchChange} />
      {serverError && <h1>{serverError}</h1>}  {/* If serverError not null, then show "Network Error" */}
      <ListPortfolio portfolioValues={portfolioValues!} onPortfolioDelete={onPortfolioDelete}/> 
      <CardList searchResults={searchResults} onPortfolioCreate={onPortfolioCreate} /> {/* Kad ukucam ticker u search form, prvo se CardList aktivira, pa tek ListPortfolio se promeni ako dodam ticker Card kroz AddPortfolio. */}
    </div>
  )
  
}


export default SearchPage;