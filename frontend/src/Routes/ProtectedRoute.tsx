import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'; 
import { useAuth } from '../Context/useAuthContext';      

type Props = {
    children : React.ReactNode // Zbog component koje ce biti unutar <ProtectedRoute> u Routes.tsx, jer React.ReactNode je 0, 1 ili vise Components koje mogu napisati unutar <ProtectedRoute>
} 
                                          
const ProtectedRoute = ({children} : Props) => {
    
    const location = useLocation(); 
    const {isLoggedIn} = useAuth(); // Moze da pristupi svemu iz Context, jer <ProtectedRoute> u Routes.tsx je child of <App> tj u App.tsx <ProtectedRoute> (i njegova deca) nalazi se u <Outlet/>, a <Outlet/> je unutar <UserProvider>
    
    return (
        isLoggedIn() ? <>{children}</> : <Navigate to="/login"  state={{from: location}} replace/> 
    )
}
/*Navigira nas na login ako pokusam da pristupim nekoj ProtectedRoute (pogledaj Routes.tsx ko je sve unutar <ProtectedRoute>) ako pre toga nisam login.
  Ako sam login, renderuje se {children} route (unutar <ProtectedRoute> u Routes.tsx) kojoj zelim da pristupim. */

export default ProtectedRoute;