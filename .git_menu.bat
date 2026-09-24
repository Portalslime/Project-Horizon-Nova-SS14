@echo off
chcp 65001 >nul
title Horizon Nova - Git Menu

cd /d "%~dp0"

echo.
echo  Запуск Git Menu...
echo.

python git_menu.py

if errorlevel 1 (
    echo.
    echo  [Ошибка] Не удалось запустить Python.
    echo  Убедись, что Python установлен и добавлен в PATH.
    echo.
    pause
)