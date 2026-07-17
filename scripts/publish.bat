@echo off
setlocal

cd /d "%~dp0.."

echo.
echo ==========================================
echo  Publicando mensagem no Pub/Sub Emulator
echo  Aspire / Publisher separado
echo ==========================================
echo.

set PUBSUB_EMULATOR_HOST=127.0.0.1:8085
set PUBSUB_PROJECT_ID=local-project
set PubSub__UseEmulator=true
set PubSub__ProjectId=local-project
set PubSub__TopicId=pedido-criado-topic
set PubSub__SubscriptionId=pedido-criado-worker-sub
set PubSub__EmulatorHost=127.0.0.1:8085

dotnet run --project ".\src\PubSubAspireDemo.Publisher\PubSubAspireDemo.Publisher.csproj"

if errorlevel 1 (
    echo.
    echo ERRO: falha ao publicar a mensagem.
    echo Verifique se o PubSubAspireDemo.AppHost esta rodando e se o recurso pubsub-emulator esta Running.
    echo.
    pause
    exit /b 1
)

echo.
echo Publish finalizado com sucesso.
echo.
pause
