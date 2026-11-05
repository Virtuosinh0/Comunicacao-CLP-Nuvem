# Instalação como Serviços Windows

Dois serviços, com início automático junto com o Windows (não precisa ninguém logado):

| Serviço | Nome exibido | O que faz | Pasta |
|---|---|---|---|
| `ClpApi` | CLP Nuvem - API | API Spring Boot **e a tela** em `http://localhost:8080` | `C:\Automatec\Servicos\Api` |
| `DataManager` | CLP Nuvem - DataManager | Lê as OPs dos CLPs (somente leitura) e envia para a API | `C:\Automatec\Servicos\DataManager` |

Se um serviço cair, o Windows o reinicia após 10 s. O `ClpApi` também reinicia a API (Java) se ela cair sozinha.

## Instalar / atualizar

Dê dois cliques em **`Instalar-Servicos.bat`** (pede permissão de administrador no final).

1. Compila DataManager, API (com testes) e FrontEnd em `Instalacao\publish`.
2. Para os serviços e qualquer instância aberta fora deles (portas 8080/5173, `DataManager.Service.exe`).
3. Copia para `C:\Automatec\Servicos`, cria os serviços se não existirem e inicia.
4. Registra em Programas e Recursos (Painel de Controle).

Autor: **LSC Automation** (empresa, autor e copyright nos executáveis; ícone da Anjo em `anjo.ico`).

Alterou o código? Rode de novo: é o mesmo script para atualizar.

## Configuração de cada serviço

- **DataManager:** `C:\Automatec\Servicos\DataManager\appsettings.json` (CLPs, intervalo). A URL da API vem da variável `Api__BaseUrl=http://localhost:8080`, definida só para o serviço (registro: `HKLM\SYSTEM\CurrentControlSet\Services\DataManager`, valor `Environment`). Os IPs dos CLPs vêm das variáveis de ambiente de **máquina** (`CLP_Anjo`, `CLP_Producao`).
- **ClpApi:** `C:\Automatec\Servicos\Api\appsettings.json` (`Java:Arguments` para argumentos extras da API). O caminho do `java.exe` é gravado na instalação (`Java__JavaExe`).

O `Instalar-Servicos.bat` sobrescreve os `appsettings.json` da pasta de serviços com os do repositório: altere no repositório.

## Logs

- DataManager e hospedeiro da API: **Visualizador de Eventos → Logs do Windows → Aplicativo**, origens `DataManager` e `ClpApi`.
- Saída da API (Spring Boot): `C:\Automatec\Servicos\Api\logs\api-AAAA-MM-DD.log` (14 dias).

## Reiniciar / parar

- Atalho da área de trabalho `Reiniciar-Servicos-CLP.bat`, ou
- `services.msc` / Gerenciador de Tarefas → Serviços.

## Painel de Controle

A instalação aparece em **Programas e Recursos** como **DataManager - CLP Nuvem**, editor **LSC Automation**, com o ícone da Anjo.
O botão **Desinstalar** roda `C:\Automatec\Servicos\desinstalar-servicos.ps1`: remove os dois serviços, a pasta `C:\Automatec\Servicos` e o registro.

## ApiHost

O Java não roda como Serviço Windows sozinho. `ApiHost` é um serviço .NET mínimo que executa
`java -jar clp-nuvem-api.jar`, serve a pasta `static` (FrontEnd compilado) e mantém a API no ar.
O Java fica num Job Object do Windows: morre junto com o hospedeiro, nunca fica órfão segurando a porta 8080.
