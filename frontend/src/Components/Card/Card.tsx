import React, { JSX, SyntheticEvent } from 'react'
import "./Card.css";
import { CompanySearch } from '../../Types/company';
import AddPortfolio from '../Portfolio/AddPortfolio/AddPortfolio';
import { Link } from 'react-router-dom';

// Umesto type moze interface, jer za .tsx je isto 
type Props = {
  id: string; 
  searchResult: CompanySearch; // Mogu pristupiti svakom polju iz CompanySearch 
  onPortfolioCreate: (e: SyntheticEvent) => void;
} 
/* Card je svaka kompanija koja se pojavi kad ukucam zeljeni ticker u search. Npr za "tsla" ticker, pojavi se Tesla,Inc(TSLA) | USD | NASDAQ - NASDAQ Global Select | Add 
gde Add dugme dodato iz AddPortfolio.tsx
   Link to sadrzi absolute route jer ima "/company..", a ne "company..."

Objasnjenje u workflow za React.memo i zasto je onPortfolioCreate useCallback.  */
const Card= React.memo(({id, searchResult, onPortfolioCreate}: Props) => {
  return (
    <div className="flex flex-col items-center justify-between w-full p-6 bg-slate-100 rounded-lg md:flex-row"
         id={id}
    > 
      <Link to={`/company/${searchResult.symbol}`} className="font-bold text-center text-black md:text-left">
        {searchResult.name} ({searchResult.symbol}) {/* Klik na Company name u Card vodi me na http://localhost:3000/company/:ticker jer u Routes.tsx je omoguceno da ta route vodi u CompanyPage.tsx */}
      </Link>
      <p className="text-black">{searchResult.currency}</p>
      <p className="font-bold text-black">{searchResult.exchangeShortName} - {searchResult.stockExchange} </p>
      <AddPortfolio onPortfolioCreate={onPortfolioCreate} symbol={searchResult.symbol} />                     {/* Add dugme u Card  */}
      {/* onPortfolioCreate je samo prosledjena, i zato nema poziv ove metode u Card.tsx */}
    </div>         
  )
});

export default Card; 