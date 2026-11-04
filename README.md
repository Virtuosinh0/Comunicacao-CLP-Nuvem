# 🏭 Sistema de Monitoramento de Produção em Tempo Real (Industrial IoT & Mobile)

Este repositório contém a documentação da arquitetura, a definição dos contratos de API REST e a simulação de payloads JSON para o sistema de monitoramento de produção industrial.

## 🛠️ Arquitetura do Sistema

```
[ CLP Siemens ] 
       │
       ▼ (Leitura direta em DBs via S7 Protocol / Snap7)
[ Serviço Local / Gateway (C# ou C++) ]
       │
       ▼ (HTTP POSTs individuais via IP fixo)
[ API Java Spring Boot ] ─── (Restrição por IP / IPs Autorizados)
       │
       ▼ (HTTP GETs de listas / Polling)
[ App Mobile (Kotlin) ]
```

### Papel de cada módulo:

1. **CLP Siemens:** Controla os dispositivos físicos do chão de fábrica (tanques/reatores e moinhos).

2. **Serviço Local (C#/C++):** Lê e processa os dados brutos do CLP. Realiza envios individuais via `POST` (uma requisição por OP e uma requisição por Moinho) a partir de um **IP Fixo**.

3. **API Centralizadora (Java Spring Boot):** Atua como middleware seguro. Recebe os payloads individuais, armazena/mantém o estado atual e expõe rotas de consulta (`GET`) que retornam listas consolidadas para **IPs autorizados**.

4. **App Mobile (Kotlin):** Consome as listas de OPs e Moinhos da API para exibição em tempo real nas telas do aplicativo.

---

## 🔌 Endpoints REST & Payloads JSON

### 1. Ingestão de Dados (Servidor Local ➔ API Spring Boot)

*Origem restrita ao IP Fixo do servidor local.*

#### A. Atualizar dados de uma OP / Reator

* **URL:** `/api/v1/telemetry/reactors`
* **Método:** `POST`
* **Request Body:**

```json
{
  "reactor_id": "R-300",
  "op_code": "AAA",
  "product_code": "00000-00",
  "requested_weight_kg": 6500.0,
  "produced_weight_kg": 4900.0,
  "current_step": 15,
  "start_time": "2026-11-04T08:00:00Z",
  "timestamp": "2026-11-04T20:30:00Z"
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

### 2. Consulta de Dados (App Mobile ➔ API Spring Boot)

*Exclusivo para IPs previamente autorizados.*

#### A. Lista de OPs / Reatores em Produção (Tela de Revisão)

Retorna o status atual de todas as OPs/reatores monitorados.

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

Retorna o resumo de produção acumulada do dia de todos os moinhos.

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

* **Chão de Fábrica:** CLP Siemens (S7-1200 / S7-1500)
* **Serviço Local (Gateway):** C# (.NET Core) ou C++ (Snap7)
* **Backend:** Java 17 + Spring Boot (IP Whitelisting Filter)
* **Mobile App:** Kotlin (Android Studio, Retrofit)
