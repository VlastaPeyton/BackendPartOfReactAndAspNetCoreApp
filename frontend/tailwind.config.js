/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{js,jsx,ts,tsx}"], /* Scan in any folder any file */
  theme: {
    screens: {
      sm: "480px", // Small devices (mobile phone) >=480px
      md: "768px", // tablets  >= 768px
      lg: "1020px", // laptops >= 1020px
      xl: "1440px", // large desktop >= 1440px
    },
    extend: {
      colors: { // Add custom colors 
        lightBlue: "hsl(215.02, 98.39%, 51.18%)", // hsl= Hue, Saturation & Lightness
        darkBlue: "hsl(213.86, 58.82%, 46.67%)",
        lightGreen: "hsl(156.62, 73.33%, 58.82%)"
      },
      fontFamily: {
        sans: ["Poppins", "sans-serif"],
      },
      spacing: {
        180: "32rem",
      },
    },
  },
  plugins: [],
};

