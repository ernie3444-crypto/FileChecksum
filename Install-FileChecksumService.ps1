# FileChecksum Service Installer
# PowerShell version with enhanced features and error handling
# 
# USAGE:
#   .\Install-FileChecksumService.ps1 -Action Install
#   .\Install-FileChecksumService.ps1 -Action Start
#   .\Install-FileChecksumService.ps1 -Action Stop
#   .\Install-FileChecksumService.ps1 -Action Uninstall
#
# REQUIREMENTS:
#   - Windows 7 or later
#   - .NET Framework 4.7.2+
#   - Run as Administrator
#   - PowerShell 3.0+

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Install", "Uninstall", "Start", "Stop", "Status", "Logs")]
    [string]$Action,
    
    [string]$ServiceName = "FileChecksumService",
    [string]$DisplayName = "File Checksum Service for Prometheus",
    [string]$InstallPath = "C:\ProgramData\FileChecksum"
)

# ============================================================================
# CONFIGURATION
# ============================================================================

$ServiceExecutable = "FileChecksum.exe"
$ConfigFileName = "config.xml"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceExePath = Join-Path $ScriptPath "bin\Release\$ServiceExecutable"
$SourceConfigPath = Join-Path $ScriptPath $ConfigFileName

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║ $($Message.PadRight(58)) ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ SUCCESS: $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ ERROR: $Message" -ForegroundColor Red
}

function Write-Warning-Custom {
    param([string]$Message)
    Write-Host "⚠ WARNING: $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ INFO: $Message" -ForegroundColor Cyan
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-NetFramework {
    $dotnetVersion = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -ErrorAction SilentlyContinue).Version
    if ($dotnetVersion) {
        Write-Info ".NET Framework version: $dotnetVersion"
        return $true
    }
    return $false
}

function Test-ExecutableExists {
    if (-not (Test-Path $SourceExePath)) {
        Write-Error-Custom "Executable not found: $SourceExePath"
        Write-Info "Build the project first: msbuild FileChecksum.sln /p:Configuration=Release"
        return $false
    }
    Write-Success "Found executable: $SourceExePath"
    return $true
}

function Test-ConfigExists {
    if (-not (Test-Path $SourceConfigPath)) {
        Write-Warning-Custom "Config file not found: $SourceConfigPath"
        Write-Info "A sample config.xml will be created in the install directory"
        return $false
    }
    Write-Success "Found config file: $SourceConfigPath"
    return $true
}

function Create-InstallDirectory {
    if (-not (Test-Path $InstallPath)) {
        Write-Info "Creating install directory: $InstallPath"
        New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
        Write-Success "Directory created"
    } else {
        Write-Info "Install directory exists: $InstallPath"
    }
}

function Copy-Files {
    Write-Info "Copying application files..."
    
    # Copy executable and dependencies
    Copy-Item -Path "$ScriptPath\bin\Release\*" -Destination $InstallPath -Recurse -Force -ErrorAction Stop | Out-Null
    Write-Success "Files copied to $InstallPath"
    
    # Copy config if it exists
    if (Test-Path $SourceConfigPath) {
        Copy-Item -Path $SourceConfigPath -Destination "$InstallPath\$ConfigFileName" -Force
        Write-Success "Config file copied"
    } else {
        Write-Warning-Custom "No local config.xml - you must create one at: $InstallPath\$ConfigFileName"
    }
}

function Install-Service {
    Write-Header "INSTALLING SERVICE"
    
    # Check for existing service
    if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
        Write-Warning-Custom "Service already exists. Removing first..."
        Stop-Service $ServiceName -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        & sc.exe delete $ServiceName | Out-Null
        Start-Sleep -Seconds 2
        Write-Info "Previous service removed"
    }
    
    # Create service
    Write-Info "Creating service: $ServiceName"
    $ServicePath = Join-Path $InstallPath $ServiceExecutable
    
    try {
        New-Service -Name $ServiceName `
                    -DisplayName $DisplayName `
                    -BinaryPathName $ServicePath `
                    -StartupType Automatic `
                    -ErrorAction Stop | Out-Null
        Write-Success "Service created successfully"
    } catch {
        Write-Error-Custom "Failed to create service: $_"
        return $false
    }
    
    # Verify service was created
    if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
        Write-Success "Service verified"
        return $true
    } else {
        Write-Error-Custom "Service verification failed"
        return $false
    }
}

function Start-ServiceNow {
    Write-Header "STARTING SERVICE"
    
    $service = Get-Service $ServiceName -ErrorAction SilentlyContinue
    
    if (-not $service) {
        Write-Error-Custom "Service not found: $ServiceName"
        return $false
    }
    
    if ($service.Status -eq "Running") {
        Write-Info "Service is already running"
        return $true
    }
    
    Write-Info "Starting service..."
    try {
        Start-Service $ServiceName -ErrorAction Stop
        Start-Sleep -Seconds 2
        
        $service = Get-Service $ServiceName
        if ($service.Status -eq "Running") {
            Write-Success "Service started successfully"
            Write-Info "Access metrics at: http://localhost:5000/metrics"
            return $true
        } else {
            Write-Error-Custom "Service failed to start. Check Event Log for details"
            return $false
        }
    } catch {
        Write-Error-Custom "Failed to start service: $_"
        return $false
    }
}

function Stop-ServiceNow {
    Write-Header "STOPPING SERVICE"
    
    $service = Get-Service $ServiceName -ErrorAction SilentlyContinue
    
    if (-not $service) {
        Write-Error-Custom "Service not found: $ServiceName"
        return $false
    }
    
    if ($service.Status -eq "Stopped") {
        Write-Info "Service is already stopped"
        return $true
    }
    
    Write-Info "Stopping service..."
    try {
        Stop-Service $ServiceName -ErrorAction Stop -Force
        Start-Sleep -Seconds 2
        Write-Success "Service stopped successfully"
        return $true
    } catch {
        Write-Error-Custom "Failed to stop service: $_"
        return $false
    }
}

function Uninstall-Service {
    Write-Header "UNINSTALLING SERVICE"

    Write-Info "Checking if service exists..."
    $service = Get-Service $ServiceName -ErrorAction SilentlyContinue

    if (-not $service) {
        Write-Warning-Custom "Service not found: $ServiceName"
        return $true
    }

    Write-Info "Service found: $ServiceName"

    # Stop service
    if ($service.Status -eq "Running") {
        Write-Info "Stopping service..."
        Stop-Service $ServiceName -ErrorAction SilentlyContinue -Force
        Start-Sleep -Seconds 3
        Write-Success "Service stopped"
    } else {
        Write-Info "Service is not running (status: $($service.Status))"
    }

    # Delete service
    Write-Info "Deleting service registration..."
    $output = & sc.exe delete $ServiceName 2>&1
    Write-Info "Delete command output: $output"
    Start-Sleep -Seconds 3

    # Verify deletion
    Write-Info "Verifying service removal..."
    $serviceAfter = Get-Service $ServiceName -ErrorAction SilentlyContinue

    if (-not $serviceAfter) {
        Write-Success "Service uninstalled successfully"
        Write-Info "Install directory remains at: $InstallPath"
        Write-Info "To remove files: Remove-Item -Path '$InstallPath' -Recurse -Force"
        return $true
    } else {
        Write-Error-Custom "Service still exists. You may need to restart Windows to complete removal."
        Write-Info "Try running: sc.exe delete $ServiceName"
        return $false
    }
}

function Show-ServiceStatus {
    Write-Header "SERVICE STATUS"
    
    $service = Get-Service $ServiceName -ErrorAction SilentlyContinue
    
    if (-not $service) {
        Write-Error-Custom "Service not found: $ServiceName"
        return
    }
    
    Write-Info "Service Name: $($service.Name)"
    Write-Info "Display Name: $($service.DisplayName)"
    Write-Info "Status: $(if ($service.Status -eq 'Running') { Write-Host $service.Status -ForegroundColor Green -NoNewline; $service.Status } else { Write-Host $service.Status -ForegroundColor Red -NoNewline; $service.Status })"
    Write-Info "Startup Type: $($service.StartType)"
    Write-Info "Install Path: $InstallPath"
    Write-Info "Config Path: $InstallPath\$ConfigFileName"
    
    # Test metrics endpoint
    Write-Info ""
    Write-Info "Testing metrics endpoint..."
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/metrics" -TimeoutSec 2 -ErrorAction Stop
        Write-Success "Metrics endpoint is accessible (HTTP 200)"
    } catch {
        Write-Warning-Custom "Metrics endpoint not responding (service may still be starting)"
    }
}

function Show-EventLogs {
    Write-Header "RECENT EVENT LOG ENTRIES"
    
    Write-Info "Retrieving last 10 entries for: FileChecksumService"
    
    $events = Get-EventLog -LogName Application -Source "FileChecksumService" -Newest 10 -ErrorAction SilentlyContinue
    
    if ($events.Count -eq 0) {
        Write-Warning-Custom "No event log entries found"
        return
    }
    
    foreach ($event in $events) {
        $timeStr = $event.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss")
        $type = $event.EntryType
        $msg = $event.Message
        
        $color = switch($type) {
            "Error" { "Red" }
            "Warning" { "Yellow" }
            "Information" { "Green" }
            default { "White" }
        }
        
        Write-Host "$timeStr [$type] $msg" -ForegroundColor $color
    }
}

function Show-Help {
    Write-Header "FILE CHECKSUM SERVICE INSTALLER"
    Write-Host @"
PowerShell installation and management script for File Checksum Service

USAGE:
  .\Install-FileChecksumService.ps1 -Action <Action>

ACTIONS:
  Install     - Install the service (creates Windows service)
  Start       - Start the service
  Stop        - Stop the service
  Uninstall   - Uninstall and remove the service
  Status      - Show service status and metrics endpoint
  Logs        - Show recent Event Log entries

EXAMPLES:
  Install service:
    .\Install-FileChecksumService.ps1 -Action Install
  
  Start service:
    .\Install-FileChecksumService.ps1 -Action Start
  
  Check status:
    .\Install-FileChecksumService.ps1 -Action Status
  
  View logs:
    .\Install-FileChecksumService.ps1 -Action Logs
  
  Stop and uninstall:
    .\Install-FileChecksumService.ps1 -Action Stop
    .\Install-FileChecksumService.ps1 -Action Uninstall

REQUIREMENTS:
  - Windows 7 or later
  - .NET Framework 4.7.2+
  - Run as Administrator
  - PowerShell 3.0+

PATHS:
  Install Directory: $InstallPath
  Config File: $InstallPath\$ConfigFileName
  Executable: $SourceExePath
  Service Name: $ServiceName

FIRST TIME SETUP:
  1. Build the project:
     msbuild FileChecksum.sln /p:Configuration=Release
  
  2. Run as Administrator:
     .\Install-FileChecksumService.ps1 -Action Install
  
  3. Start the service:
     .\Install-FileChecksumService.ps1 -Action Start
  
  4. Verify:
     .\Install-FileChecksumService.ps1 -Action Status

TROUBLESHOOTING:
  - Check Event Log: .\Install-FileChecksumService.ps1 -Action Logs
  - Verify config: $InstallPath\$ConfigFileName
  - Check metrics: http://localhost:5000/metrics
  - Review README_REFACTORED.md for configuration help

"@
}

# ============================================================================
# MAIN SCRIPT
# ============================================================================

# Check admin privileges
if (-not (Test-Administrator)) {
    Write-Error-Custom "This script must be run as Administrator"
    Write-Host ""
    Write-Host "To run as Administrator:" -ForegroundColor Yellow
    Write-Host "  1. Open PowerShell as Administrator"
    Write-Host "  2. Run: .\Install-FileChecksumService.ps1 -Action $Action"
    Write-Host ""
    exit 1
}

# Check prerequisites
Write-Header "CHECKING PREREQUISITES"

$prereqsOk = $true

if (-not (Test-NetFramework)) {
    Write-Error-Custom ".NET Framework 4.7.2 or later not found"
    Write-Info "Install from: https://dotnet.microsoft.com/download/dotnet-framework"
    $prereqsOk = $false
} else {
    Write-Success ".NET Framework detected"
}

if ($Action -ne "Help" -and -not (Test-ExecutableExists)) {
    $prereqsOk = $false
}

if ($Action -eq "Install") {
    if (-not (Test-ConfigExists)) {
        Write-Warning-Custom "config.xml will need to be created manually"
    }
}

if (-not $prereqsOk) {
    Write-Host ""
    Write-Error-Custom "Prerequisites check failed"
    exit 1
}

# Execute action
Write-Host ""

switch ($Action) {
    "Install" {
        Create-InstallDirectory
        if (-not (Copy-Files)) { exit 1 }
        if (-not (Install-Service)) { exit 1 }
        
        Write-Host ""
        Write-Header "INSTALLATION COMPLETE"
        Write-Host @"
Next steps:

1. Configure the service:
   - Edit: $InstallPath\$ConfigFileName
   - See: README_REFACTORED.md for configuration help

2. Start the service:
   - .\Install-FileChecksumService.ps1 -Action Start

3. Verify installation:
   - .\Install-FileChecksumService.ps1 -Action Status
   - View Event Log: .\Install-FileChecksumService.ps1 -Action Logs
   - Test metrics: http://localhost:5000/metrics

4. Monitor performance:
   - Check: filechecksum_files_monitored metric
   - Check: filechecksum_matches and filechecksum_mismatches

For help, see README_REFACTORED.md or MIGRATION_GUIDE.md

"@
    }
    
    "Start" {
        if (-not (Start-ServiceNow)) { exit 1 }
    }
    
    "Stop" {
        if (-not (Stop-ServiceNow)) { exit 1 }
    }
    
    "Uninstall" {
        if (-not (Uninstall-Service)) { exit 1 }
    }
    
    "Status" {
        Show-ServiceStatus
    }
    
    "Logs" {
        Show-EventLogs
    }
    
    "Help" {
        Show-Help
    }
    
    default {
        Write-Error-Custom "Unknown action: $Action"
        Show-Help
        exit 1
    }
}

Write-Host ""
exit 0
