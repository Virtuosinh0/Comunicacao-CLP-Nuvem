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

Acesse `http://localhost:5173`.
