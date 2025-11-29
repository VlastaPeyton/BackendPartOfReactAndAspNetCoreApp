import React from 'react'
import DeletePortfolio from '../DeletePortfolio/DeletePortfolio';
import { Link } from 'react-router-dom';
import { PortfolioGetFromBackend } from '../../../Models/PortfolioGetFromBackend';

type Props = {
  portfolioValue: PortfolioGetFromBackend;
  index: number;
  onPortfolioDelete: (symbol: string) => void;
}

// Ovde sam doso kad sam u Card.tsx kliknuo Add (AddPortfolio.tsx) da dodam ticker Card u My Portfolio 
const CardPortfolio = ( {portfolioValue, index, onPortfolioDelete}: Props) => {
  // Link sadrzi absolute route jer ima "/company..", a ne "company..." i vodi nas u https://localhost:port/company/:ticker jer u Routes omogucen route
  return (
    <>
      <Link to={`/company/${portfolioValue.symbol}`} className="flex flex-col w-full p-8 space-y-4 text-center">{portfolioValue.symbol}</Link> 
      <DeletePortfolio index={index} portfolioSymbol={portfolioValue.symbol} onPortfolioDelete={onPortfolioDelete} /> 
    </>
  )
}

export default CardPortfolio;