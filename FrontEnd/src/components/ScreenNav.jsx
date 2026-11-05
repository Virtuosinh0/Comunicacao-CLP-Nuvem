import { useScreen } from '../services/ScreenService.jsx'
import logoAnjo from '../assets/logo-anjo.svg'

/** Barra superior: logo, abas das telas e anterior/próxima. */
export default function ScreenNav() {
  const { screens, currentId, goTo, next, prev } = useScreen()

  return (
    <header className="screen-nav">
      <div className="screen-nav__row">
        <div className="screen-nav__brand">
          <img className="screen-nav__logo" src={logoAnjo} alt="Anjo Tintas" />
          <span className="screen-nav__brand-sep" aria-hidden="true" />
          <span>Produção</span>
        </div>

        <div className="screen-nav__controls">
          <button type="button" className="screen-nav__icon" onClick={prev} aria-label="Tela anterior" title="Tela anterior (←)">
            ‹
          </button>
          <button type="button" className="screen-nav__icon" onClick={next} aria-label="Próxima tela" title="Próxima tela (→)">
            ›
          </button>
        </div>
      </div>

      {/* Abas em formato de pasta, encaixadas na base da barra (como no site da Anjo). */}
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
    </header>
  )
}
