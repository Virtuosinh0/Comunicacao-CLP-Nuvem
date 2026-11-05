<#
  Remove os Serviços Windows do CLP Nuvem (ClpApi e DataManager), a pasta C:\Automatec\Servicos
  e o registro em Programas e Recursos. Chamado pelo botão "Desinstalar" do Painel de Controle.
  LSC Automation.
#>
$ErrorActionPreference = 'Stop'
$Destino = 'C:\Automatec\Servicos'

# Precisa de administrador: se não for, reabre pedindo permissão (UAC).
$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $admin) {
    Start-Process powershell -Verb RunAs -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    exit
}

try {
    $resposta = Read-Host 'Remover os serviços DataManager e ClpApi e a pasta C:\Automatec\Servicos? (S/N)'
    if ($resposta -notmatch '^[sS]') { Write-Host 'Cancelado.'; Start-Sleep 2; exit 0 }

    foreach ($nome in 'DataManager', 'ClpApi') {
        if (Get-Service $nome -ErrorAction SilentlyContinue) {
            Stop-Service $nome -Force
            sc.exe delete $nome | Out-Null
            Write-Host "Serviço $nome removido."
        }
    }
    Start-Sleep 2

    Remove-Item 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ClpNuvem' -Recurse -ErrorAction SilentlyContinue

    # Este script está dentro da pasta: apaga o resto e deixa um comando agendado para apagá-la depois.
    Get-ChildItem $Destino -Exclude 'desinstalar-servicos.ps1' | Remove-Item -Recurse -Force
    Start-Process cmd -WindowStyle Hidden -ArgumentList '/c', "timeout /t 3 /nobreak >nul & rmdir /s /q `"$Destino`""

    Write-Host 'Desinstalado.' -ForegroundColor Green
    Start-Sleep 3
}
catch {
    Write-Host $_ -ForegroundColor Red
    Read-Host 'Enter para fechar'
    exit 1
}
