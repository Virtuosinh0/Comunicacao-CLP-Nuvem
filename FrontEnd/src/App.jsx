import { ScreenProvider } from './services/ScreenService.jsx'
import { SCREENS } from './config/screens.js'
import ScreenNav from './components/ScreenNav.jsx'
import ScreenView from './components/ScreenView.jsx'
import Footer from './components/Footer.jsx'

function App() {
  return (
    <ScreenProvider screens={SCREENS}>
      <div className="app">
        <ScreenNav />
        <ScreenView />
        <Footer />
      </div>
    </ScreenProvider>
  )
}

export default App
