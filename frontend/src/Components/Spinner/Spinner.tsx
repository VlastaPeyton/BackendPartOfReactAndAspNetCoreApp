
import { ClipLoader } from 'react-spinners';
import "./Spinner.css";

type Props = {
    isLoading?: boolean; // ? jer je optional Prop pa i ne moram da ga prosledim u <Spinner /> ako ne zelim jer mi je true by default
}

// isLoading je optional, pa ako ga ne prosledim, zelim npr da bude true (inace je undefined ako nema default value)
const Spinner = ({isLoading = true}: Props) => {
  // id="loading-spinner" jer u Spinner.css ima za ovaj id definisano.
  return (
    <div id="loading-spinner">
        <ClipLoader color="#36d7b7" loading={isLoading} size={35} aria-label='Loading Spinner' />
    </div>
  )
}

// U Spinner.css namestim da Spinner bude u centru stranice
export default Spinner;