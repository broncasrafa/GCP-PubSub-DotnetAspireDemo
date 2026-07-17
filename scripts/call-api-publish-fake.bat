@echo off
setlocal

echo.
echo ==========================================
echo  Chamando API para publicar mensagem fake
echo ==========================================
echo.

curl -X POST "https://localhost:7044/pedidos/publicar-fake" ^
  -H "Content-Type: application/json" ^
  -k

echo.
echo.
pause
