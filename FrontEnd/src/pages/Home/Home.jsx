import { useScreen } from '../../services/ScreenService.jsx'

export default function Home() {
  const { goTo } = useScreen()

  return (
    <section className="page">
      <h1>Acompanhamento de Produção</h1>
      <p className="page__subtitle">Setor Industrial Químico — monitoramento em nuvem</p>

      <div className="cards">
        <button type="button" className="card" onClick={() => goTo('producao')}>
          <h2>Revisão de Produção</h2>
          <p>OPs e reatores em andamento</p>
        </button>
        <button type="button" className="card" onClick={() => goTo('moagem')}>
          <h2>Moagem</h2>
          <p>Produção diária dos moinhos</p>
        </button>
      </div>
    </section>
  )
}
