import { useScreen } from '../services/ScreenService.jsx'

/** Renderiza a tela atual. */
export default function ScreenView() {
  const { current } = useScreen()
  const Screen = current.component

  return (
    <main key={current.id} className="screen-view">
      <Screen />
    </main>
  )
}
