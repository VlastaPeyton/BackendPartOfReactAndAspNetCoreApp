import Table from '../../Components/Table/Table';
import RatioList from '../../Components/RatioList/RatioList';
import { CompanyKeyMetrics } from '../../Types/company';
import { testIncomeStatementData } from '../../Components/Table/testData'; //testIncomeStatementData samo za "AAPL" imam i zato ovde ne ocitavam preko api.tsx (kao u CompanyProfile npr), jer imam offline podatke.

type Props = {}

// tableConfig je tabela (zbog RatioList.tsx definicije imace 1 kolonu) koja ce se pojaviti u http://localhost:3000/design-guide 
// Morao sam napraviti tableConfig kao listu, jer RatioList.tsx je kodiran sa config.map, sto znaci da u RatioList config mora biti lista sa bar 1 element
const tableConfig = [
  // Element ove liste predstavlja, u DesignGuide, vrstu/kolonu tabele (jer tako RatioList/Table kodiran) koja ima label, subTitle i prikazanu vrednost render metodom. 
  { 
    label: "Market Cap",
    render: (company: CompanyKeyMetrics) => company.marketCapTTM,  // marketCapTTM je polje iz CompanyKeyMetrics 
    subTitle: "Total value of all"
  },
];
/* Pogledaj koja polja ima element of testIncomeStatementData, a koja render u tableConfig => ni jedno polje u testIncomeStatementData nije isto kao u CompanyKeyMetrics i to pravi error => napravi da testIcomeStatementData ima polja kao CompanyKeyMetrics  i napravi interface koji ima sva polja kao element iz testIncomeStatementData koga zameni u render
u RatioList/Table kod row.render(data) linije samo za DesignGuide slucaj, pa ne moze prikaze nista on this page via RatioList/Table. 
U RatioList/Table u Props je data:any (jer RatioList/Table je reusable component) i zato nema error tu, ali svakako ne prikaze nista.

  RatioList ocekuje samo 1 element of testIncomeStatementData i zato je tako kodiran RatioList.tsx => mora npr testIncomeStatementData[0]
  Table ocekuje ceo testIncomeStatementData niz i zato je tako kodiran Table.tsx => mora testIncomeStatementData. 
*/

// U Routes.tsx definisano http://localhost:3000/design-guide za ovu stranicu.
const DesignGuide = (props: Props) => {
  
  return (
    <>
        <h1>Desing guide</h1>
        <RatioList data={testIncomeStatementData[0]} tableConfig={tableConfig} />
        <Table data={testIncomeStatementData} tableConfig={tableConfig} />
    </>
  )
}

export default DesignGuide;