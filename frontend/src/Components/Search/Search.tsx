import React, { useState, MouseEvent, SyntheticEvent, ChangeEvent } from 'react'

// Ovako se prosledjuje funkcija u Props
type Props = {
    onSearchSubmit: (e: SyntheticEvent) => void; // SyntheticEvent je univerzalni tip za bilo koji event i najlakse je ovako kad event imam.
    search: string | undefined;
    handleSearchChange: (eTargetValue: string) => void;
}

// Used in SearchPage
const Search = React.memo(({onSearchSubmit, search, handleSearchChange}: Props) => {
   /* React.memo koristim jer Search je child component of SearchPage, a SearchPage rendering logika podrzava React.memo ovde + imam funkcije kao props. 
   Zbog React.memo(), props funkcije moraju biti definisane sa useCallback u SearchPage. 
   Objasnjene je u workflow.txt 
   */
    return (
        /* In <form>, onSubmit function (tj onSearchSubmit) mora imati e.preventDefault() in it's definition jer argument je Event type, 
         dok kod onChange ili onClick (u <form> ili van forme), skoro nikad nema e.preventDefault() jer argument is not Event type.
         Stoga u SearchPage handleSearchChange nece biti e.preventDefault, jer handleSearchChange ne prima argument tipa Event, vec string koji sam uneo u formu. 
         Uvek u formi, bez obzira da li je onSubmit,onChange ili onClick, ako funkcija prima argument, mora {(e) => funkcija(e ili e.target.value)} u zavisnosti da l je
         argument Eventy ili non-Event type. 

          <input> ima onoliko puta koliko inputa imamo u formi. Svaki input mora da ima onChange i value koje dohvata pomocu e.target.value. Br inputs ne zavisi od br argumenata u funkciji
        koju referencira onSubmit, jer najcesce onSubmit function samo Event argument prima, dok inputa imam vise -pogledaj handleSearchChange i onSearchSubmit i bice ti jasno !!!!
        Ali ako ima vise inputs, svaki onChange funtion mora biti razlicit tj za svaki argument poseban onChange function mora ! 
         
         Ispod prikazani slucajevi kad onSearchSubmit nema argument ili ima 1/2 non-Event type argumenta. */
        <div className="flex justify-center items-center my-10 px-4">
            <form
                onSubmit={(e) => onSearchSubmit(e)}
                className="flex flex-col sm:flex-row gap-4 bg-white shadow-lg rounded-lg p-6 w-full max-w-2xl"
            >
                <input
                    value={search}
                    onChange={(e) => handleSearchChange(e.target.value)}
                    placeholder="Enter ticker, not a real name"
                    className="flex-1 px-4 py-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-400 text-lg"
                />
                <button
                    type="submit"
                    className="bg-green-500 hover:bg-green-600 text-white font-semibold px-6 py-3 rounded-md text-lg transition duration-300"
                >
                    Search {/* Kliknem na ovo pokrecem onSearchSubmit u SearchPage.tsx */}
                 </button>
            </form>
       </div>
    )
});

export default Search;
/* 
1) onSearchSubmit nema argument :
    <form onSubmit={(e) => {e.preventDefault(); onSearchSubmit()}}...>

2) onSearchSubmit ima 1 non-Event type argument:
    <form onSubmit={(e) => {e.preventDefault(); onSearchSubmit(argument)}}..>
    

3) onSearchSubmit ima vise non-Event type argumenta:
    <form onSubmit={(e) => {e.preventDefault(); onSearchSubmit(argument1, argument2...)}}..>

 */