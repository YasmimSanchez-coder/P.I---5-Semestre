@echo off
chcp 65001 >nul
title AgendAi - API
cd /d "%~dp0"

echo ==================================================
echo   AgendAi - preparar e iniciar a API
echo ==================================================
echo.

where node >nul 2>nul
if errorlevel 1 goto sem_node

if exist "2-api\.env" goto tem_env

echo [1/3] Criando o arquivo de configuracao 2-api\.env
copy "2-api\.env.example" "2-api\.env" >nul
echo.
echo O Bloco de Notas vai abrir. Preencha estas duas linhas:
echo    DB_PASSWORD = a senha do seu MySQL
echo    JWT_SECRET  = qualquer texto longo, por exemplo minha-chave-agendai-2026
echo Depois salve com Ctrl+S e FECHE o Bloco de Notas para continuar.
echo.
notepad "2-api\.env"

:tem_env
echo.
choice /c SN /m "[2/3] Importar o banco de dados agora (apaga e recria o banco agendai)"
if errorlevel 2 goto pular_banco

set "MYSQL="
where mysql >nul 2>nul
if not errorlevel 1 set "MYSQL=mysql"
if not defined MYSQL if exist "%ProgramFiles%\MySQL\MySQL Server 8.0\bin\mysql.exe" set "MYSQL=%ProgramFiles%\MySQL\MySQL Server 8.0\bin\mysql.exe"
if not defined MYSQL if exist "%ProgramFiles%\MySQL\MySQL Server 8.4\bin\mysql.exe" set "MYSQL=%ProgramFiles%\MySQL\MySQL Server 8.4\bin\mysql.exe"
if not defined MYSQL goto sem_mysql_cli

echo.
echo Digite a senha do MySQL (usuario root) quando for pedida:
"%MYSQL%" -u root -p --default-character-set=utf8mb4 < "1-banco-de-dados\agendai.sql"
if errorlevel 1 goto erro_banco
echo Banco de dados importado com sucesso.
goto pular_banco

:sem_mysql_cli
echo.
echo Nao encontrei o comando mysql neste computador.
echo Importe pelo MySQL Workbench:
echo    File ^> Open SQL Script ^> escolha 1-banco-de-dados\agendai.sql ^> clique no raio
echo Quando terminar de importar, volte aqui.
pause

:pular_banco
echo.
echo [3/3] Instalando as dependencias e iniciando a API...
cd 2-api
if not exist node_modules call npm install
if errorlevel 1 goto erro_npm

echo.
echo Quando aparecer "Banco de dados conectado", a API esta pronta.
echo Teste no navegador: http://localhost:3000/saude
echo Deixe esta janela ABERTA enquanto usa o app. Para parar, use Ctrl+C.
echo.
call npm run dev
pause
exit /b 0

:sem_node
echo O Node.js nao foi encontrado. Instale a versao LTS em https://nodejs.org
echo e execute este arquivo de novo.
pause
exit /b 1

:erro_banco
echo.
echo Nao foi possivel importar o banco. Confira a senha do MySQL e se ele esta ligado.
pause
exit /b 1

:erro_npm
echo.
echo Falha ao instalar as dependencias (npm install). Veja a mensagem acima.
pause
exit /b 1
