/* Klikom na "Click to crach app and test ErrorBoundary" dugme testiram ovu component, stoga aktivirace se greska koja ce da aktivira ErrorBoundary 
  
  Ocu da bude prisutna na svakoj stranici aplikacije i zato je u App.tsx stavljam + nisam je stavio u Routes.tsx 
*/

import { useState } from "react";

type Props = {}

const BuggyComponent = (props: Props) => {

  const [shouldThrow, setShouldThrow] = useState(false);

  if (shouldThrow) {
    throw new Error("User triggered error!"); // Aktivirace ErrorBoundary, jer ErrorBoundary built-in reaguje na ovo + okruzuje BuggyComponent u App.tsx
  }

  // Kliknom na ovo dugme => useState promeni vrednost => re-render => ocitava se opet BuggyComponent koji baca Error => aktivira ErrorBoundary jer je ovo child of ErrorBoundary
  return (
    <button onClick={() => setShouldThrow(true)}> 
      Click to crash app and test ErrorBoundary
    </button>
  );
};

export default BuggyComponent;

