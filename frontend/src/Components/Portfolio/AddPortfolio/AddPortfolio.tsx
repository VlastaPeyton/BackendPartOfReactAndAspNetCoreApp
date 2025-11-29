import React, { SyntheticEvent } from 'react'

type Props = {
    onPortfolioCreate: (e: SyntheticEvent) => void;
    symbol: string;
}

const AddPortfolio = ({onPortfolioCreate, symbol}: Props) => {
    /* In <form>, onSubmit function (onPortfolioCreate) mora imati e.preventDefault() in it's definition 
    dok kod onChange ili onClick (u <form> ili van forme), skoro nikad nema e.preventDefault() - objasnjeno detaljno u Search.tsx
       <input name="symbol"> da bi onPortfolioCreate znao da doda value={symbol} from input to portfolioValues niz u SearchPage jer onPortfolioCreate def u SearchPage, 
    jer e.target[0].value(=e.target.elements.symbol.value), u onPortfolioCreate, se odnosi na value iz input. 
    e.target = <form...> 
    e.target.elements = <input> i <button>
    e.target.elemets.symbol = <input> jer <input name="symbol">
    e.target.elements.symbol.value = value iz <input>  */
    return (
        <form onSubmit={(e) => onPortfolioCreate(e)}> 
            <input name="symbol" readOnly={true} hidden={true} value={symbol} />
            <button type="submit">Add</button>
        </form>
    )
}

export default AddPortfolio;