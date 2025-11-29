import { createBrowserRouter} from "react-router-dom";
import { lazy, Suspense } from "react";

import App from "../App";
import ProtectedRoute from "./ProtectedRoute";
import Spinner from "../Components/Spinner/Spinner"

// Lazy loading koristiti zbog route-level code splitting. Da se ocita samo stranica koja je zatrazena u browseru cime se smanjuje Bundle i brze se inicijalno otvara app.
const HomePage = lazy(() => import("../Pages/HomePage/HomePage"));
const SearchPage = lazy(() => import("../Pages/SearchPage/SearchPage"));
const CompanyPage = lazy(() => import("../Pages/CompanyPage/CompanyPage"));
const CompanyProfile = lazy(() => import("../Components/CompanyProfile/CompanyProfile"));
const IncomeStatement = lazy(() => import("../Components/IncomeStatement/IncomeStatement"));
const DesignGuide = lazy(() => import("../Pages/DesignGuide/DesignGuide"));
const BalanceSheet = lazy(() => import("../Components/BalanceSheet/BalanceSheet"));
const Cashflow = lazy(() => import("../Components/Cashflow/Cashflow"));
const LoginPage = lazy(() => import("../Pages/LoginPage/LoginPage"));
const RegisterPage = lazy(() => import("../Pages/RegisterPage/RegisterPage"));
const ForgotPasswordPage = lazy(() => import("../Pages/ForgotPasswordPage/ForgotPasswordPage"));
const ResetPasswordPage = lazy(() => import("../Pages/ResetPasswordPage/ResetPasswordPage"));

// Helper component to wrap lazy components with Suspense. 
type PropsLazyWrapper = {
    children: React.ReactNode // Unutar <LazyWrapper> bice neka component 
}
// Spinner shows while loading component. Children shows when loading completes.
const LazyWrapper = ({ children }: PropsLazyWrapper) => (
  <Suspense fallback={<div className="flex justify-center items-center min-h-screen"> <Spinner /> </div>}>
    {children} 
  </Suspense>
);

/*  Routes.tsx se koristi u index.tsx ! 

    createBrowserRouter je noviji nacin da napravim Routes u odnosu na <Routes> <Route path="/" element={<HomePage />} /> .... </Routes>.

    Components, koje nisu navedene ovde, a jesu u App.tsx, bice prisutne na svakoj stranici ovde navedenoj.
    
    Sve ove children routes nekog parent route (elementa) (tj <App> ili <CompanyPage>) moraju biti inside <Outlet /> u definicije parenta ( App / CompanyPage). 
    Nijedna route se ne pise sa "/" na pocetku, a razlog pogledaj u Sidebar.tsx - zato sto u <Link to> ako kucam "/route" onda je to apsolutna ruta pod uslovom da je definisana ovde kao parent ili child route.
   Dok ako kucam "childRoute" to je relativna route koja se nadoveze na current route pod uslovom da je ta relativna route child of current route <Link to="childRoute"> 
    CompanyPage je ProtectedRoute, pa treba login da bih joj pristupio => svi njeni children routes su protected automatski.
*/
export const router = createBrowserRouter([
    {   
        path:"/",         // Base path when opening app (htpp://localhost:3000/)
        element: <App />, // Uvek App.tsx ovde za path:"/"
        children: [       // Gde sve mozemo otici iz pocetne strane. Zbog childer za <App /> , u App.tsx mora <Outlet /> postojati. 
            {path: "", element: <LazyWrapper><HomePage/></LazyWrapper>},                                          // http://localhost:3000/
            {path: "search", element:<LazyWrapper><ProtectedRoute><SearchPage /></ProtectedRoute></LazyWrapper>}, // http://localhost:3000/search
            {path: "design-guide", element: <LazyWrapper><DesignGuide /></LazyWrapper>},                          // http://localhost:3000/design-guide
            {path: "login", element: <LazyWrapper><LoginPage /></LazyWrapper>},                                   // http://localhost:3000/login
            {path: "register", element: <LazyWrapper><RegisterPage /></LazyWrapper>},                             // http://localhost:3000/register
            {path: "forgot-password", element: <LazyWrapper><ForgotPasswordPage /></LazyWrapper>},                // http://localhost:3000/forgot-password
            {path: "reset-password", element: <LazyWrapper><ResetPasswordPage /></LazyWrapper>},                  // http://localhost:3000/reset-password
            {path: "company/:ticker",                   
             element:  <LazyWrapper><ProtectedRoute><CompanyPage /></ProtectedRoute></LazyWrapper>,               // http://localhost:3000/company/:ticker
             children: [{path:"company-profile", element: <LazyWrapper><CompanyProfile /></LazyWrapper>},         // http://localhost:3000/company/:ticker/company-profile
                        {path:"income-statement", element: <LazyWrapper><IncomeStatement /></LazyWrapper>},       // http://localhost:3000/company/:ticker/income-statement
                        {path:"balance-sheet", element: <LazyWrapper><BalanceSheet /></LazyWrapper>},             // http://localhost:3000/company/:ticker/balance-sheet
                        {path:"cashflow", element: <LazyWrapper><Cashflow /></LazyWrapper>}                       // http://localhost:3000/company/:ticker/cashflow
             ], 
            } 
        ],
    },
]);
/* Zbog children routes za CompanyPage (koje su takodje ProtectedRoute kao CompanyPage), CompanyPage.tsx tj CompanyDashboard.tsx (glavni element za CompanyPage) mora imati children: React.ReactNode u Props i <Outlet />, jer kad pristupam nekoj child route 
of CompanyPage, prvo se renderuje CompanyPage i CompanyDashboard u njoj, pa onda pomocu <Outlet ... /> u CompanyDashboard se renderuje zeljeni child route kad u Sidebar kliknem Company Profile/ Income Statement / Balance Sheet / Cashflow. 
   Isto tako <Outlet> u App.tsx renderuje sve child routes of App. 
   U child route of CompanyPage, ako zelimo da dohvatimo context iz <Outlet context=...> from parent route (CompanyPage tj CompanyDashboard) moram koristiti useOutletContext da dohvatim context vrednost.  
   Zbog nemanja "path: "", element: <CompanyProfileDashboardPage />" u children za <CompanyPage />, na svaku child route of CompanyPage, bice pristuno sve iz <CompanyPage /> + pojedinosti za tog child route. Dok ovaj path:"" za App ima i tamo to nije slucaj. */