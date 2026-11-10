@echo off
chcp 65001 >nul
title Установка Генератора документов ЭДО
cls
echo =======================================================================
echo          Установка «Генератор документов ЭДО (УПД 5.03)»
echo =======================================================================
echo.

set "TARGET_DIR=%LOCALAPPDATA%\Programs\EdoXmlGenerator"
echo [1/3] Копирование файлов программы в: "%TARGET_DIR%"...
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
xcopy /E /I /Y "%~dp0Программа_без_установки (Portable)\*" "%TARGET_DIR%\" >nul
if errorlevel 1 (
    echo [ОШИБКА] Не удалось скопировать файлы программы!
    pause
    exit /b 1
)

echo [2/3] Создание ярлыков на Рабочем столе и в меню «Пуск»...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ws = New-Object -ComObject WScript.Shell; " ^
  "$desk = [Environment]::GetFolderPath('Desktop'); " ^
  "$sc1 = $ws.CreateShortcut(\"$desk\Генератор документов ЭДО.lnk\"); " ^
  "$sc1.TargetPath = '%TARGET_DIR%\EdoXmlGenerator.exe'; " ^
  "$sc1.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$sc1.Description = 'Генератор документов ЭДО и УПД 5.03'; " ^
  "$sc1.Save(); " ^
  "$start = [Environment]::GetFolderPath('Programs'); " ^
  "$sc2 = $ws.CreateShortcut(\"$start\Генератор документов ЭДО.lnk\"); " ^
  "$sc2.TargetPath = '%TARGET_DIR%\EdoXmlGenerator.exe'; " ^
  "$sc2.WorkingDirectory = '%TARGET_DIR%'; " ^
  "$sc2.Description = 'Генератор документов ЭДО и УПД 5.03'; " ^
  "$sc2.Save();"

echo [3/3] Регистрация в реестре Windows...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdoXmlGenerator" /v "DisplayName" /t REG_SZ /d "Генератор документов ЭДО (УПД 5.03)" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdoXmlGenerator" /v "DisplayVersion" /t REG_SZ /d "1.0.0" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdoXmlGenerator" /v "Publisher" /t REG_SZ /d "EdoXmlGenerator" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdoXmlGenerator" /v "InstallLocation" /t REG_SZ /d "%TARGET_DIR%" /f >nul
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall\EdoXmlGenerator" /v "DisplayIcon" /t REG_SZ /d "%TARGET_DIR%\EdoXmlGenerator.exe" /f >nul

echo.
echo =======================================================================
echo               УСТАНОВКА УСПЕШНО ЗАВЕРШЕНА!
echo =======================================================================
echo.
echo Ярлык программы создан на вашем Рабочем столе: «Генератор документов ЭДО».
echo.
set /p RUN="Запустить программу сейчас? (Y/N, Enter = Да): "
if /i "%RUN%"=="N" exit /b 0

start "" "%TARGET_DIR%\EdoXmlGenerator.exe"
exit /b 0
