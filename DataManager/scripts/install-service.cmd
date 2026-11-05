@echo off
REM Publica e instala o DataManager como Servico Windows.
REM Executar como Administrador.
setlocal
set SERVICE_NAME=DataManager
set PUBLISH_DIR=%~dp0..\publish

dotnet publish "%~dp0..\src\DataManager.Service\DataManager.Service.csproj" -c Release -o "%PUBLISH_DIR%" || exit /b 1

sc.exe create %SERVICE_NAME% binPath= "%PUBLISH_DIR%\DataManager.Service.exe" start= auto DisplayName= "DataManager - Gateway CLP Nuvem"
sc.exe description %SERVICE_NAME% "Le dados do CLP Siemens e envia para a API em nuvem."
sc.exe start %SERVICE_NAME%
endlocal
