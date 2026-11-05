# FrontEnd — Web App React

React + Vite. Exibe o painel de **Revisão de Produção**: um card por tanque com OP ativa.

```
public/          # Ícones do site
src/
├── api/
│   ├── client.js       # fetch + mensagens de erro prontas para exibir
│   └── production.js   # GET /api/v1/production/reactors
├── assets/      # Logo da Anjo Tintas
├── components/
│   ├── ScreenNav.jsx   # Logo, abas e anterior/próxima
│   ├── ApiStatus.jsx   # Indicador de conexão com a API e horário da última resposta
│   ├── Footer.jsx      # Rodapé
│   └── ScreenView.jsx  # Renderiza a tela atual
├── config/
│   ├── screens.js      # Lista de telas (a primeira é a inicial)
│   └── opStatus.js     # Código de status da OP → texto e cor da IHM
├── hooks/
│   └── usePolling.js   # Busca periódica; a próxima só começa quando a anterior termina
├── pages/
│   ├── Producao/  # Tela de Revisão de Produção (tela inicial) e ReactorCard
│   └── Moagem/    # Aba reservada para os moinhos (fora do escopo deste trabalho)
├── services/
│   └── ScreenService.jsx  # Serviço de troca de tela (ScreenProvider + useScreen)
├── utils/
│   └── format.js       # Formatação de kg, datas, duração e percentual (pt-BR)
└── styles/      # CSS global
```

## Troca de tela

O `ScreenService` controla qual tela está visível:

- **Abas** no topo, botões ‹ › ou setas ← → do teclado (no celular, só as abas).
- **Link direto:** a tela atual fica na URL (`#/producao`, `#/moagem`), útil para deixar uma TV abrindo direto num painel.

Em qualquer componente:

```jsx
import { useScreen } from '../services/ScreenService.jsx'

const { currentId, goTo, next, prev } = useScreen()
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

Visual baseado no site da Anjo Tintas: fonte Poppins (pacote `@fontsource/poppins`, embutida no build, funciona sem internet), vermelho `#D01235`, blocos cinza `#F0F0F0`, cantos assimétricos e abas em formato de pasta (variáveis no topo de `src/styles/global.css`). O status da OP usa a paleta da IHM. Alertas usam âmbar para não se confundirem com o vermelho da marca.
