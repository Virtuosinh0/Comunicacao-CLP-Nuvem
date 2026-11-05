import Producao from '../pages/Producao/Producao.jsx'
import Moagem from '../pages/Moagem/Moagem.jsx'

/**
 * Telas da aplicação. Para adicionar uma tela nova, crie o componente em src/pages
 * e inclua uma entrada aqui.
 *
 * - id:         usado na URL (#/id)
 * - title:      texto da aba
 * - component:  componente React da tela
 *
 * A primeira tela é a inicial (abre quando a URL não indica outra).
 */
export const SCREENS = [
  { id: 'producao', title: 'Produção', component: Producao },
  { id: 'moagem', title: 'Moagem', component: Moagem },
]
