# 🧪 Acompanhamento de Produção para o Setor Industrial Químico em Nuvem

Este repositório contém a documentação da arquitetura, a definição dos contratos de API REST e a simulação de payloads JSON para a solução de acompanhamento de produção industrial em nuvem, voltada ao setor químico.

## 🛠️ Arquitetura do Sistema

```
[ CLP Siemens ] 
       │
       ▼ (Leitura direta em DBs via S7 Protocol / Snap7)
[ Serviço Local / Gateway (C# ou C++) ]
       │
       ▼ (HTTP POSTs individuais via IP fixo)
[ API Java Spring Boot (Nuvem) ] ─── (Restrição por IP / IPs Autorizados)
       │
       ▼ (HTTP GETs de listas / Polling ou WebSockets)
[ Web App (React) ]
```

### Papel de cada módulo:

1. **CLP Siemens:** Controla os dispositivos físicos do chão de fábrica químico (tanques de mistura, reatores e moinhos).

2. **Serviço Local (C#/C++):** Lê e processa os dados brutos do CLP. Realiza envios individuais via `POST` (uma requisição por OP e uma requisição por Moinho) a partir de um **IP Fixo** do gateway local para a nuvem.

3. **API Centralizadora em Nuvem (Java Spring Boot):** Atua como middleware seguro na nuvem. Recebe os payloads individuais, armazena/mantém o estado atual e expõe rotas de consulta (`GET`) que retornam listas consolidadas para **IPs autorizados**.

4. **Web App (React):** Interface web interativa e responsiva para exibição em tempo real dos painéis (dashboards) de **Revisão de Produção** (reatores/OPs) e **Moagem** (moinhos).

---

## 🔌 Endpoints REST & Payloads JSON

### 1. Ingestão de Dados (Servidor Local ➔ API Spring Boot em Nuvem)

*Origem restrita ao IP Fixo do servidor local.*

#### A. Atualizar dados de uma OP / Reator

* **URL:** `/api/v1/telemetry/reactors`
* **Método:** `POST`
* Um envio por OP. Inclui `plc_id` (o mesmo tanque pode existir em CLPs diferentes). Campos `null` (ex.: `end_time` sem data) são omitidos.
* **Request Body:**
```json
{
  "plc_id": "CLP1",
  "reactor_id": "R300",
  "op_code": "AAA",
  "product_code": "00000-00",
  "status": 2,
  "priority": 1,
  "requested_weight_kg": 6500.0,
  "produced_weight_kg": 4900.0,
  "current_step": 15,
  "start_time": "2026-11-04T08:00:00Z",
  "final_destination": "TQ-10",
  "timestamp": "2026-11-04T20:30:00Z"
}
```

#### A2. Atualizar todas as OPs de um CLP (lote)

* **URL:** `/api/v1/telemetry/reactors/batch`
* **Método:** `POST`
* Um envio por CLP com todos os tanques. Tanque sem OP vai só com `reactor_id` e `op_code: ""`. Substitui o estado do CLP inteiro.
* **Request Body:**
```json
{
  "plc_id": "CLP2",
  "timestamp": "2026-11-05T14:49:07Z",
  "reactors": [
    { "reactor_id": "R210", "op_code": "AEMEKL01001", "product_code": "P62475", "status": 2, "priority": 999,
      "requested_weight_kg": 6500.0, "produced_weight_kg": 3767.4, "current_step": 3,
      "start_time": "2026-11-05T14:14:47Z", "final_destination": "R210" },
    { "reactor_id": "R211", "op_code": "" }
  ]
}
```

#### B. Atualizar dados de um Moinho

* **URL:** `/api/v1/telemetry/mills`
* **Método:** `POST`
* **Request Body:**
```json
{
  "mill_id": "M-200",
  "name": "Moinho 200",
  "production_today_kg": 4500.0,
  "timestamp": "2026-11-04T20:30:00Z"
}
```

---

### 2. Consulta de Dados (Web App React ➔ API Spring Boot em Nuvem)

*Exclusivo para IPs previamente autorizados.*

#### A. Lista de OPs / Reatores em Produção (Tela de Revisão)

Retorna o status atual de todas as OPs/reatores monitorados no processo químico para renderização no dashboard React.

* **URL:** `/api/v1/production/reactors`
* **Método:** `GET`
* **Response Body:**
```json
{
  "total_active_ops": 2,
  "last_update": "2026-11-04T20:30:05Z",
  "reactors": [
    {
      "reactor_id": "R-300",
      "op_code": "AAA",
      "product_code": "00000-00",
      "requested_weight_kg": 6500.0,
      "produced_weight_kg": 4900.0,
      "remaining_weight_kg": 1600.0,
      "current_step": 15,
      "start_time": "2026-11-04T08:00:00Z"
    },
    {
      "reactor_id": "R-301",
      "op_code": "AAB",
      "product_code": "00000-01",
      "requested_weight_kg": 3000.0,
      "produced_weight_kg": 3000.0,
      "remaining_weight_kg": 0.0,
      "current_step": 40,
      "start_time": "2026-11-04T13:15:00Z"
    }
  ]
}
```

#### B. Lista de Moinhos (Tela de Moagem)

Retorna o resumo de produção acumulada do dia de todos os moinhos da planta industrial.

* **URL:** `/api/v1/milling/summary`
* **Método:** `GET`
* **Response Body:**
```json
{
  "unit": "kg",
  "date": "2026-11-04",
  "total_milled_today_kg": 8255.0,
  "mills": [
    {
      "mill_id": "M-200",
      "name": "Moinho 200",
      "production_today_kg": 4500.0
    },
    {
      "mill_id": "M-201",
      "name": "Moinho 201",
      "production_today_kg": 50.0
    },
    {
      "mill_id": "M-202",
      "name": "Moinho 202",
      "production_today_kg": 3705.0
    }
  ]
}
```

---

## 💻 Tecnologias Previstas

* **Chão de Fábrica (OT):** CLP Siemens (S7-1200 / S7-1500)
* **Serviço Local / Gateway:** C# (.NET Core) ou C++ (Snap7)
* **Backend em Nuvem:** Java 17 + Spring Boot (IP Whitelisting Filter)
* **Frontend Web:** React (Vite, JavaScript/TypeScript, Axios, HTML5/CSS3)
