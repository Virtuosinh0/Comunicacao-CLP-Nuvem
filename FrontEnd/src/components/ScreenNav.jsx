import { useScreen } from '../services/ScreenService.jsx'
import { ROTATION_INTERVAL_OPTIONS } from '../config/screens.js'

function formatInterval(seconds) {
  return seconds < 60 ? `${seconds}s` : `${seconds / 60} min`
}

/** Barra superior: abas das telas, anterior/próxima e controle da troca automática. */
export default function ScreenNav() {
  const { screens, currentId, goTo, next, prev, rotation, setAutoRotate, setIntervalSeconds, cycle } = useScreen()

  const intervalOptions = [...new Set([...ROTATION_INTERVAL_OPTIONS, rotation.intervalSeconds])].sort((a, b) => a - b)

  return (
    <header className="screen-nav">
      <div className="screen-nav__row">
        <nav className="screen-nav__tabs" aria-label="Telas">
          {screens.map((screen) => (
            <button
              key={screen.id}
              type="button"
              className={`screen-nav__tab${screen.id === currentId ? ' is-active' : ''}`}
              aria-current={screen.id === currentId ? 'page' : undefined}
              onClick={() => goTo(screen.id)}
            >
              {screen.title}
            </button>
          ))}
        </nav>

        <div className="screen-nav__controls">
          <button type="button" className="screen-nav__icon" onClick={prev} aria-label="Tela anterior" title="Tela anterior (←)">
            ‹
          </button>
          <button type="button" className="screen-nav__icon" onClick={next} aria-label="Próxima tela" title="Próxima tela (→)">
            ›
          </button>

          <label className="screen-nav__rotate">
            <input type="checkbox" checked={rotation.enabled} onChange={(e) => setAutoRotate(e.target.checked)} />
            Troca automática
          </label>

          <select
            className="screen-nav__interval"
            value={rotation.intervalSeconds}
            onChange={(e) => setIntervalSeconds(e.target.value)}
            aria-label="Intervalo da troca automática"
          >
            {intervalOptions.map((seconds) => (
              <option key={seconds} value={seconds}>
                {formatInterval(seconds)}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="screen-nav__progress" aria-hidden="true">
        {rotation.enabled && (
          <div key={cycle} className="screen-nav__progress-bar" style={{ animationDuration: `${rotation.intervalSeconds}s` }} />
        )}
      </div>
    </header>
  )
}
