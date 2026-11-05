# DataManager — Serviço Windows (Gateway Local)

C# (.NET 8 Worker Service rodando como Serviço Windows). Lê os DBs do CLP Siemens via protocolo S7 usando o **Sharp7** (port oficial em C# do Snap7, sem DLL nativa) e monta um JSON por OP/Reator e um por Moinho, no formato da API.

```
src/DataManager.Service/
├── Config/
│   ├── plc-tags.json      # "JSON CLP": mapa de tags → campos da API
│   ├── PlcOptions.cs      # Conexão com o CLP (seção "Plc" do appsettings.json)
│   └── TagMapOptions.cs   # Estrutura do plc-tags.json
├── Models/
│   ├── PlcTagDefinition.cs  # Uma tag como escrita no JSON (db, format, address, name)
│   ├── PlcTag.cs            # Tag validada (offset, bit, tamanho)
│   ├── PlcDataType.cs       # BOOL, BYTE, WORD, DWORD, INT, DINT, REAL, LREAL, STRING, DTL
│   ├── TagGroup.cs          # N tags que formam 1 JSON da API
│   └── Api/                 # "JSON API": ReactorTelemetry, MillTelemetry
├── Plc/
│   ├── Sharp7PlcReader.cs   # Conexão S7 e leitura dos DBs (1 leitura por DB por grupo)
│   └── PlcValueDecoder.cs   # Bytes (big-endian) → valores .NET
├── Services/
│   ├── TagMapResolver.cs    # Valida o plc-tags.json na inicialização
│   ├── TelemetryMapper.cs   # N valores do CLP → 1 payload da API
│   └── Worker.cs            # Ciclo: conectar → ler → converter → enviar
└── Http/
    ├── ITelemetrySender.cs        # Contrato de envio (POST por reator / por moinho)
    └── LoggingTelemetrySender.cs  # Provisório: só registra o JSON no log
```

## Conversão JSON CLP → JSON API

Cada tag do CLP é descrita por:

| Campo     | Exemplo                 | Descrição |
|-----------|-------------------------|-----------|
| `db`      | `100`                   | Número do DB |
| `format`  | `REAL`                  | `BOOL`, `BYTE`, `WORD`, `DWORD`, `INT`, `DINT`, `REAL`, `LREAL`, `STRING`, `DTL` |
| `address` | `"24"` ou `"2.3"`       | Offset em bytes no DB; `byte.bit` para BOOL |
| `name`    | `"produced_weight_kg"`  | Nome do campo no JSON da API |
| `length`  | `10`                    | Só para STRING: tamanho declarado (`String[10]`). Padrão 254 |

As tags são agrupadas: **N tags de um grupo formam 1 JSON da API**. Os campos fixos (`reactor_id`, `mill_id`, `name`) vêm do próprio grupo, e o `timestamp` é gerado pelo DataManager no momento da leitura.

```jsonc
// Config/plc-tags.json (trecho)
{ "reactorId": "R-300", "tags": [
    { "db": 100, "format": "STRING", "address": "0",  "length": 10, "name": "op_code" },
    { "db": 100, "format": "STRING", "address": "12", "length": 10, "name": "product_code" },
    { "db": 100, "format": "REAL",   "address": "24", "name": "requested_weight_kg" },
    { "db": 100, "format": "REAL",   "address": "28", "name": "produced_weight_kg" },
    { "db": 100, "format": "INT",    "address": "32", "name": "current_step" },
    { "db": 100, "format": "DTL",    "address": "34", "name": "start_time" }
] }
```

vira

```json
{"reactor_id":"R-300","op_code":"AAA","product_code":"00000-00","requested_weight_kg":6500,"produced_weight_kg":4900.1,"current_step":15,"start_time":"2026-11-04T08:00:00Z","timestamp":"2026-11-04T20:30:00Z"}
```

Tags obrigatórias por grupo:
- **Reator:** `op_code`, `product_code`, `requested_weight_kg`, `produced_weight_kg`, `current_step`, `start_time` (DTL)
- **Moinho:** `production_today_kg`

Se o `plc-tags.json` tiver erro (formato inválido, endereço ímpar, tag obrigatória faltando, nome repetido), o serviço **não sobe** e lista todos os problemas.

> No TIA Portal, os DBs lidos precisam estar com **"Optimized block access" desligado** e o CLP com **"Permit access with PUT/GET"** habilitado.

## Configuração

`appsettings.json` → seção `Plc`: `Ip`, `Rack`, `Slot`, `ConnectTimeoutMs`, `PollIntervalSeconds` e `ClockIsUtc` (se o relógio do CLP está em UTC ou no horário local).

## Como rodar

Requisitos: **.NET 8 SDK** (Windows).

Modo console (debug):
```bash
cd DataManager
dotnet run --project src/DataManager.Service
```

Como Serviço Windows (prompt **como Administrador**):
```bat
scripts\install-service.cmd     :: publica, cria e inicia o serviço "DataManager"
scripts\uninstall-service.cmd   :: para e remove o serviço
```

Os logs aparecem no Visualizador de Eventos → Logs do Windows → Aplicativo (origem `DataManager`).
