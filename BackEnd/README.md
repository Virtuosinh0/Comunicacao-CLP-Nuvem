# BackEnd — API Java Spring Boot (Nuvem)

Java 17 + Spring Boot. Recebe a telemetria do DataManager e expõe as consultas para o FrontEnd.

```
src/main/java/com/clpnuvem/api/
├── config/       # Configurações do Spring (CORS, beans, propriedades)
├── controller/   # TelemetryController (POST) e ProductionController / MillingController (GET)
├── dto/
│   ├── telemetry/   # Payloads recebidos: /api/v1/telemetry/reactors e /mills
│   └── production/  # Respostas: /api/v1/production/reactors e /api/v1/milling/summary
├── model/        # Entidades de domínio (Reactor, Mill)
├── repository/   # Persistência / armazenamento do estado atual
├── service/      # Regras de negócio (ex.: cálculo do remaining_weight_kg)
├── security/     # Filtro de IPs autorizados (whitelist)
└── exception/    # Tratamento global de erros
src/main/resources/  # application.yml
src/test/java/       # Testes
```

## Como rodar

Requisitos: **JDK 17+** e **Maven** (ou abrir a pasta no IntelliJ / VS Code com Extension Pack for Java).

```bash
cd BackEnd
mvn spring-boot:run
```

A API sobe em `http://localhost:8080`. Testes: `mvn test`.

## Endpoints

| Método | URL | Uso |
|---|---|---|
| `POST` | `/api/v1/telemetry/reactors` | **Uma OP** por requisição (DataManager `Api:ReactorMode = Single`) |
| `POST` | `/api/v1/telemetry/reactors/batch` | **Todas as OPs de um CLP** em uma requisição (DataManager `Api:ReactorMode = Batch`) |
| `GET`  | `/api/v1/production/reactors` | Tanques com OP ativa (para o Web App) |

Os POST respondem `204 No Content`; payload inválido → `400`.

- Tanque sem OP chega como `{"reactor_id":"R211","op_code":""}` e não aparece no GET.
- O lote **substitui** o estado do CLP inteiro: tanque que deixou de vir some.
- O estado é mantido **por CLP + tanque** (o R280 existe no CLP1 e no CLP2).
- Estado em memória: ao reiniciar a API, volta a ser preenchido no próximo ciclo do DataManager (5 s).

Ainda falta: filtro de IPs autorizados (`security/`) e endpoint de moinhos.
