import React from 'react';
import ReactDOM from 'react-dom/client'; // ReactDOM ubacuje React komponente u HTML kako bi mogle da budu prikazane u browseru.
import './index.css';
import reportWebVitals from './reportWebVitals';
import { RouterProvider } from 'react-router-dom';
import { router } from './Routes/Routes'; // createBrowserRouter from "react-router-dom" koji pruza routes i routing
import * as serviceWorkerRegistration from './serviceWorkerRegistration'; // Zbog pravljenja PWA

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement // = <div id="root"></div>, jer React je SPA. root je mesto gde se React app "montira" unutar index.html file-a.
                                                 // MPA (poput .NET MVC) ima .html za svaki route, ali ovde imam gomilu route na 1 .html file i zato je SPA.
);

// Ubacuje React router unutar <div> u public/index.html file koji se ocitava on startup. React router ima lazy loading i code splitting, pa se on app startup samo homepage se ocitava.
root.render(
  <React.StrictMode> 
    <RouterProvider router={router} />
  </React.StrictMode>
);
// StrictMode only runs in development - it's automatically removed in production builds. It doesn't render any visible UI, just adds extra checks.

serviceWorkerRegistration.register(); // Pogledaj PWA.txt

reportWebVitals(); // Ovo je built-in i nisam ga skidao iako mogo sam.

// Da bi razumeo sve, kreni iz App.tsx i Pages foldera, jer citajuci sta ima na kojoj strani, shvatices ceo kod.