// Koristi u DesingGuide.tsx / IncomeStatement.tsx / Cashflow.tsx, pa je reusable component

type Props = {
    data: any;   // niz, a ne 1 element, je prosledjen iz DesignGuide / Incomestatement / Cashflow 
    tableConfig: any; // tableConfig from DesignGuide / IncomeStatement / Cashflow
    // Oba polja su type any, jer je Table.tsx reusable component, posto koristim Table in DesignGuide / IncomeStatement / Cashflow, jer ove 3 klase imaju razlicit tableConfig i zato any
}  

// Modifikuj sa generic kodom bas kao u RatioList.tsx sto je uradjeno da ne bude row:any i config:any. 

const Table = ({data, tableConfig}: Props) => {
    // Ocekuje data kao listu(niz, a ne kao 1 element) i zato ovako je napisano sa data.map(renderedRows) i config.map(renderedHeaders)
    
    /* 
    .map pravi listu i koristi se samo nad listom, sto znaci da tableConfig u DesignGuide/IncomeStatement/Cashflow mora biti lista sa bar 1 elementom(objektom).
    .map pravi listu, a React mora da prati elemente liste (vrste tabele) i zato svaki mora imati unique key. 
    <tr> mora imati jey unique i moze key={index} jer nzm koje je unique polje u data niz prop poslato iz DesignGuide/IncomeStatement/Cashlflow, a mrzi me da trazim kroz testIncomeStatementData/CompanyIncomeStatement/CompanyCashflow.
    <td> mora imati key unique, ali ne sme imati key={index}, jer <tr> ima key={index}, ali moze key={val.label} jer u DesignGuide/IncomeStatement/Cashflow tableConfig ima label polje koje je unique 
    <th> mora imati key unique i moze key={config.label}, pa ne mora index , a <th> se odnosi na config a tu znam da je svaki label unique (vidim u DesignGuide)
    
    Zbog data.map((elem:any, index: number)), elem je svaki element iz data niza (testIncomeStatementData/CompanyIncomeStatement/CompanyCashflow niz) koji ima svoja polja, dok index je redni broj elementa koji mogu da dohvatim u .map ili ne moram.
    Zbog elem:any, VSC dozvoljava da bilo sta kucam i onda, pored elem.polje( polje iz testIncomeStatementData/CompanyIncomeStatement/CompanyCashflow) moze i elem.nepoStojecePolje i onda dodje do runtime greske ako to polje ne postoji, i zato treba se Table odraditi kao generic u RatioList sto pise, jer elem.nepostojecePolje dovodi do greske koju jedva nadjem. 
    Zbog tableConfig.map(config:any), index mi ne treba, config je element in iz tableConfig niza koji ima label/render polja. 
    Zbog config:any, VSC dozvoljava da bilo sta kucam i onda, pored config.label/render moze i config.nepostojecePolje i onda dodje go runtime greske ako to polje ne postoji i zato treba se Table odradti geenric kao u RatioList sto pise, jer config.nepostojecePolje dovodi do greske koju jedva nadjem.
    
    config.render(elem) jer render u tableConfig prima argument tipa testIncomeStatementData/CompanyIncomeStatement/CompanyCashflow i dohvata zeljeno polje tog argumenta

    */
    
    // Table rows tj vrednosti kolona.
    const renderedRows = data.map((elem: any, index: number) => {
        // Inside same list (same .map) everything has to have different unique key => <tr> mora razlicit key od <td> 
        // <tr> je svaka vrsta tabele koju punim sa config.render(elem)
        /*  Svaki element iz niza(data) ima npr 20 polja i data.map(elem...) dohvati svaki element iz niza(data) i predstavi ga kao elem sa svih njegovih 20 polja.
        U DesignGuide/IncomeStatement/Cashflow, tableConfig elementi imaju label i render polje (i subTitle za DesignGuide samo), ali ne koriste u render sva polja iz testIncomeStatementData/CompanyIncomeStatement/CompanyCashflow i to je u redu tj nece se mapirati taj visak koji sam dobio (iz API za IncomeStatement i Cashflow, offline za DesignGuide)
        TableConfig nema 20 rendera za svako polje iz element of data po jedan render i to je u redu jer ne moramo prikazati sve sto smo dobili u data.
        Kada data.map uzme prvi element iz data, sa 20 polja, poziva se tableConfig.map onoliko puta koliko ima elemenata u tableConfig, da za neka od tih polja elementa uradi render(elem) jer u svakoj tableConfig render prima argument tipa elementa iz data (testIncomeStatementData/CompanyIncomeStatement/CompanyCashflow)
        */
        return (                
            <tr key={index}> 
                {tableConfig.map((config: any) => {
                    return (<td className="p-4 whitespace-nowrap text-sm font-normal text-gray-900" key={config.label}>{config.render(elem)}</td>)
                })}
            </tr>
        )
    })

    // Table headers tj imena kolona. 
    const renderedHeaders = tableConfig.map((config: any) => {
        // <th> je svako ime kolone tabele.
        // Ovde moze key isti kao <td> (jer u <td> gore imam takodje tableConfig.map) jer su u razlicitoj listi (different .map). Svakako, moze isti key kao <tr> jer su u razlicitoj .map listi.
        return (                
            <th className='p-4 text-left text-xs font-medium text-gray-500 uppercase tracking-wider' key={config.label}>{config.label}</th>
        )
    })

    // Zbog dva .map i HTML, ovo je tabela sa vise od 1 kolone koje su label iz tableConfig.

    return (
        // <div className='bg-white shadow rounded-lg p-4 sm:p-6 xl:p-8'>
        //     <table>
        //         <thead className="min-w-full divide-y divide-gray-200 m-5">
        //             <tr>{renderedHeaders}</tr>
        //         </thead>
        //         <tbody>{renderedRows}</tbody>
        //     </table>
        // </div>
        // Ovo sam komentirao jer nesto tabela mi je skracena u desnom delu 

        <div className='bg-white shadow rounded-lg p-4 sm:p-6 xl:p-8 overflow-x-auto'>
            <div className="min-w-full inline-block align-middle">
                <div className="overflow-x-auto">
                    <table className="min-w-full">
                        <thead>
                            <tr>
                            {renderedHeaders}
                            </tr>
                        </thead>
                        <tbody>{renderedRows}</tbody>
                    </table>
                </div>
            </div>
        </div>
  )
}

export default Table;