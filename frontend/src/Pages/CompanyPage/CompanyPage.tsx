import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { CompanyProfile } from '../../Types/company';
import { getCompanyProfile } from '../../Axios/api';
import Sidebar from '../../Components/Sidebar/Sidebar';
import CompanyDashboard from '../../Components/CompanyDashboard/CompanyDashboard';
import Tile from '../../Components/Tile/Tile';
import Spinner from '../../Components/Spinner/Spinner';
import TenK from '../../Components/TenK/TenK';
import { toast } from 'react-toastify';

type Props = {}

/* Ovde smo stigli kada u SearchPage.tsx u input formi sam ukucao npr "tsla" ticker i prikazala se Tesla,Inc (TSLA) | USD | ... Card i kliknuo sam 
na "Tesla,Inc (TSLA)" i odvelo me je ovde, jer je u Routes definisana http://localhost:3000/company/:ticker za element=<CompanyPage /> 

  Sve sto je prikazano na CompanyPage pojavice se i na child pages of CompanyPage tj na CompanyProfile/IncomeStatement/BalanceSheet/Cashflow */
const CompanyPage = (props: Props) => {

  let {ticker} = useParams();  // u Routes.tsx, PompanyPage route je "http://localhost:3000/company/:ticker", pa da se uzme sve posle : tj ticker uzima from URL.
  // Zbog useParams, moguce je da ticker bude undefined by default i zato svuda u then-catch  umesto ticker! mogu da stavim if(ticker) i onda bez ticker, a ne ticker!  

  const [company, setCompany] = useState<CompanyProfile>();
  
  useEffect(() => {
    // Dobra praksa je staviti ovu metodu izvan useEffect 
    const getProfileInit = async () => {
      // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u getCompanyprofile koji je Frontend.
      // Mora ticker! jer iance compile error posto ovime garantujem da ticker nece biti prazan string, jer nisam napisao if(ticker) pa da moze samo ticker
      await getCompanyProfile(ticker!).then((result) => {
        // result.data je tipa CompanyProfile[] ako backend posalje StatusCode=2XX to getCompanyProfile jer onda u try block izvrsio
        // result je undefined, i result.data ne moze, ako backend posalje StatusCode!=2XX (error) to getCompanyProfile jer onda u catch block izvrsio
        setCompany(result?.data[0]); // Mora result? zato sto moze biti i undefined i CompanyProfile[], a neamo if(result) da otklonimo sumnju. React re-renders this component when company is set. 
      }).catch((err) => toast.warn(err));  // Mali pop-up gore desno sa porukom ako greska bude u getCompanyProfile u frontend
    } 

    getProfileInit();

  }, []) // Samo tokom page loading ce ovo da se izvrsi, jer nema dependencies

  return (
    /* Moze <CompanyDashboard...> <Tile...>... <TenK> </CompanyDashboard> jer CompanyDashboard.tsx ima children: React.ReactNode sto predstavlja 0, 1 ili vise Components da bude unutar <CompanyDashboard>.
    Moze Tile i TenK da se proslede kao obican Prop u CompanyDashboard, ali onda u CompanyDashboard mora da napravim 2 child tipa Tile/TenK sto je losa praksa, jer ovako prosledjivanje children omogucava 0 ili vise Components bilo kog tipa da stavim u <CompanyDashboard>.
      U CompanyDashboard imam <Outlet..> koji renderuje children routes of CompanyPage definisane u Routes.tsx bas kao u App.tsx sto sam napisao <Outlet> da renderuje children routes of <App> takodje definisane u Routes.tsx
      Prosledio sam ticker! u CompanyDashboard, a ne ticker, jer useParams ne garantuje da ce ticker biti string jer moze i undefined biti.
      subTitle Prop in Tile.tsx je string | number, jer odavde prosledjujemo companyName (string) i price (number)
      company ? jer brze propagira kod iz poziva getCompanyProfile dovde gde renderuje, nego sto se izvrsi ta async metoda i onda inicijalno company=undefined i render prikaze spiner, ali onda async metoda kad zavrsi setCompany re-renders opet with company!=undefined i prikaze lepo
      */
    <>
    {company ? (
                <div className="w-full relative flex ct-docs-disable-sidebar-content overflow-x-hidden">
                  <Sidebar />
                  <CompanyDashboard ticker={ticker!}> 
                    <Tile title="Company Name" subTitle={company.companyName} /> 
                    <Tile title="Price" subTitle={'$' + company.price} /> {/* price je number -> Tile Props subTitle mora biti string | number  */}
                    <Tile title="Sector" subTitle={company.sector} />
                    <Tile title="DCF" subTitle={'$' + company.dcf.toString()} /> {/* Moglo i kao number jer u Tile namesteno da prima string ili number */}
                    <TenK ticker={company.symbol} />
                    <p className="bg-white shadow rounded text-medium text-gray-900 p-3 mt-1 m-4">{company.description}</p>
                  </CompanyDashboard>
                </div> ) 
                
             : (<Spinner />)}
    </>
  )
}

export default CompanyPage;