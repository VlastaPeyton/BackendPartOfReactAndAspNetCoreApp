import { useEffect, useState } from 'react'
import { CompanyCashFlow } from '../../Types/company';
import { useOutletContext } from 'react-router-dom';
import { getCashflow } from '../../Axios/api';
import Table from '../Table/Table';
import Spinner from '../Spinner/Spinner';
import { formatLargeMonetaryNumber } from '../../NumberFormatting/NumberFormatting';
import { wait } from '@testing-library/user-event/dist/utils';
import { toast } from 'react-toastify';

type Props = {}

// Kolone tabele koja ce biti prikaza na http://localhost:3000/company/:ticker/cashflow tj kad klikenm u Cashflow u Sidebar.tsx
const tableConfig = [
  // Element ove liste predstavlja, u Cashflow, vrstu tabele(jer tako Table kodiran) koja ima label i prikazanu vrednost render metodom. 
  {
    label: "Date",
    render: (company: CompanyCashFlow) => company.date,
  },
  {
    label: "Operating Cashflow",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.operatingCashFlow),
  },
  {
    label: "Investing Cashflow",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.netCashUsedForInvestingActivites),
  },
  {
    label: "Financing Cashflow",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.netCashUsedProvidedByFinancingActivities),
  },
  {
    label: "Cash At End of Period",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.cashAtEndOfPeriod),
  },
  {
    label: "CapEX",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.capitalExpenditure),
  },
  {
    label: "Issuance Of Stock",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.commonStockIssued),
  },
  {
    label: "Free Cash Flow",
    render: (company: CompanyCashFlow) => formatLargeMonetaryNumber(company.freeCashFlow),
  },
];

function Cashflow({}: Props) {
  const ticker = useOutletContext<string>(); // Get data from <Outlet context=..> koji je smesten u parent route tj u CompanyPage (tj u njen CompanyDashboard). 
  // useOutletContext<string> garantuje da ticker ne moze biti undefined i onda ne mora getCashflow(ticker!) vec getCashflow(ticker)
  const [cashflow, setCashflow] = useState<CompanyCashFlow[]>(); // undefined by default
  // cashflow je niz, a ne 1 element, jer Table napravljen da radi sa niz,a ne sa 1 element

  useEffect(() => {
    const fetchCashflow = async () => {
      // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u getCashflow koji je Frontend.
      await getCashflow(ticker).then((result) => {
      // Ako backend poslao StatusCode=2XX to getCashflow, result.data je niz tipa CompanyCashFlow, pa Table moze da renderuje kako treba, jer polja iz result niza (jer Table radi sa ceo niz) postoje u tableConfig render poljima.
      // Ako backend poslao StatusCode!=2XX (error) to getCashflow, otiso je u catch blok gde nema return, pa je result=undefined i ne moze result.data i zato proveravam if(result) ili samo result?.data
      setCashflow(result?.data); // React re-renders this component when casflow is set
      }).catch((err) => toast.warn(err)); // AKo dodje do greske u getCashflow frontend
    }

    fetchCashflow();

  },[])

  // cashflow ? jer brze propagira kod iz poziva fetchCashflow dovde gde renderuje nego sto se izvrsi ta async metoda i onda inicijalno cashflow=undefined i render prikaze spiner, ali onda async metoda kad zavrsi setCashflow re-renders opet with cashflow!=undefined i prikaze lepo 
  // Mora bar <> i </> u return ako vec neam <div> ili tako nesto explicitno.
  return (
    <>
      {cashflow ? (<Table data={cashflow} tableConfig={tableConfig} />) : (<Spinner />)}
    </>
  )
}

export default Cashflow;