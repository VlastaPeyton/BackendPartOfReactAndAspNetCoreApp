import CardPortfolio from '../CardPortfolio/CardPortfolio';
import { v4 as uuidv4 } from 'uuid';
import { PortfolioGetFromBackend } from '../../../Models/PortfolioGetFromBackend';

type Props = {
    portfolioValues: PortfolioGetFromBackend[];
    onPortfolioDelete: (e: any) => void;
}

// Kada u Search.tsx input formi ukucam zeljeni ticker, mogu da kliknem na Add u zeljenom Cardu da ga dodam u My portfolio.
const ListPortfolio = ({portfolioValues, onPortfolioDelete}: Props) => {
    /* .map zahteva unique key da prosledim to CardPortfolio iako CardPortfolio ne prima key, vec da bi React znao da prati elemente liste jer <CardPortfolio> renderovan u .map 
    Uzeo key={index} jer index sig uvek uqiue jer je redni broj elementa liste. */
    return (
        <section id="portfolio">
            <h2 className="mb-3 mt-3 text-3xl font-semibold text-center md:text-4xl">
                My portfolio
            </h2>
            <div className="relative flex flex-col items-center max-w-5xl mx-auto space-y-10 px-10 mb-5 md:px-6 md:space-y-0 md:space-x-7 md:flex-row">
                <>
                    {portfolioValues.length > 0 ? (
                        portfolioValues.map((portfolioValue: PortfolioGetFromBackend, index:number) => (
                            <CardPortfolio
                                portfolioValue={portfolioValue}
                                index={index}
                                onPortfolioDelete={onPortfolioDelete}
                                key={index}
                            />
                        ))
                    ) : (
                        <h3 className="mb-3 mt-3 text-xl font-semibold text-center">
                            Your portfolio is empty {/* Sve dok ne kliknem bar na 1 Card Add, pisace ovo. */}
                        </h3>
                    )}
                </>
            </div>
        </section>
    );
    
}

export default ListPortfolio;