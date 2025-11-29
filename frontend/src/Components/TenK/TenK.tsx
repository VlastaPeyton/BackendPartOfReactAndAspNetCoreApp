import { useEffect, useState } from 'react'
import { CompanyTenK } from '../../Types/company';
import { getTenK } from '../../Axios/api';
import TenKItem from './TenKItem';
import Spinner from '../Spinner/Spinner';
import { toast } from 'react-toastify';

type Props = {
    ticker: string;
}

const TenK = ({ticker}: Props) => {

    const [companyData, setCompanyData] = useState<CompanyTenK[]>(); // Default value is undefined 

    useEffect(() => {
        // Dobra praksa je staviti definiciju ove metode izvan useEffect. 
        const fetchTenK = async () => {
            // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u getTenK koji je Frontend.
            await getTenK(ticker).then((result) => {
                // Ako backend posalje StatusCode=2XX to getTenk onda result.data je niz CompanyTenk[] jer smo u try usli 
                // Ako backend posalje StatusCode!=2XX (error) to getTenK onda result=undefined i result.data ne moze jer smo u catch block 
                setCompanyData(result?.data); // Mora result? jer result moze biti i undefined,a nema if(result) pa da moze bez upitnika. React re-renders this component when company is set. 
            }).catch((err) => toast.warn(err)); // Pop-up window ako se desi greska u getTenK frontend
        }

        fetchTenK();

    }, [ticker]);

    // companyData ? jer brze propagira kod iz poziva fetchTenK dovde gde renderuje nego sto se izvrsi ta async metoda i onda inicijalno companyData=undefined i render prikaze spiner, ali onda async metoda kad zavrsi setCompanyData re-renders opet with company!=undefined
    return (
        <div className="inline-flex rounded-md shadow-sm m-4">
            {companyData ? (companyData.slice(0, 5).map((tenK : CompanyTenK, index: number) => {return <TenKItem key={index} tenK={tenK}/> })) : (<Spinner />)} 
        </div>
    )
    // Mora key, iako TenKItem neka key, jer .map zahteva to da bi HTML znao da prati elemente liste. Stavio key={index} jer mi najlakse, a mogo sam i symbol polje iz CompanyTenK
}

export default TenK;