import React from 'react'
import { Outlet } from 'react-router-dom'; // Isto kao u App.tsx gde sam import from 'react-router'

type Props = {
  children: React.ReactNode; /*Da bih mogo 0, 1 ili vise Components (tj Tile.tsx) staviti unutar <CompanyDashboard>
                             Moze i <CompanyDashboard children={<Component/>}...> ali to se ne radi nikad. */
  ticker: string; 
}

// Zbog ovakog Props ce u CompanyPage.tsx da imamo <CompanyDashboard ticker={ticker!}> <Tile...> <TenK><CompanyDashboard>, a ticker! mora jer nije ticker: string | undefined u Props
// Moram imati built-in <Outlet /> koji omogucava nested routes tj children (Tile.tswx) i zato ne prosledjujem Tile kao Prop !
const Dashboard = ({children, ticker}: Props) => {
  return (
    <div className="relative md:ml-64 bg-blueGray-100 w-full">
      <div className="relative pt-20 pb-32 bg-lightBlue-500">
        <div className="px-4 md:px-6 mx-auto w-full">
          <div>
            <div className="flex flex-wrap">{children}</div> {/* U mom slucaju, children je Tile.tsx i TenK.tsx, koji su u CompanyPage napisan izmedju <CompanyDashboard> i </CompanyDashboard> */}
            <div className="flex flex-wrap">{<Outlet context={ticker}/>}</div> {/* Kao u App.tsx sluzi da renderuje zeljenu child route of CompanyPage according to Routes.tsx */}
          </div>
        </div>
      </div>
    </div>
  )
}

export default Dashboard;