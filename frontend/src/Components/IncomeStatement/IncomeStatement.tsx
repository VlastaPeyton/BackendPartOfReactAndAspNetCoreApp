import React, { useEffect, useState } from 'react'
import { CompanyIncomeStatement } from '../../Types/company';
import { useOutletContext } from 'react-router-dom';
import { getIncomeStatement } from '../../Axios/api';
import Table from '../Table/Table';
import Spinner from '../Spinner/Spinner';
import { formatLargeMonetaryNumber, formatRatio } from '../../NumberFormatting/NumberFormatting';
import { toast } from 'react-toastify';

type Props = {}

// Kolone tabele koja ce biti prikaza na http://localhost:3000/company/:ticker/income-statement tj kad klikenm u Income-statement u Sidebar.tsx
const tableConfig = [
  // Element ove liste predstavlja, u IncomeStatement, kolonu tabele(jer tako Table kodiran) koja ima label i prikazanu vrednost render metodom. 
  {
    label: "Date",
    render: (company: CompanyIncomeStatement) => company.date,
  },
  {
    label: "Revenue",
    render: (company: CompanyIncomeStatement) => formatLargeMonetaryNumber(company.revenue),
  },
  {
    label: "Cost Of Revenue",
    render: (company: CompanyIncomeStatement) => formatLargeMonetaryNumber(company.costOfRevenue),
  },
  {
    label: "Depreciation",
    render: (company: CompanyIncomeStatement) => formatLargeMonetaryNumber(company.depreciationAndAmortization),
  },
  {
    label: "Operating Income",
    render: (company: CompanyIncomeStatement) => formatLargeMonetaryNumber(company.operatingIncome),
  },
  {
    label: "Income Before Taxes",
    render: (company: CompanyIncomeStatement) => formatLargeMonetaryNumber(company.incomeBeforeTax),
  },
  {
    label: "Net Income",
    render: (company: CompanyIncomeStatement) => formatLargeMonetaryNumber(company.netIncome),
  },
  {
    label: "Net Income Ratio",
    render: (company: CompanyIncomeStatement) => formatRatio(company.netIncomeRatio),
  },
  {
    label: "Earnings Per Share",
    render: (company: CompanyIncomeStatement) => formatRatio(company.eps),
  },
  {
    label: "Earnings Per Diluted",
    render: (company: CompanyIncomeStatement) => formatRatio(company.epsdiluted),
  },
  {
    label: "Gross Profit Ratio",
    render: (company: CompanyIncomeStatement) => formatRatio(company.grossProfitRatio),
  },
  {
    label: "Opearting Income Ratio",
    render: (company: CompanyIncomeStatement) => formatRatio(company.operatingIncomeRatio),
  },
  {
    label: "Income Before Taxes Ratio",
    render: (company: CompanyIncomeStatement) => formatRatio(company.incomeBeforeTaxRatio),
  },
];

const IncomeStatement = (props: Props) => {
  const ticker = useOutletContext<string>(); // Get data from <Outlet context=..> koji je smesten u parent route tj u CompanyPage (tj u njen CompanyDashboard). 
  // Zbog useOutletContext garantuje da bice string, pa ne treba getIncomeStatement(ticker!), vec getIncomeStatement(ticker)
  const [incomeStatement, setIncomeStatement] = useState<CompanyIncomeStatement[]>(); // undefined by default 
  // incomeStatement je niz, a ne 1 element, jer Table napravljen da radi sa niz,a ne sa 1 element

  useEffect(() => {
    const fetchIncomeStatement = async() => {
      // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u getIncomeStatement koji je Frontend.
      await getIncomeStatement(ticker).then((result) => {
         // Ako getIncomeStatement dobar, result je niz tipa CompanyIncomeStatement, pa Table moze da renderuje kako treba, jer polja iz result niza (jer Table radi sa ceo niz) postoje u tableConfig render poljima.
      // Ako getIncomeStatement error, otiso je u catch blok gde nema return, pa je result=undefined i zato proveravam ovaj if. 
      // Ovaj if je sigurniji, nego da ga nemam kad ovu liniju ispod komentiranu  imam sa result?.data. Mogu biti more strict da proverim i ova  polja iz tableConfig render da l su undefined (typeof result.data.revenue === 'number'), ali nema potrebe jer znam da ce API da ih posalje uvek. U principu ni ovaj if ne treba, jer znam da API sve posalje.
      // result.data, jer u getIncomeStatement vraca AxiosResponse<CompanyIncomeStatement[]>, a ne CompanyIncomeStatement[], pa moram da izvucem samo payload pomocu .data
      if(result && Array.isArray(result.data))
        setIncomeStatement(result.data); // React re-renders this component when incomeStatement is set
        //setIncomeStatement(result?.data);  // Ako ne zelim ovaj uslov iznad(jer nepotreban obzirom da API znam da ce uvek da vrati kako treba, ali je dobra praksa imati ga), jer getIncomeStatement from api.tsx moze da niz od CompanyIncomeStatement ili undefined(ako bude error u getIncomeStatement pa udje u catch blok gde nema return), pa zato ? mora, jer ? proveri da li je result=undefined
      }).catch((err) => toast.warn(err)); // Mali pop-up window gore desno ako greska bude u getIncomeStatement frontend 
    }

    fetchIncomeStatement();

  },[]);

  // incomeStatement ? jer brze propagira kod iz poziva getIncomeStatement dovde gde renderuje nego sto se izvrsi ta async metoda i onda inicijalno incomeStatement=undefined i render prikaze spiner, ali onda async metoda kad zavrsi setIncomeStatement re-renders opet with incomeStatement!=undefined i prikaze lepo 
  // Mora bar <> i </> u return ako vec neam <div> ili tako nesto explicitno.
  return (
    <>
      {incomeStatement ? (<Table data={incomeStatement} tableConfig={tableConfig} />) : (<Spinner />)}
    </>
  )
}

export default IncomeStatement;