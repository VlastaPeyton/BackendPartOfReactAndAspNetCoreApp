import { useEffect, useState } from 'react'
import { CompanyKeyMetrics } from '../../Types/company';
import { useOutletContext } from 'react-router-dom';
import { getKeyMetrics } from '../../Axios/api';
import RatioList from '../RatioList/RatioList';
import Spinner from '../Spinner/Spinner';
import { formatLargeNonMonetaryNumber, formatRatio } from '../../NumberFormatting/NumberFormatting';
import StockComment from '../StockComment/StockComment';
import { toast } from 'react-toastify';

type Props = {}

// Vrste tabele i vrednosti njihove koje ce se pojaviti na http://localhost:3000/company/:ticker/company-profile tj kad kliknem Company-profile u Sidebar.tsx
// Morao sam napraviti tableConfig kao listu, jer RatioList.tsx je kodiran sa config.map, sto znaci da u RatioList config mora biti lista sa bar 1 element
const tableConfig = [
  // Element ove liste predstavlja, u CompanyProfile, vrstu tabele(jer RatioList tako kodiran) koja ima label, subTitle i prikazanu vrednost render metodom. 
  {
    label: "Market Cap",
    render: (company: CompanyKeyMetrics) => formatLargeNonMonetaryNumber(company.marketCapTTM),
    subTitle: "Ukupna vrednost svih firminih stocks"
  },
  {
    label: "Current Ratio",
    render: (company: CompanyKeyMetrics) => formatRatio(company.currentRatioTTM),
    subTitle: "Firmina sposobnost da plati kratkorocna dugovanja"
  },
  {
    label: "Return On Equity",
    render: (company: CompanyKeyMetrics) => formatRatio(company.roeTTM),
    subTitle: "Mera firminog net income podeljen sa shareholders equity"
  },
  {
    label: "Return On Asset",
    render: (company: CompanyKeyMetrics) => formatRatio(company.cashPerShareTTM),
    subTitle: "Koliko firma efektno koristi svoje assets"
  },
];

const CompanyProfile = (props: Props) => {
  const ticker = useOutletContext<string>(); // Get data from <Outlet context=..> koji je smesten u parent route tj u CompanyPage (tj u njen CompanyDashboard). 
  const [companyData, setCompanyData] = useState<CompanyKeyMetrics>(); // Initial value je undefined, jer nema explicitno setovan default value. Ako getKeyMetrics vrati undefined, ovo ostaje undefined.
  // companyData je CompanyKeyMetric type tj 1 element, a ne niz, jer RatioList napravljen da radi sa 1 element, a ne sa niz.

  useEffect(() => {
    const getCompanyMetrics = async () => {
      // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u getKeyMetrics koji je Frontend.
      await getKeyMetrics(ticker).then((result) => {
        // Ako backend posalje StatusCode=2XX to getKeyMetrics onda odradi try block, result.data je niz tipa CompanyKeyMetrics, pa RatioList moze da renderuje kako treba, jer polja iz result niza (jer RatioList radi sa 1 elem of result niz) postoje u tableConfig render funkcijama.
        // Ako backend posalje StatusCode!=2XX (Error) to getKeyMetrics onda odradi catch block gde nema return, pa je result=undefined, ne moze result.data, i zato proveravam ovaj if. 
        // Ovaj if je sigurniji, nego da ga nemam kad ovu liniju ispod komentiranu  imam sa result?.data[0]. Mogu biti more strict da proverim i ova polja iz tableConfig render da l su undefined (typeof result.data[0].marketCapTTM === 'number'), ali nema potrebe jer znam da ce API da ih posalje uvek. U principu ni ovaj if ne treba, jer znam da API sve posalje.
        // result.data, jer u getKeyMetrics vraca AxiosResponse<CompanyKeyMetrics[]>, a ne CompanyKeyMetrics[], pa moram da izvucem samo payload pomocu .data
        if(result && Array.isArray(result.data) && result.data[0]) 
          //setCompanyData(result?.data[0]); // Ako ne zelim ovaj uslov iznad(jer nepotreban obzirom da API znam da ce uvek da vrati kako treba, ali je dobra praksa imati ga), jer getKeyMetrcis from api.tsx moze da niz od CompanyKeyMetrics ili undefined(ako bude error u getKeyMetrics pa udje u catch blok gde nema return), pa zato ? mora, jer ? proveri da li je result=undefined
          setCompanyData(result.data[0]);   // React re-renders this component when companyData is set 
      }).catch((err) => toast.warn(err));  // Prikaze mali pop-up u gornji desni ugao ekrana ako bude frontend greska u getKeyMetrics (ne u backendu)
    }

    getCompanyMetrics();

  },[]); 

  // companyData ? jer brze propagira kod iz poziva getKeyMetrics dovde gde renderuje nego sto se izvrsi ta async metoda i onda inicijalno companyData=undefined i render prikaze spiner, ali onda async metoda kad zavrsi setCompanyData re-renders opet with companyData!=undefined i prikaze lepo 
  // Mora <> i </> ako vracam vise od 1 component i zato su <RatioList/> i <StockComponent/> okruzene time, dok <Spinner/> nije.
  // Mora bar <> i </> u return ako vec neam <div> ili tako nesto explicitno.
  return (
    <>
    {companyData ? (<> <RatioList data={companyData} tableConfig={tableConfig} />
                       <StockComment stockSymbol={ticker} />
                    </> ) 
                    
                  : (<Spinner />)}
    </>
  )
}

export default CompanyProfile;