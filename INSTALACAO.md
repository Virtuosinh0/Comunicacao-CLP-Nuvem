# Instalação — Ferramentas, Extensões e Bibliotecas

Tudo o que precisa estar instalado para desenvolver e rodar os três módulos do projeto.

| Módulo        | Pasta          | Tecnologia                         |
|---------------|----------------|------------------------------------|
| API em nuvem  | `BackEnd/`     | Java 17 + Spring Boot 3.5          |
| Gateway local | `DataManager/` | C# .NET 8 (Serviço Windows) + Sharp7 |
| Web App       | `FrontEnd/`    | React 19 + Vite 6                  |

---

## 1. Ferramentas (SDKs e runtimes)

| Ferramenta | Versão | Usada em | Download | Verificar |
|---|---|---|---|---|
| **TIA Portal** | **V21** | CPUs S7-1500 | Siemens (licenciado) | abrir o projeto do CLP |
| **Git** | qualquer recente | todos | https://git-scm.com/downloads | `git --version` |
| **JDK** | **17 ou superior** (recomendado 21 LTS) | BackEnd | https://adoptium.net (Temurin) | `java -version` |
| **Maven** | 3.6.3 ou superior | BackEnd | https://maven.apache.org/download.cgi | `mvn -v` |
| **.NET SDK** | **8.0** | DataManager | https://dotnet.microsoft.com/download/dotnet/8.0 | `dotnet --list-sdks` |
| **Node.js** | 20 LTS ou superior (npm já incluso) | FrontEnd | https://nodejs.org | `node -v` / `npm -v` |

> **Atenção: Java 8 não funciona.** O Spring Boot 3 exige Java 17+. Se tiver mais de um JDK instalado, confira se `JAVA_HOME` aponta para o 17+.
>
> Dica: o Maven pode ser dispensado se for usado o IntelliJ IDEA, que já traz um Maven embutido.
>
> Dica: o DataManager só roda em **Windows** (é um Serviço Windows).

---

## 2. Extensões do VS Code

### Recomendadas

| Extensão | ID | Para quê |
|---|---|---|
| **Extension Pack for Java** | `vscjava.vscode-java-pack` | Compilar, rodar e depurar Java / Maven |
| **Spring Boot Extension Pack** | `vmware.vscode-boot-dev-pack` | Suporte a Spring Boot e `application.yml` |
| **C# Dev Kit** | `ms-dotnettools.csdevkit` | Compilar, rodar e depurar o DataManager (instala junto a extensão **C#**) |
| **ES7+ React/Redux/React-Native snippets** | `dsznajder.es7-react-js-snippets` | Snippets de componentes React |

### Opcionais

| Extensão | ID | Para quê |
|---|---|---|
| ESLint | `dbaeumer.vscode-eslint` | Análise de código JavaScript |
| Prettier | `esbenp.prettier-vscode` | Formatação de código |
| REST Client | `humao.rest-client` | Testar os endpoints da API com arquivos `.http` |
| XML | `redhat.vscode-xml` | Edição do `pom.xml` |

Instalação pelo terminal:
```bash
code --install-extension vscjava.vscode-java-pack
code --install-extension vmware.vscode-boot-dev-pack
code --install-extension ms-dotnettools.csdevkit
code --install-extension dsznajder.es7-react-js-snippets
```

> Alternativas ao VS Code: **IntelliJ IDEA** (BackEnd) e **Visual Studio 2022** com a carga de trabalho *"Desenvolvimento para desktop com .NET"* (DataManager — abrir `DataManager.sln`).

---

## 3. Bibliotecas por módulo

As bibliotecas **não precisam ser instaladas manualmente**: cada gerenciador de pacotes baixa tudo no primeiro build. A lista abaixo é para referência.

### BackEnd — Maven (`BackEnd/pom.xml`)

| Biblioteca | Versão | Função |
|---|---|---|
| `spring-boot-starter-parent` | 3.5.7 | Base do projeto Spring Boot (gerencia as versões) |
| `spring-boot-starter-web` | (do parent) | API REST + servidor Tomcat embutido |
| `spring-boot-starter-validation` | (do parent) | Validação dos payloads recebidos (`@NotBlank`, `@NotNull`) |
| `spring-boot-starter-security` | (do parent) | Filtro de IPs autorizados e bloqueio de rotas não listadas |
| `spring-boot-starter-test` | (do parent) | Testes (JUnit 5, Mockito, MockMvc) |
| `spring-security-test` | (do parent) | Testes do filtro de segurança |

```bash
cd BackEnd
mvn spring-boot:run
```

### DataManager — NuGet (`DataManager/src/DataManager.Service/DataManager.Service.csproj`)

| Biblioteca | Versão | Função |
|---|---|---|
| `Microsoft.Extensions.Hosting` | 8.0.1 | Host do Worker Service (DI, configuração, logs) |
| `Microsoft.Extensions.Hosting.WindowsServices` | 8.0.1 | Execução como Serviço Windows + log no Visualizador de Eventos |
| `Sharp7` | 1.1.84 | Comunicação S7 com as CPUs Siemens S7-1500 (port C# do **Snap7**, sem DLL nativa) |

```bash
cd DataManager
dotnet restore
dotnet run --project src/DataManager.Service
```

### FrontEnd — npm (`FrontEnd/package.json`)

| Biblioteca | Versão | Tipo | Função |
|---|---|---|---|
| `react` | ^19.1.0 | dependência | Biblioteca de interface |
| `react-dom` | ^19.1.0 | dependência | Renderização no navegador |
| `@fontsource/poppins` | ^5.2.7 | dependência | Fonte Poppins embutida no build (funciona sem internet) |
| `vite` | ^6.3.5 | desenvolvimento | Servidor de desenvolvimento e build |
| `@vitejs/plugin-react` | ^4.4.1 | desenvolvimento | Suporte a React/JSX no Vite |

```bash
cd FrontEnd
npm install
npm run dev
```

---

## 4. Configuração das CPUs S7-1500 (TIA Portal V21)

Vale para as duas CPUs, a CPU 1515-2 PN (CLP1) e a CPU 1516-3 PN (CLP2), configuradas no **TIA Portal V21**.

Necessário para o DataManager conseguir ler os DBs via Sharp7/Snap7:

1. **Propriedades do CLP → Protection & Security → Connection mechanisms:** marcar **"Permit access with PUT/GET communication from remote partner"**.
2. **Propriedades de cada DB lido → Attributes:** desmarcar **"Optimized block access"** (os endereços passam a ser fixos e aparecem na coluna *Offset*).
3. Fazer o download da configuração de hardware e dos DBs para o CLP.
4. Garantir que o gateway alcança o CLP na rede (porta **TCP 102**).

Os endereços (offsets) mostrados no TIA Portal V21 para o UDT da OP precisam bater com os de [OpLayout.cs](DataManager/src/DataManager.Service/Plc/OpLayout.cs). O número do DB (10) e o IP de cada CLP ficam no `appsettings.json` do DataManager.

---

## Checklist rápido

- [ ] Git instalado
- [ ] JDK 17+ instalado e `JAVA_HOME` configurado
- [ ] Maven instalado (ou IntelliJ)
- [ ] .NET 8 SDK instalado
- [ ] Node.js 20+ instalado
- [ ] Extensões do VS Code instaladas
- [ ] CLP com PUT/GET habilitado e DBs sem acesso otimizado
