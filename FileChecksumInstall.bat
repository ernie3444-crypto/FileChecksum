@echo off
REM ============================================================================
REM File Checksum Service Installation Script - SIMPLIFIED
REM ============================================================================

setlocal enabledelayedexpansion

REM Configuration
set SERVICE_NAME=FileChecksumService
set SERVICE_DISPLAY_NAME=File Checksum Service for Prometheus
set INSTALL_DIR=C:\ProgramData\FileChecksum

REM Get current directory
cd /d "%~dp0"
set SOURCE_BIN_DIR=%~dp0bin\Release
set SERVICE_EXE=%INSTALL_DIR%\FileChecksum.exe
set SOURCE_EXE=%SOURCE_BIN_DIR%\FileChecksum.exe
set SOURCE_CONFIG=%~dp0config.xml

REM ============================================================================
REM ADMIN CHECK
REM ============================================================================

echo Checking administrator privileges...
fsutil dirty query %systemdrive% >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: This script must be run as Administrator!
    echo.
    echo To fix this:
    echo   1. Right-click on this .bat file
    echo   2. Click "Run as administrator"
    echo.
    pause
    exit /b 1
)
echo OK - Administrator privileges confirmed
echo.

REM ============================================================================
REM SHOW HELP IF NO COMMAND
REM ============================================================================

if "%1"=="" (
    echo.
    echo File Checksum Service Installer
    echo ================================
    echo.
    echo USAGE:
    echo   FileChecksumInstall.bat install
    echo   FileChecksumInstall.bat start
    echo   FileChecksumInstall.bat stop
    echo   FileChecksumInstall.bat uninstall
    echo   FileChecksumInstall.bat status
    echo.
    echo EXAMPLES:
    echo   FileChecksumInstall.bat install
    echo   FileChecksumInstall.bat start
    echo.
    pause
    exit /b 0
)

REM ============================================================================
REM INSTALL COMMAND
REM ============================================================================

if /i "%1"=="install" (
    echo.
    echo ========================================================================
    echo install
    echo ========================================================================
    echo.
    
    echo Step 1: Checking for executable...
    if not exist "%SOURCE_EXE%" (
        echo ERROR: Not found: %SOURCE_EXE%
        echo.
        echo You need to build the project first:
        echo   msbuild FileChecksum.sln /p:Configuration=Release
        echo.
        pause
        exit /b 1
    )
    echo OK: Found %SOURCE_EXE%
    echo.
    
    echo Step 2: Checking if service already exists...
    sc query %SERVICE_NAME% >nul 2>&1
    if %errorlevel% equ 0 (
        echo WARNING: Service already exists, removing old version...
        net stop %SERVICE_NAME% >nul 2>&1
        timeout /t 2 /nobreak >nul
        sc delete %SERVICE_NAME% >nul 2>&1
        timeout /t 2 /nobreak >nul
        echo OK: Old service removed
    ) else (
        echo OK: No existing service found
    )
    echo.
    
    echo Step 3: Creating install directory...
    if not exist "%INSTALL_DIR%" (
        mkdir "%INSTALL_DIR%"
        echo OK: Created %INSTALL_DIR%
    ) else (
        echo OK: Directory exists %INSTALL_DIR%
    )
    echo.
    
    echo Step 4: Copying files...
    xcopy "%SOURCE_BIN_DIR%\*" "%INSTALL_DIR%\" /Y /E /I >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Failed to copy files
        pause
        exit /b 1
    )
    echo OK: Files copied to %INSTALL_DIR%
    echo.
    
    echo Step 5: Copying config file...
    if exist "%SOURCE_CONFIG%" (
        copy "%SOURCE_CONFIG%" "%INSTALL_DIR%\config.xml" /Y >nul
        echo OK: Config copied
    ) else (
        echo WARNING: config.xml not found - you'll need to create one
        echo Location: %INSTALL_DIR%\config.xml
    )
    echo.
    
    echo Step 6: Creating Windows service...
    sc create %SERVICE_NAME% binPath= "%SERVICE_EXE%" DisplayName= "%SERVICE_DISPLAY_NAME%" start= auto >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Failed to create service
        pause
        exit /b 1
    )
    echo OK: Service created
    echo.
    
    echo Step 7: Verifying service...
    sc query %SERVICE_NAME% >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Service verification failed
        pause
        exit /b 1
    )
    echo OK: Service verified
    echo.
    
    echo ========================================================================
    echo INSTALLATION SUCCESSFUL!
    echo ========================================================================
    echo.
    echo Next steps:
    echo.
    echo 1. Start the service:
    echo    FileChecksumInstall.bat start
    echo.
    echo 2. Check status:
    echo    FileChecksumInstall.bat status
    echo.
    echo 3. Configure if needed:
    echo    Edit: %INSTALL_DIR%\config.xml
    echo.
    echo 4. View metrics:
    echo    http://localhost:5000/metrics
    echo.
    pause
    exit /b 0
)

REM ============================================================================
REM START COMMAND
REM ============================================================================

if /i "%1"=="start" (
    echo.
    echo ========================================================================
    echo start
    echo ========================================================================
    echo.
    
    echo Checking if service exists...
    sc query %SERVICE_NAME% >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Service not found
        echo.
        echo Install it first:
        echo   FileChecksumInstall.bat install
        echo.
        pause
        exit /b 1
    )
    echo OK: Service found
    echo.
    
    echo Checking if already running...
    sc query %SERVICE_NAME% | find "RUNNING" >nul
    if %errorlevel% equ 0 (
        echo INFO: Service is already running
        pause
        exit /b 0
    )
    echo OK: Service is not running
    echo.
    
    echo Starting service...
    net start %SERVICE_NAME% >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Failed to start service
        echo.
        echo Check Event Log:
        echo   Event Viewer ^> Windows Logs ^> Application
        echo   Filter by: FileChecksumService
        echo.
        pause
        exit /b 1
    )
    
    timeout /t 2 /nobreak >nul
    echo OK: Service started!
    echo.
    echo Metrics available at: http://localhost:5000/metrics
    echo.
    pause
    exit /b 0
)

REM ============================================================================
REM STOP COMMAND
REM ============================================================================

if /i "%1"=="stop" (
    echo.
    echo ========================================================================
    echo stop
    echo ========================================================================
    echo.
    
    echo Checking if service exists...
    sc query %SERVICE_NAME% >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Service not found
        pause
        exit /b 1
    )
    echo OK: Service found
    echo.
    
    echo Stopping service...
    net stop %SERVICE_NAME% >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Failed to stop service
        pause
        exit /b 1
    )
    
    timeout /t 2 /nobreak >nul
    echo OK: Service stopped
    echo.
    pause
    exit /b 0
)

REM ============================================================================
REM UNINSTALL COMMAND
REM ============================================================================

if /i "%1"=="uninstall" (
    echo.
    echo ========================================================================
    echo uninstall
    echo ========================================================================
    echo.

    echo Checking if service exists...
    sc query %SERVICE_NAME%
    if %errorlevel% neq 0 (
        echo.
        echo INFO: Service '%SERVICE_NAME%' not found (nothing to uninstall)
        pause
        exit /b 0
    )
    echo OK: Service found
    echo.
    
    echo Stopping service...
    sc query %SERVICE_NAME% | find "RUNNING" >nul
    if %errorlevel% equ 0 (
        net stop %SERVICE_NAME% >nul 2>&1
        timeout /t 2 /nobreak >nul
        echo OK: Service stopped
    ) else (
        echo INFO: Service was already stopped
    )
    echo.
    
    echo Deleting service...
    sc delete %SERVICE_NAME%
    if %errorlevel% neq 0 (
        echo ERROR: Failed to delete service
        pause
        exit /b 1
    )

    timeout /t 3 /nobreak
    echo.
    echo OK: Service uninstalled successfully
    echo.
    echo Install directory still exists at: %INSTALL_DIR%
    echo To remove it: rmdir /s /q "%INSTALL_DIR%"
    echo.
    pause
    exit /b 0
)

REM ============================================================================
REM STATUS COMMAND
REM ============================================================================

if /i "%1"=="status" (
    echo.
    echo ========================================================================
    echo status
    echo ========================================================================
    echo.
    
    echo Checking if service exists...
    sc query %SERVICE_NAME% >nul 2>&1
    if %errorlevel% neq 0 (
        echo ERROR: Service not found
        pause
        exit /b 1
    )
    echo.
    
    echo Service Information:
    sc query %SERVICE_NAME%
    echo.
    echo Install Directory: %INSTALL_DIR%
    echo.
    pause
    exit /b 0
)

REM ============================================================================
REM UNKNOWN COMMAND
REM ============================================================================

echo ERROR: Unknown command "%1"
echo.
echo Valid commands:
echo   install
echo   start
echo   stop
echo   uninstall
echo   status
echo.
pause
exit /b 1
