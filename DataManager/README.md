# DataManager — Serviço Windows (Gateway Local)

**Autor:** LSC Automation · Copyright © 2026 LSC Automation. All rights reserved

C# (.NET 8 Worker Service rodando como Serviço Windows). Lê os DBs dos CLPs Siemens (CPUs S7-1500) via protocolo S7 usando o **Sharp7** (port oficial em C# do Snap7, sem DLL nativa) e envia as OPs de cada tanque para a API no formato descrito na [Documentação técnica](../DOCUMENTACAO-TECNICA.md). O serviço **só lê** os CLPs, nunca escreve neles.

```
src/DataManager.Service/
├── Config/
│   ├── PlcCatalog.cs      # CLPs e OPs fixos no código (CLP1: 8 OPs, CLP2: 25 OPs)
│   ├── ApiOptions.cs      # Envio para a API (seção "Api" do appsettings.json)
│   ├── plc-tags.json      # Moinhos (fora do escopo deste trabalho)
│   ├── PlcOptions.cs      # Conexão de cada CLP (seção "Plc" do appsettings.json)
│   └── TagMapOptions.cs   # Estrutura do plc-tags.json
├── Models/
│   ├── PlcTagDefinition.cs  # Uma tag como escrita no JSON (db, format, address, name)
│   ├── PlcTag.cs            # Tag validada (offset, bit, tamanho)
│   ├── PlcDataType.cs       # BOOL, BYTE, WORD, DWORD, INT, DINT, REAL, LREAL, STRING, DTL
│   ├── PlcDefinition.cs     # Um CLP + nomes das OPs (ordem do array)
│   ├── OpData.cs            # Uma OP decodificada
│   ├── TagGroup.cs          # N tags que formam 1 JSON da API; PlcStation (CLP pronto p/ leitura)
│   └── Api/                 # "JSON API": ReactorTelemetry, MillTelemetry
├── Plc/
│   ├── Sharp7PlcReader.cs   # Conexão S7 com 1 CLP e leitura dos DBs
│   ├── OpLayout.cs          # Offsets do UDT da OP (160 bytes) e decode
│   └── PlcValueDecoder.cs   # Bytes (big-endian) → valores .NET
├── Services/
│   ├── TagMapResolver.cs    # Valida catálogo + conexões + plc-tags.json na inicialização
│   ├── TelemetryMapper.cs   # Dados do CLP → payloads da API
│   └── Worker.cs            # Ciclo por CLP: conectar → ler → converter → enviar
└── Http/
    ├── ITelemetrySender.cs        # Contrato de envio para a API
    ├── HttpTelemetrySender.cs     # Envia os POSTs para a API (Api:BaseUrl preenchido)
    └── LoggingTelemetrySender.cs  # Só registra no log o JSON que seria enviado (Api:BaseUrl vazio)
```

## OPs / Reatores

As OPs ficam em um **array de UDT** (`Array[0..N-1] of "udtOPx"`) no **DB10** (`dbProducao`) de cada CLP. Os CLPs e os nomes das OPs são **fixos no código** em [`Config/PlcCatalog.cs`](src/DataManager.Service/Config/PlcCatalog.cs) — a ordem dos nomes é a ordem do array (1º nome = `OP[0]`).

| CLP  | CPU | OPs |
|------|-----|-----|
| CLP1 | CPU 1515-2 PN | 8: R300, R301, R302, R311, R312, SB300, R230, R280 |
| CLP2 | CPU 1516-3 PN | 25: R210, R211, R212, R280, B380A, B380B, B380C, R350…R355, R360…R364, R370, R371, R482, AMZ1…AMZ4 |

Layout do UDT (DB **não otimizado**), definido em [`Plc/OpLayout.cs`](src/DataManager.Service/Plc/OpLayout.cs):

| Offset | Campo CLP        | Tipo        | Campo API             |
|-------:|------------------|-------------|-----------------------|
| 0      | `codigoOp`       | String[14]  | `op_code`             |
| 16     | `status`         | Int         | `status`              |
| 18     | `prioridade`     | Int         | *(não lido)*          |
| 20     | `quantidadeSP`   | Real        | `requested_weight_kg` |
| 24     | `quantidadePV`   | Real        | `produced_weight_kg`  |
| 28     | `nomeProduto`    | String[12]  | `product_code`        |
| 42     | `dataStart`      | DTL         | `start_time`          |
| 54     | `dataEnd`        | DTL         | `end_time`            |
| 66     | `destinoFinal`   | String[10]  | *(não lido)*          |
| 90     | `tarefaAtualInt` | Int         | `current_step`        |
| **160** | *(tamanho de 1 OP, inclui campos não lidos)* | | |

O array inteiro é lido em **uma leitura por CLP** (CLP1: 1280 bytes, CLP2: 4000 bytes). O `reactor_id` é o nome da OP no catálogo e o `timestamp` é a hora da leitura, em UTC. DTL zerado ou com o valor inicial `DTL#1970-01-01-00:00:00` vira campo ausente, e um REAL inválido (NaN) também; nunca é enviado `0` no lugar.

Exemplo de envio em lote (um CLP, um tanque com OP e um sem):

```json
{"plc_id":"CLP1","timestamp":"2026-11-05T12:00:00Z","reactors":[
  {"reactor_id":"R301","op_code":"OP12345678901","product_code":"00000-00","status":2,"requested_weight_kg":6500.5,"produced_weight_kg":4900.1,"current_step":15,"start_time":"2026-11-04T08:00:00Z"},
  {"reactor_id":"R302","op_code":""}
]}
```

> Se o UDT mudar no CLP, atualizar os offsets em `OpLayout.cs`. Para incluir/remover OPs, editar `PlcCatalog.cs`.

## Moinhos (plc-tags.json): fora do escopo deste trabalho

> A leitura de moinhos é uma etapa futura combinada com a Anjo Tintas e não faz parte do trabalho acadêmico. A API ainda não tem o endpoint de moinhos, e os endereços no `plc-tags.json` (DB200) são apenas um exemplo.

Cada moinho indica de qual CLP ler (`plc`) e suas tags:

| Campo     | Exemplo                 | Descrição |
|-----------|-------------------------|-----------|
| `db`      | `200`                   | Número do DB |
| `format`  | `REAL`                  | `BOOL`, `BYTE`, `WORD`, `DWORD`, `INT`, `DINT`, `REAL`, `LREAL`, `STRING`, `DTL` |
| `address` | `"0"` ou `"2.3"`        | Offset em bytes no DB; `byte.bit` para BOOL |
| `name`    | `"production_today_kg"` | Nome do campo no JSON da API |
| `length`  | `10`                    | Só para STRING: tamanho declarado (`String[10]`). Padrão 254 |

```jsonc
{ "plc": "CLP1", "millId": "M-200", "name": "Moinho 200", "tags": [
    { "db": 200, "format": "REAL", "address": "0", "name": "production_today_kg" }
] }
```

Tag obrigatória: `production_today_kg`.

Se a configuração tiver erro (CLP sem conexão no appsettings, DB inválido, formato inválido, endereço ímpar, tag obrigatória faltando, nome repetido), o serviço **não sobe** e lista todos os problemas.

> No TIA Portal V21, os DBs lidos precisam estar com **"Optimized block access" desligado** e o CLP com **"Permit access with PUT/GET"** habilitado.

## Configuração

`appsettings.json` → seção `Plc`:

```json
"Plc": {
  "ConnectTimeoutMs": 2000,
  "PollIntervalSeconds": 5,
  "ClockIsUtc": false,
  "Connections": {
    "CLP1": { "Enabled": true, "IpEnvironmentVariable": "CLP_Anjo", "Rack": 0, "Slot": 1, "OpDb": 10, "OpStartAddress": 0 },
    "CLP2": { "Enabled": true, "IpEnvironmentVariable": "CLP_Producao", "Rack": 0, "Slot": 1, "OpDb": 10, "OpStartAddress": 0 }
  }
}
```

- `IpEnvironmentVariable`: nome da variável de ambiente com o IP do CLP, para o IP não ficar gravado no repositório. Também é possível usar `"Ip": "192.168.0.1"` direto.
- `Enabled`: `false` desliga a leitura daquele CLP sem apagar a configuração.
- `OpDb`: número do DB com o array de OPs (10); `OpStartAddress`: offset do início do array no DB (normalmente 0).
- `ClockIsUtc`: se o relógio dos CLPs está em UTC ou no horário local do gateway.
- Cada CLP é lido de forma independente: se um cair, o outro continua sendo lido.

## Envio para a API

`appsettings.json` → seção `Api`:

```json
"Api": { "BaseUrl": "http://localhost:8080", "ReactorMode": "Batch", "TimeoutSeconds": 10 }
```

- `BaseUrl` vazio: não envia, só registra no log o JSON que seria enviado.
- `ReactorMode`: `Batch` = 1 POST por CLP com todas as OPs (`/api/v1/telemetry/reactors/batch`); `Single` = 1 POST por OP (`/api/v1/telemetry/reactors`).
- Tanque com `codigoOp` vazio: os outros campos não são decodificados e o tanque vai só como `{"reactor_id":"R211","op_code":""}`.
- A cada ciclo o log mostra o tempo de cada CLP: `CLP2: 172 ms (leitura 140 ms, conversão 4.7 ms, envio 28 ms em 1 POST) - 2/25 OP(s) ativas`.

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
