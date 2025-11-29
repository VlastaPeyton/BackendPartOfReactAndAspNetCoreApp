
/* Da bi ovo bio pravi TS Reusable Component, mora :           
    export type RowConfig<T> ={
        label: string;     // Jer tableConfig u DesignGuide/CompanyProfile/ BalanceSheet ima ovo polje
        subTitle?: string; // Jer tableConfig u DesignGudie ima, dok u CompanyProfile/BalanceSheet nema ovo polje
        render: (item: T) => React.ReactNode; 
        };
        
    type Props<T> ={
        data: T[]; // Jer sam u DesignGuide prosledio testIncomeStatementData niz, a ne samo jedan element
        config: RowConfig<T>[];
    };

    function RatioList<T>({data, config}: Props<T>){    // ili const RatioList<T,>
            const renderedRows = config.map((row, index) => {
                return (....)
            })
            return (...)
    }
    
    U DesignGuide/CompanyProfile/BalanceSheet onda mora <RatioList<CompanyKeyMetrics> data... />   
*/

type Props = {
    data: any;   // 1 element, a ne niz, je prosledjen iz DesignGuide/CompanyProfile/BalanceSheet
    tableConfig: any; // tableConfig from DesignGuide/ CompanyProfile / BalanceSheet
    // Oba polja su type any, jer je RatioList reusable component, posto koristim RatioList in DesignGuide.tsx /CompanyProfile.tsx / BalanceSheet.tsx, jer ovi fajlovi imaju razlicit tableConfig
}  

// Koristi se u DesignGuide.tsx, CompanyProfile.tsx i BalanceSheet.tsx, jer ovo je prikaz svake vrste tabele.
const RatioList = ({data, tableConfig }: Props) => {
    /* 
    <li> je list item i koristi se uvek kod liste tj u .map 
    .map pravi listu i koristi se samo nad listom, sto znaci da tableConfig u DesignGuide/CompanyProfile/BalanceSheet mora biti lista sa bar 1 elementom(objektom).
    .map pravi listu, pa u <li> moram key unique imati da bi React pratio elemente liste, a to moze recimo biti index ili neko polje sa unique value iz testIncomeStatementData[0] elementa (kad DesignGuide salje prop to RatioList)
    ili CompanyKeyMetric elementa (kad CompanyProfile salje prop to RatioList) ili CompanyBalanceSheet elementa (kad BalanceSheet salje prop to RatioList). 
    
    Zbog tableConfig.map((row:any, ...),a row je svaki element iz tableConfig, VSC dozvoljava bilo sta da kucam zbog row:any, i onda, pored row.label/render/subTitle, moze i row.nepostojecePolje, ali onda odje do greske ako to polje ne postoji u tableConfig u DesignGuide/CompanyProfile/BalanceSheet,
    pa zato ovo moram napraviti kao iznad sto pise generic da ne bi doslo do ovog potencijalnog problema (da ne omasim i ukucam polje koje ne postoji tj  npr za DesignGuide slucaj da samo moze row.label, row.subTitle i row.render).
    
    index:number, jer index je redni broj elementa(row) iz tableConfig, a redni broj je uvek 0,1,2...
    
    row.render(data) jer render u tableConfig prima argument tipa CompanyKeyMetrics/CompanyKeyMetrics/CompanyBalanceSheet i vraca zeljeno polje tog argumenta
    */

    // Ovo ce napravity, u DesignGuide/CompanyProfile/BalanceSheet, jedno polje only, iznad tabele, tipa "Company Name" | "This is company name" |  "Apple"
    
    // {row.subTitle && row.subTitle} <=> if row.subTitle is not null then show row.subTitle 
    const renderedRows = tableConfig.map((row: any, index: number) => {
        return (         // Nije dobra praksa row:any i zato vidi da popravis to sa generic u props ali u tom slucaju RatioList mora biti function generic a ne const generic
            <li className="py-3 sm:py-4" key={index}>
                <div className="flex items space">
                    <div className='flex-1 min-w-0'>
                        <p className="text-sm fontmt-medium text-gray-900 truncate">{row.label}</p> {/* Postoji label u tableConfig u DesignGuide/CompanyProfile/BalanceSheet, pa ne mora if else  */}
                        <p className="text-sm text-gray-500 truncate">{row.subTitle && row.subTitle}</p> {/* U BalanceSheet, tableConfig nema subtitle, dok u DesignGuide i CompanyProfile ima, pa zato if else mora */}
                    </div>
                    <div className="inline-flex items-center text-base font-semibold text-gray-900">{row.render(data)}</div> {/* Postoji render u tableConfing u DesignGuide/CompanyProfile/BalanceSheet, pa ne mora if else */}
                </div>
            </li>
        )
    })

    // renderedRows je <li>, pa ovde moram imati <ul> ili <ol>, jer <li> mora biti unutar <ol>/<ul> 
    return (
        <div className='bg-white shadow rounded-lg ml-4 mt-4 mb-4 p-4 sm:p-6 h-full'>
            <ul className='divide-y divided-gray-200'>{renderedRows}</ul> {/* Jer svaki row(element iz tableConfig) ima svoj <li>, pa mora <ul> za ceo renderedRows */}
        </div>
    )

    // Zbog samo 1 .map, RatioList je tabela sa jednom kolonom koja ima vrste iz tableConfig.
}

export default RatioList;
/* classname="py-3 sm:py-4"  :
    py-3 = Vertical padding
    sm:py-4 = Small Screen vertical padding
 */