# Comunicacao-CLP-Nuvem

1. **CLP Siemens:** Dispositivo físico de automação controlando o processo produtivo.
2. **Serviço Local (C#/C++):** Realiza a leitura e escrita direta nos blocos de dados (DBs) do CLP utilizando bibliotecas de comunicação industrial (ex: Snap7).
3. **API Spring Boot:** Centraliza as regras de negócio, gerencia o banco de dados relacional e expõe os endpoints REST / WebSockets.
4. **App Mobile (Kotlin):** Aplicativo nativo Android para acompanhamento dinâmico dos indicadores de desempenho (OEE, contagens e alarmes).

---

## 🔌 Simulação de Endpoints (API REST)

Abaixo estão definidos os contratos preliminares dos endpoints que serão consumidos pelo aplicativo em Kotlin.

### 1. Obter Status Geral da Produção
Retorna as métricas consolidadas da linha de produção atual em tempo real.

* **URL:** `/api/v1/production/status`
* **Método:** `GET`
* **Response (JSON simulado):**
```json
{
  "line_id": "LPP-01",
  "status": "EM_OPERACAO",
  "current_order": "OP-2026-984A",
  "product_name": "Tinta Acrílica Premium 18L",
  "metrics": {
    "target_quantity": 5000,
    "produced_quantity": 3420,
    "rejected_quantity": 45,
    "efficiency_percentage": 91.5,
    "oee": 84.2
  },
  "timestamp": "2026-11-04T20:00:00Z"
}
