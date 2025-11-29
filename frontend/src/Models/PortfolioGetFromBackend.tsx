export type PortfolioGetFromBackend = {
    id: number;
    symbol: string;
    companyName: string;
    purchase: number;
    dividend: number;
    industry: string;
    marketCap: number; 
    comments: any;
    // Nisam preslikao Portfolios iz Stock.cs jer mi ne treba ovde. 
}

/* GetUserPortfolios in Backend vraca listu of Stock koji ima ova polja iznad samo u PascalCase.
    Automatski se mapira Id->id, Symbol->symbol ... iz Backend to here. */