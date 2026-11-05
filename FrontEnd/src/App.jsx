import { ScreenProvider } from './services/ScreenService.jsx'
import { DEFAULT_ROTATION_SECONDS, SCREENS } from './config/screens.js'
import ScreenNav from './components/ScreenNav.jsx'
import ScreenView from './components/ScreenView.jsx'

function App() {
  return (
    <ScreenProvider screens={SCREENS} defaultIntervalSeconds={DEFAULT_ROTATION_SECONDS}>
      <div className="app">
        <ScreenNav />
        <ScreenView />
      </div>
    </ScreenProvider>
  )
}

export default App
