// Zbog Sidebar 4 one komponente jer ocu da tabele imaju lepe brojeve 

export const formatLargeMonetaryNumber = (number: number) : any => {
    // Mora any ili string | undefined zbog rekurzije
    if (number < 0) {
      return "-" + formatLargeMonetaryNumber(-1 * number);
    }
    if (number < 1000) {
      return "$" + number;
    } else if (number >= 1000 && number < 1_000_000) {
      return "$" + (number / 1000).toFixed(1) + "K"; // toFixed makes string from number
    } else if (number >= 1_000_000 && number < 1_000_000_000) {
      return "$" + (number / 1_000_000).toFixed(1) + "M";
    } else if (number >= 1_000_000_000 && number < 1_000_000_000_000) {
      return "$" + (number / 1_000_000_000).toFixed(1) + "B";
    } else if (number >= 1_000_000_000_000 && number < 1_000_000_000_000_000) {
      return "$" + (number / 1_000_000_000_000).toFixed(1) + "T";
    }
  };
  
  // Ne mora return type, jer TS ce da svali da je string jer nema rekurzija
  export const formatLargeNonMonetaryNumber = (number: number)  => {
    if (number < 0) {
      return "-" + formatLargeMonetaryNumber(-1 * number);
    }
    if (number < 1000) {
      return number;
    } else if (number >= 1000 && number < 1_000_000) {
      return (number / 1000).toFixed(1) + "K";
    } else if (number >= 1_000_000 && number < 1_000_000_000) {
      return (number / 1_000_000).toFixed(1) + "M";
    } else if (number >= 1_000_000_000 && number < 1_000_000_000_000) {
      return (number / 1_000_000_000).toFixed(1) + "B";
    } else if (number >= 1_000_000_000_000 && number < 1_000_000_000_000_000) {
      return (number / 1_000_000_000_000).toFixed(1) + "T";
    }
  };
  
  export const formatRatio = (ratio: number) => {
    return (Math.round(ratio * 100) / 100).toFixed(2);
  };