import { CompanyTenK } from '../../Types/company';
import { Link } from 'react-router-dom';

type Props = {
    tenK: CompanyTenK;
}

const TenKItem = ({tenK}: Props) => {
    const fillingDate = new Date(tenK.fillingDate).getFullYear();
    /* reloadDocument - jer mi treba full page reload jer samo tako oce raditi tj da obrise iz svakog .tsx useState koji je setovan bio. 
     Ovo koristim samo u <Link> i Sluzi najcesce za Login/Logout da se state variable izbrisu.
       
       Kada sam kliknuo ono plavo u CompanyPage (ili njenoj child route), aktivira se reloadDocument da obrise sve useState u celoj application i otvara stranicu tj URL, 
    u istom prozoru (ne u novom prozoru), druge aplikacije koju gadja tenkK.finalLink tj salje novi Requst na taj neki Endpoint (to je endpoint tudji u ovom slucaju, a moglo je i da gadja neki moj 
    koji mogo biti kodiran u backendu za ovu aplikaciju ili u nekom drugom backendu koj bi morao biti running naravno). 

       Mora type="button" inace nece raditi 
       Link me vodi na finalLink koji ne znam kako izgleda, ali getTenK iz api.tsx ga vrati kroz polje CompanyTenkK objekta */

    return <Link to={tenK.finalLink} 
                 reloadDocument
                 type="button" 
                 className='inline-flex items-center p-4 text-md text-white bg-lightGreen'>
                    10K - {tenK.symbol} - {fillingDate} 
            </Link>
    
}

export default TenKItem;