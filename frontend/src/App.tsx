import { Outlet } from 'react-router'; // Isto kao u CompanyDashboard.tsx gde import from 'react-router-dom'
import './App.css';
import { ToastContainer } from 'react-toastify'; // For small pop-up which will be seen while login/register....
import "react-toastify/dist/ReactToastify.css"; // Default CSS for ToastContainer
import { UserProvider } from './Context/useAuthContext';
import ErrorBoundary from './Components/ErrorBoundary/ErrorBoundary';
import BuggyComponent from './Components/BuggyComponent/BuggyComponent';
import Navbar from './Components/Navbar/Navbar';

function App() {

  return (
    /* 
      App je glavna komponenta u koju se ucitava sve ostalo u Routes.tsx 
      Components koje su ovde navedene, bice prisutne na svakoj stranici (unutar Routes.tsx), pa zato Navbar, BuggyComponent i ToastContainer stavljam ovde u App.tsx, a ne u Routes.tsx.
      Mora ovde biti <Outlet> (Outlet = sve children routes za <App> in Routes.tsx) + da renderuje Component of current route.

      Routes.tsx ima createBrowserRouter, jer je to novija verzija od <Routes><Route></Routes> koje sam u ReactApp projektu pisao u App.tsx. 
      Routes.tsx ima lazy loading of components, sto znaci da se on startup ocita samo homepage (jer sam tako namestio), a ne sve stranice.

      <UserProvider> okruzuje <Navbar>, <ToastContainer>, <BuggyComponent> i <Outlet> , stoga u tim components i u njihovoj deci mogu da pristupim direktno svemu iz <UserProvider> 
     (useAuthContext.tsx) bez prop passing, jer u UserProvider imam children:React.ReactNode i renderovanje children + logiku da pristup direktno moze.

      ErrorBoundary sluzi da pokupi rendering greske u <App> ili child route of <App>. 
    */
    <>
    <ErrorBoundary>       
      <UserProvider>      
        <BuggyComponent />
        <Navbar />  
        <Outlet /> 
        <ToastContainer />
      </UserProvider>
    </ErrorBoundary>
    </>
  );
}

export default App;

// Da bi razumeo aplikaciju, pocni od Pages foldera...