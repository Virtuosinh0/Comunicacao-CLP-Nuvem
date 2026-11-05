<#
  Instala/atualiza os Serviços Windows do Comunicacao-CLP-Nuvem:
    ClpApi       API Spring Boot + tela (FrontEnd compilado) em http://localhost:8080
    DataManager  Lê os CLPs (SOMENTE LEITURA) e envia para a API

  Etapa 1 (usuário normal): compila tudo em Instalacao\publish
  Etapa 2 (administrador, pede UAC): para os serviços, copia para C:\Automatec\Servicos,
          cria os serviços se não existirem e inicia.

  Rodar de novo depois de alterar o código = atualizar os serviços.
#>
param(
    [switch]$Instalar,      # uso interno: etapa 2 (já elevada)
    [string]$JavaExe = ""
)

$ErrorActionPreference = 'Stop'
$Repo     = Split-Path $PSScriptRoot -Parent
$Publish  = Join-Path $PSScriptRoot 'publish'
$Destino  = 'C:\Automatec\Servicos'
$ApiUrl   = 'http://localhost:8080'

function Etapa($texto) { Write-Host "`n=== $texto ===" -ForegroundColor Cyan }
function Executar($exe, [string[]]$argumentos) {
    & $exe @argumentos
    if ($LASTEXITCODE -ne 0) { throw "Falhou: $exe $($argumentos -join ' ')" }
}

# ------------------------------------------------------------------ Etapa 1: compilar
if (-not $Instalar) {
    $tools = Join-Path $env:USERPROFILE 'tools'
    $mvn   = Join-Path $tools 'apache-maven-3.9.9\bin\mvn.cmd'
    $env:Path = (Join-Path $tools 'node-v22.12.0-win-x64') + ';' + $env:Path

    if (Test-Path $Publish) { Remove-Item $Publish -Recurse -Force }

    Etapa 'DataManager'
    Executar dotnet @('publish', "$Repo\DataManager\src\DataManager.Service", '-c', 'Release', '-o', "$Publish\DataManager", '--nologo', '-v', 'q')

    Etapa 'Hospedeiro da API'
    Executar dotnet @('publish', "$PSScriptRoot\ApiHost", '-c', 'Release', '-o', "$Publish\Api", '--nologo', '-v', 'q')

    Etapa 'API (Maven, com testes)'
    Push-Location "$Repo\BackEnd"
    try { Executar $mvn @('-B', '-ntp', '-q', 'package') } finally { Pop-Location }
    $jar = Get-ChildItem "$Repo\BackEnd\target\*.jar" -Exclude '*plain*' | Select-Object -First 1
    Copy-Item $jar.FullName "$Publish\Api\clp-nuvem-api.jar"

    Etapa 'FrontEnd'
    Push-Location "$Repo\FrontEnd"
    try { Executar npx @('vite', 'build', '--outDir', "$Publish\Api\static", '--emptyOutDir') } finally { Pop-Location }

    # Java usado pela API (caminho fixo: o serviço roda como SYSTEM, sem o PATH do usuário).
    # java escreve isso no stderr; via cmd para o PowerShell 5.1 não tratar como erro.
    $javaHome = (cmd /c 'java -XshowSettings:properties -version 2>&1' | Select-String 'java.home = (.+)').Matches[0].Groups[1].Value.Trim()
    $javaExe  = Join-Path $javaHome 'bin\java.exe'
    if (-not (Test-Path $javaExe)) { throw "java.exe não encontrado em $javaHome" }

    Etapa 'Instalando (pede permissão de administrador)'
    $p = Start-Process powershell -Verb RunAs -Wait -PassThru -ArgumentList @(
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"", '-Instalar', '-JavaExe', "`"$javaExe`"")
    if ($p.ExitCode -ne 0) { throw "Instalação falhou (código $($p.ExitCode))." }
    Write-Host "`nPronto. Tela: $ApiUrl" -ForegroundColor Green
    return
}

# ------------------------------------------------------------------ Etapa 2: instalar (admin)
try {
    $servicos = @(
        @{ Nome = 'ClpApi'; Exibicao = 'CLP Nuvem - API'; Pasta = 'Api'; Exe = 'ClpApi.Host.exe'
           Descricao = "API de produção + tela em $ApiUrl"
           Ambiente  = @("Java__JavaExe=$JavaExe") },
        @{ Nome = 'DataManager'; Exibicao = 'CLP Nuvem - DataManager'; Pasta = 'DataManager'; Exe = 'DataManager.Service.exe'
           Descricao = 'Lê as OPs dos CLPs (somente leitura) e envia para a API'
           Ambiente  = @("Api__BaseUrl=$ApiUrl") }
    )

    Etapa 'Parando serviços e versões antigas'
    foreach ($nome in 'DataManager', 'ClpApi') {
        if (Get-Service $nome -ErrorAction SilentlyContinue) { Stop-Service $nome -Force }
    }
    # Instâncias abertas fora do serviço (janelas "CLP - ...", VS Code, Vite).
    Get-Process DataManager.Service -ErrorAction SilentlyContinue | Stop-Process -Force
    foreach ($porta in 8080, 5173) {
        Get-NetTCPConnection -LocalPort $porta -State Listen -ErrorAction SilentlyContinue |
            ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
    }
    Start-Sleep 2

    foreach ($s in $servicos) {
        Etapa "Serviço $($s.Nome)"
        $pasta = Join-Path $Destino $s.Pasta
        # /MIR espelha a pasta publicada; logs e configuração local da API (config\) são preservados.
        robocopy (Join-Path $Publish $s.Pasta) $pasta /MIR /XD logs config /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy falhou para $pasta ($LASTEXITCODE)" }

        # API: IPs autorizados desta instalação. Criado só na primeira vez; depois é seu (não é sobrescrito).
        $configApi = Join-Path $pasta 'config\application.properties'
        if ($s.Nome -eq 'ClpApi' -and -not (Test-Path $configApi)) {
            New-Item (Split-Path $configApi) -ItemType Directory -Force | Out-Null
            Set-Content $configApi -Encoding ASCII -Value @(
                '# Configuracao local da API (sobrepoe o application.properties de dentro do jar).',
                '# Alterou? Reinicie o servico ClpApi (Reiniciar-Servicos-CLP.bat).',
                '#',
                '# IPs autorizados: IP (192.168.0.10) ou faixa CIDR (192.168.0.0/24), separados por virgula.',
                '# Fora da lista -> 403 e "ACESSO BLOQUEADO" no log da API.',
                '#',
                '# Quem pode ENVIAR leituras (DataManager):',
                'security.allowed-ips.gateway=127.0.0.1,::1',
                '# Quem pode VER a tela (PCs, TVs). Ex.: 127.0.0.1,::1,192.168.0.50,192.168.0.51',
                'security.allowed-ips.viewers=127.0.0.1,::1')
            Write-Host "Criado: $configApi (IPs autorizados)"
        }

        $exe = Join-Path $pasta $s.Exe
        if (-not (Get-Service $s.Nome -ErrorAction SilentlyContinue)) {
            New-Service -Name $s.Nome -BinaryPathName "`"$exe`"" -DisplayName $s.Exibicao `
                -Description $s.Descricao -StartupType Automatic | Out-Null
            Write-Host "Criado."
        }
        # Se cair: reinicia após 10 s (sempre).
        sc.exe failure $s.Nome reset= 86400 actions= restart/10000/restart/10000/restart/10000 | Out-Null
        # Variáveis de ambiente só deste serviço.
        Set-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$($s.Nome)" -Name Environment -Type MultiString -Value $s.Ambiente

        Start-Service $s.Nome
        Write-Host "Rodando: $exe"
    }

    Etapa 'Painel de Controle (Programas e Recursos)'
    # Registra como programa instalado: aparece com ícone, editor LSC Automation e botão Desinstalar.
    Copy-Item (Join-Path $PSScriptRoot 'desinstalar-servicos.ps1') $Destino -Force
    $dmExe   = Join-Path $Destino 'DataManager\DataManager.Service.exe'
    $versao  = (Get-Item $dmExe).VersionInfo.ProductVersion -replace '\+.*$', ''
    $tamanho = [int]((Get-ChildItem $Destino -Recurse -File | Measure-Object Length -Sum).Sum / 1KB)
    $chave   = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClpNuvem'
    New-Item $chave -Force | Out-Null
    $valores = @{
        DisplayName     = 'DataManager - CLP Nuvem'
        DisplayVersion  = $versao
        Publisher       = 'LSC Automation'
        DisplayIcon     = "$dmExe,0"
        InstallLocation = $Destino
        InstallDate     = (Get-Date -Format 'yyyyMMdd')
        Comments        = "Serviços DataManager (leitura dos CLPs) e ClpApi (API + tela em $ApiUrl)"
        UninstallString = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$Destino\desinstalar-servicos.ps1`""
    }
    foreach ($v in $valores.GetEnumerator()) { Set-ItemProperty $chave -Name $v.Key -Value $v.Value }
    Set-ItemProperty $chave -Name EstimatedSize -Value $tamanho -Type DWord
    Set-ItemProperty $chave -Name NoModify -Value 1 -Type DWord
    Set-ItemProperty $chave -Name NoRepair -Value 1 -Type DWord
    Write-Host "Registrado: DataManager - CLP Nuvem $versao (LSC Automation)"

    Get-Service ClpApi, DataManager | Format-Table Name, DisplayName, Status, StartType -AutoSize
    Read-Host 'Enter para fechar'
    exit 0
}
catch {
    Write-Host $_ -ForegroundColor Red
    Read-Host 'Enter para fechar'
    exit 1
}
