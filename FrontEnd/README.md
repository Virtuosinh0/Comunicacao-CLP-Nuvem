# FrontEnd — Web App React

React + Vite. Exibe os painéis de Revisão de Produção e Moagem.

```
public/          # Arquivos estáticos
src/
├── api/         # Chamadas aos endpoints GET da API
├── assets/      # Imagens, ícones
├── components/
│   ├── ScreenNav.jsx   # Abas, anterior/próxima e controle da troca automática
│   └── ScreenView.jsx  # Renderiza a tela atual
├── config/
│   └── screens.js      # Lista de telas e intervalos da troca automática
├── hooks/       # Hooks (ex.: polling periódico dos dados)
├── pages/
│   ├── Home/      # Tela inicial
│   ├── Producao/  # Tela de Revisão de Produção (OPs/Reatores)
│   └── Moagem/    # Tela de Moagem (Moinhos)
├── services/
│   └── ScreenService.jsx  # Serviço de troca de tela (ScreenProvider + useScreen)
├── types/       # Tipos/interfaces dos payloads
└── styles/      # CSS global
```

## Troca de tela

O `ScreenService` controla qual tela está visível:

- **Manual:** abas no topo, botões ‹ › ou setas ← → do teclado.
- **Automática:** marque "Troca automática" e escolha o intervalo. Alterna entre as telas com `inRotation: true` (a tela Início fica de fora). Qualquer troca manual reinicia a contagem, e a barra azul no topo mostra o tempo restante.
- **Link direto:** a tela atual fica na URL (`#/producao`, `#/moagem`), útil para deixar uma TV abrindo direto num painel.
- A preferência de troca automática fica salva no navegador.

Em qualquer componente:

```jsx
import { useScreen } from '../services/ScreenService.jsx'

const { currentId, goTo, next, prev, rotation, setAutoRotate } = useScreen()
goTo('moagem')
```

**Adicionar uma tela nova:** crie o componente em `src/pages/` e inclua uma entrada em `src/config/screens.js`.

## Como rodar

Requisitos: **Node.js 20+**.

```bash
cd FrontEnd
npm install
npm run dev
```

Acesse `http://localhost:5173`. A API precisa estar rodando em `http://localhost:8080`.

## Dados da API

- Em desenvolvimento, o Vite repassa `/api/...` para `http://localhost:8080` (proxy em `vite.config.js`), então a API não precisa de CORS. Outra API: `VITE_API_PROXY=http://host:porta npm run dev`.
- Em produção, se a API estiver em outro endereço: `VITE_API_BASE_URL=https://api.exemplo.com npm run build`.
- A tela **Revisão de Produção** busca `GET /api/v1/production/reactors` a cada 5 s (mesmo ciclo do DataManager).

Para não exibir dado desatualizado como se fosse atual:

| Situação | Na tela |
|---|---|
| API fora do ar / erro | Aviso âmbar com o horário da falha; os cards ficam esmaecidos com os dados da última resposta |
| Tanque `stale` (API sem leitura nova do CLP) | Card cinza com selo "Sem atualização" + aviso no topo |
| Campo ausente | `—` (nunca 0) |
| Produzido acima do programado | Percentual > 100% e "Acima do programado: X kg" |
| Sem quantidade programada | Sem barra e sem percentual |

O `status` da OP é traduzido pela tabela em `src/config/opStatus.js` (00 Programado … 11 Finalizada). Código fora da tabela aparece como "Status N (desconhecido)".

Tema: branco + vermelho `rgb(255, 32, 32)` (variáveis no topo de `src/styles/global.css`). Alertas usam âmbar para não se confundirem com o vermelho da marca.
