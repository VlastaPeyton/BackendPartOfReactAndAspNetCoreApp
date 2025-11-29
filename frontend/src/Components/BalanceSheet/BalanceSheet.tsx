import { useEffect, useState } from 'react'
import { CompanyBalanceSheet } from '../../Types/company';
import { useOutletContext } from 'react-router-dom';
import { getBalanceSheet } from '../../Axios/api';
import RatioList from '../RatioList/RatioList';
import Spinner from '../Spinner/Spinner';
import { formatLargeMonetaryNumber } from '../../NumberFormatting/NumberFormatting';
import { toast } from 'react-toastify';

type Props = {}

// Vrste tabele koja ce biti prikazana na http://localhost:3000/company/:ticker/balance-sheet tj kad klikenm u Balance-sheet u Sidebar.tsx
// Morao sam napraviti tableConfig kao listu, jer RatioList.tsx je kodiran sa config.map, sto znaci da u RatioList config mora biti lista sa bar 1 element
const tableConfig = [
    // Element ove liste predstavlja, u BalanceSheet, vrstu tabele(jer RatioList tako kodiran) koja ima label i prikazanu vrednost render metodom. 
    {
      label: "Total Assets",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.totalAssets),
    },
    {
      label: "Current Assets",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.totalCurrentAssets),
    },
    {
      label: "Total Cash",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.cashAndCashEquivalents),
    },
    {
      label: "Property & equipment",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.propertyPlantEquipmentNet),
    },
    {
      label: "Intangible Assets",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.intangibleAssets),
    },
    {
      label: "Long Term Debt",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.longTermDebt),
    },
    {
      label: "Total Debt",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.otherCurrentLiabilities),
    },
    {
      label: "Total Liabilites",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.totalLiabilities),
    },
    {
      label: "Current Liabilities",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.totalCurrentLiabilities),
    },
    {
      label: "Long-Term Debt",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.longTermDebt),
    },
    {
      label: "Long-Term Income Taxes",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.otherLiabilities),
    },
    {
      label: "Stakeholder's Equity",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.totalStockholdersEquity),
    },
    {
      label: "Retained Earnings",
      render: (company: CompanyBalanceSheet) => formatLargeMonetaryNumber(company.retainedEarnings),
    },
  ];
  
const BalanceSheet = (props: Props) => {
    const ticker = useOutletContext<string>(); // Get data from <Outlet context=..> koji je smesten u parent route tj u CompanyPage (tj u njen CompanyDashboard). 
    // useOutletContext<string> garantuje da ticker ne moze biti undefined i zato ne mora getBalanceSheet(ticker!) vec getBalanceSheet(ticker)
    const [balanceSheet, setBalanceSheet] = useState<CompanyBalanceSheet>(); // undefined by default 
    // balanceSheet je CompanyBalanceSheet type tj 1 element, a ne niz, jer RatioList napravljen da radi sa 1 element, a ne sa niz.

    useEffect(() => {
        const fetchBalanceSheet = async () => {
          // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u getBalanceSheet Frontend.
          await getBalanceSheet(ticker).then((result) => {
            // Ako getBalanceSheet dobar, result je niz tipa CompanyBalanceSheet, pa RatioList moze da renderuje kako treba, jer polja iz result niza (jer RatioList radi sa 1 elem of result niz) postoje u tableConfig render funkcijama.
            // Ako getBalanceSheet error, otiso je u catch blok gde nema return, pa je result=undefined i zato result?.data[0] jer result? proveri da li je undefined jer ako bez toga runtime error ako result=undefined pa onda ne moze undefined.data[0]. 
            // result.data, jer u getBalanceSheet vraca AxiosResponse<CompanyBalanceSheet[]>, a ne CompanyBalanceSheet[], pa moram da izvucem samo payload pomocu .data
            setBalanceSheet(result?.data[0]); // React re-renders this component when balanceSheet is set. 
            // Mogo sam, pre setBalanceSheet, staviti if kao u CompanyProfile, ali nisam, jer sam siguran da ce API da vrati uvek CompanyBalanceSheet[].
          }).catch((err) => toast.warn(err)); // Ako dodje do greske u getBalanceSheet u frontend
        }

        fetchBalanceSheet();

    },[])

    // balanceSheet ? jer brze propagira kod iz poziva fetchBalanceSheet dovde gde renderuje nego sto se izvrsi ta async metoda i onda inicijalno balanceSheet=undefined i render prikaze spiner, ali onda async metoda kad zavrsi setBalanceSheet re-renders opet with balanceSheet!=undefined i prikaze lepo 
    // Mora bar <> i </> u return ako nemam <div> ili tako nesto explicitno.
    return (
        <>
            {balanceSheet ? (<RatioList data={balanceSheet} tableConfig={tableConfig} /> ) : (<Spinner />)}
        </>
    )
}

export default BalanceSheet;