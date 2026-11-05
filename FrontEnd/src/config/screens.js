import Home from '../pages/Home/Home.jsx'
import Producao from '../pages/Producao/Producao.jsx'
import Moagem from '../pages/Moagem/Moagem.jsx'

/**
 * Telas da aplicação. Para adicionar uma tela nova, crie o componente em src/pages
 * e inclua uma entrada aqui.
 *
 * - id:         usado na URL (#/id)
 * - title:      texto da aba
 * - component:  componente React da tela
 * - inRotation: participa da troca automática
 */
export const SCREENS = [
  { id: 'inicio', title: 'Início', component: Home, inRotation: false },
  { id: 'producao', title: 'Revisão de Produção', component: Producao, inRotation: true },
  { id: 'moagem', title: 'Moagem', component: Moagem, inRotation: true },
]

/** Intervalo padrão da troca automática, em segundos. */
export const DEFAULT_ROTATION_SECONDS = 30

/** Opções exibidas no seletor de intervalo, em segundos. */
export const ROTATION_INTERVAL_OPTIONS = [10, 15, 30, 60, 120, 300]
