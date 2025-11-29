import React from 'react'

type Props = {}

const GoogleMap = (props: Props) => {

    const navigationUrl = `https://www.google.com/maps/dir/?api=1&destination=$Radnička 9, Beograd 11030`;
    // navigationUrl - ako user klikne "Get directions to our office", otvara se mapa sa odredistem vec upisanim, dok startno odrestiste ja upisujem

    return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-gray-50 p-4">
      <div className="bg-white rounded-xl shadow-lg overflow-hidden max-w-4xl w-full">
        {/* Map Container */}
        <div className="p-6">
          <div className="relative rounded-lg overflow-hidden shadow-md">
            <iframe 
              src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d2831.6738283288305!2d20.413692986228817!3d44.7874545964795!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x475a6fc4bdedd6d3%3A0x2beff75d73a3f02!2sAda%20Mall!5e0!3m2!1sen!2srs!4v1751106391245!5m2!1sen!2srs" 
              width="100%" 
              height="450" 
              style={{ border: 0 }} 
              allowFullScreen 
              loading="lazy" 
              referrerPolicy="no-referrer-when-downgrade"
            />
          </div>
          
          {/* Navigation Button */}
          <div className="mt-6 text-center">
            <button 
              onClick={() => window.open(navigationUrl, '_blank')}
              className="bg-gradient-to-r from-green-500 to-green-600 hover:from-green-600 hover:to-green-700 text-white font-semibold py-4 px-8 rounded-full shadow-lg hover:shadow-xl transform hover:scale-105 transition-all duration-200 focus:outline-none focus:ring-4 focus:ring-green-300"
            >
              Get Directions to Our Office
            </button>
          </div>

        </div>
      </div>
    </div>
  );
}
// referrerPolicy="no-referrer-when-downgrade" - da kazemo Google Maps koji sajt koristi njihovu mapu. Ovo poboljsava analitiku koju Google nam pravi.

export default GoogleMap;