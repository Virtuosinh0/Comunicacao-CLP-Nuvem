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

A API sobe em `http://localhost:8080` (ainda sem endpoints).
