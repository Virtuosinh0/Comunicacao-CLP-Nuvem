# BackEnd — API Java Spring Boot (Nuvem)

Java 17 + Spring Boot. Recebe a telemetria do DataManager e expõe as consultas para o FrontEnd.

Estrutura no mesmo padrão do projeto Jewelry-API (pacotes no plural, separados por domínio):

```
src/main/java/com/clpnuvem/api/
├── configurations/   # AppConfig (beans), TelemetryProperties, GlobalExceptionHandler (erros → JSON)
├── controllers/
│   ├── telemetry/    # TelemetryController   POST /api/v1/telemetry/reactors[/batch]
│   └── production/   # ProductionController  GET  /api/v1/production/reactors
├── dtos/
│   ├── common/       # ErrorResponseDto { "message": "..." } (todo erro 400/403)
│   ├── telemetry/    # *RequestDto: o que o DataManager envia
│   └── production/   # *ResponseDto: o que a tela recebe
├── mappers/reactor/  # ReactorMapper: DTO ↔ modelo (só copia/calcula, sem regra de aceitar/rejeitar)
├── models/reactor/   # ReactorState: estado atual de um tanque
├── repositories/reactor/  # ReactorStateRepository: estado em memória
├── services/
│   ├── telemetry/    # TelemetryService: regras de recebimento (contra dado falso)
│   └── production/   # ProductionService: lista de OPs ativas + "stale"
└── infra/security/   # IPs autorizados: SecurityConfigurations, SecurityFilter, handlers de 403
src/main/resources/application.properties
src/test/java/        # Testes (MockMvc)
```

## Como rodar

Requisitos: **JDK 17+** e **Maven** (ou abrir a pasta no IntelliJ / VS Code com Extension Pack for Java).

```bash
cd BackEnd
mvn spring-boot:run
```

A API sobe em `http://localhost:8080`. Testes: `mvn test`.

## Endpoints

| Método | URL | Uso | Quem pode |
|---|---|---|---|
| `POST` | `/api/v1/telemetry/reactors` | **Uma OP** por requisição (DataManager `Api:ReactorMode = Single`) | lista `gateway` |
| `POST` | `/api/v1/telemetry/reactors/batch` | **Todas as OPs de um CLP** em uma requisição (DataManager `Api:ReactorMode = Batch`) | lista `gateway` |
| `GET`  | `/api/v1/production/reactors` | Tanques com OP ativa (para o Web App) | lista `viewers` |
| `GET`  | `/`, `/assets/**`, ícones | Tela compilada, quando servida pela API (Serviço Windows) | lista `viewers` |

Os POST respondem `204 No Content`; payload inválido → `400` com `{"message": "..."}`.
Qualquer outra rota/método → `403`.

- Tanque sem OP chega como `{"reactor_id":"R211","op_code":""}` e não aparece no GET.
- O estado é mantido **por CLP + tanque** (o R280 existe no CLP1 e no CLP2).
- Estado em memória: ao reiniciar a API, volta a ser preenchido no próximo ciclo do DataManager (5 s).

### Regras contra dado falso

| Situação | Comportamento |
|---|---|
| DataManager/CLP parou de enviar | O tanque continua no GET com a última leitura, mas com `"stale": true` após `telemetry.stale-after` (30 s, relógio da API) |
| Tanque não veio no lote | Não é apagado: fica com a última leitura e vira `stale` |
| Leitura mais antiga chega depois de uma mais nova | Descartada (log de aviso), não sobrescreve |
| `timestamp` no futuro além de `telemetry.max-clock-skew` (5 min) | `400`: relógio do gateway errado |
| Envio individual sem `timestamp` ou `plc_id` | `400`: a API não inventa hora nem origem |
| Produzido acima do pedido | `remaining_weight_kg` negativo (não é cortado em 0) |
| PC que só deveria ver a tela tenta enviar leitura | `403`: enviar exige IP da lista `gateway` |

`last_update` = hora da leitura no CLP (relógio do gateway).

## Segurança: só IPs previamente autorizados

Sem login/senha. Toda requisição passa por duas camadas (`infra/security`):

1. **SecurityFilter:** o IP de origem precisa estar na lista certa:
   - `security.allowed-ips.gateway`: quem pode **enviar** leituras (o DataManager);
   - `security.allowed-ips.viewers`: quem pode **ver** a tela e consultar.
2. **SecurityConfigurations:** só as rotas da tabela acima existem; o resto é `403` (`anyRequest().denyAll()`).

Bloqueios aparecem no log como `ACESSO BLOQUEADO` com IP, método e URL.

| Onde configurar | Como |
|---|---|
| Padrão (`application.properties`) | Só este computador: `127.0.0.1,::1` nas duas listas |
| Variáveis de ambiente | `CLP_API_GATEWAY_IPS` e `CLP_API_VIEWER_IPS` (ex.: `127.0.0.1,::1,192.168.0.0/24`) |
| Serviço Windows instalado | `C:\Automatec\Servicos\Api\config\application.properties` (criado pelo instalador, preservado nas atualizações). Depois de alterar, reinicie o serviço `ClpApi`. |

Cada item: IP (`192.168.0.10`) ou faixa CIDR (`192.168.0.0/24`), IPv4 ou IPv6. Item inválido impede a API de subir (não libera nada por engano).

**MAC address não é possível filtrar na API.** O MAC só existe dentro da rede local: depois do primeiro roteador
(e sempre na nuvem) a API recebe apenas o IP. Para identificar o gateway além do IP, o caminho seria um
token/chave no cabeçalho ou certificado de cliente (mTLS).

**Proxy/balanceador na frente da API (nuvem):** o IP usado é o da conexão (`getRemoteAddr()`); o cabeçalho
`X-Forwarded-For` é ignorado de propósito (qualquer um pode forjá-lo). Se a API ficar atrás de um proxy,
será preciso configurar o proxy como confiável (`server.forward-headers-strategy`) — senão todos parecerão vir do IP do proxy.

Ainda falta: endpoint de moinhos.
