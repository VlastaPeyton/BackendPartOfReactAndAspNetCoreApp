import GoogleMap from '../../Components/GoogleMap/GoogleMap';
import Hero from '../../Components/Hero/Hero'

type Props = {}

const HomePage = (props: Props) => {
  return (
    <div>
      <Hero /> 
      <GoogleMap />
    </div>
  )
}

export default HomePage;