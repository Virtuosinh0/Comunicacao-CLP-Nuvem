@echo off
REM Para e remove o Servico Windows do DataManager.
REM Executar como Administrador.
sc.exe stop DataManager
sc.exe delete DataManager
