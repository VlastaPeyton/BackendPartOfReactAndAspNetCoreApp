import React from 'react'
import hero from "./hero.jpg";
import { Link } from 'react-router-dom';

type Props = {}

const Hero = (props: Props) => {
    return (
      <section id="hero">
        <div className="container flex flex-col-reverse mx-auto p-8 lg:flex-row">
          
          <div className="flex flex-col space-y-10 mb-44 m-10 lg:m-10 xl:m-20 lg:mt:16 lg:w-1/2 xl:mb-52">
            <h1 className="text-5xl font-bold text-center lg:text-6xl lg:max-w-md lg:text-left">
              Financial data with no news i ovo je h1 u html.
            </h1>
            <p className="text-2xl text-center text-gray-400 lg:max-w-md lg:text-left">
              Search relevant financial documents i ovo je paragraf u html.
            </p>
          </div>
          
          <div className="mb-24 mx-auto md:w-180 md:px-10 lg:mb-0 lg:w-1/2">
            <img src={hero} alt="" />
          </div>
          
        </div>
      </section>
    );
  };
  

export default Hero;

/* In HTML, <section> requires <h1>,<h2>....
  <section id="hero"> je dobra praksa za top level section + mogu koristiti <a href="#hero">NekiText</a> tako da klikom na NekiText baci me na sectiod with id="hero". 
  <div className="container flex flex-col-reverse mx-auto p-8 lg:flex-row"> je glavni div koji sadrzi sliku i ove tekstove :
    container = Sets a responsive fixed-width container with automatic horizontal padding
    flex = Applies Flexbox layout to the div. Potrebno zbog flex-col-reverse
    mx-auto	= Sets horizontal margins to auto, centering the container
    lg:flex-row	+ flex-col-reverse = na large screen textovi i slika horizontalno, a na small screen(kad suzim window), prvo slika, pa tekstovi.
    
*/