@echo off
setlocal

set BASE=%1
if "%BASE%"=="" (
  set BASE=http://localhost:5000
)
echo Using API base: %BASE%

REM Delegate to PowerShell script (works on Win 10+)
powershell -ExecutionPolicy Bypass -File "%~dp0smoke_win.ps1" -BaseUrl %BASE%

endlocal
