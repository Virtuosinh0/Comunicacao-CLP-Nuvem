@echo off
rem Instala/atualiza os servicos ClpApi e DataManager (pede permissao de administrador).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0instalar-servicos.ps1"
pause
