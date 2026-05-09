# Installation Scripts Guide

## Overview

Two modern installation scripts are provided:

1. **PowerShell Script** (`Install-FileChecksumService.ps1`) - Recommended for modern environments
2. **Batch Script** (`FileChecksumInstall-Enhanced.bat`) - Works everywhere, simpler

Choose based on your preference or environment.

---

## PowerShell Script (Recommended)

**File:** `Install-FileChecksumService.ps1`  
**When to use:** Modern Windows environments, better diagnostics, structured output

### Features

✅ **Colorized output** - Green (success), Red (error), Yellow (warning)  
✅ **Prerequisite checking** - Validates .NET Framework, executable, config  
✅ **Better error handling** - Clear error messages with next steps  
✅ **Metrics endpoint testing** - Automatically checks if service is responding  
✅ **Event log viewing** - Show recent service logs  
✅ **Non-destructive** - Safely handles existing services  
✅ **Help system** - Built-in detailed help

### Installation

```powershell
# Make script executable
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Run as Administrator
.\Install-FileChecksumService.ps1 -Action Install
```

### Usage Examples

```powershell
# Show help
.\Install-FileChecksumService.ps1 -Action Help

# Install service
.\Install-FileChecksumService.ps1 -Action Install

# Start service
.\Install-FileChecksumService.ps1 -Action Start

# Stop service
.\Install-FileChecksumService.ps1 -Action Stop

# Check status
.\Install-FileChecksumService.ps1 -Action Status

# View logs
.\Install-FileChecksumService.ps1 -Action Logs

# Uninstall
.\Install-FileChecksumService.ps1 -Action Uninstall
```

### Output Example

```
╔════════════════════════════════════════════════════════════╗
║           CHECKING PREREQUISITES                           ║
╚════════════════════════════════════════════════════════════╝

ℹ INFO: .NET Framework version: 4.7.2
✓ SUCCESS: Found executable: C:\...\bin\Release\FileChecksum.exe
✓ SUCCESS: Found config file: C:\...\config.xml

╔════════════════════════════════════════════════════════════╗
║           INSTALLING SERVICE                              ║
╚════════════════════════════════════════════════════════════╝

ℹ INFO: Install directory exists: C:\ProgramData\FileChecksum
ℹ INFO: Copying application files...
✓ SUCCESS: Files copied to C:\ProgramData\FileChecksum
✓ SUCCESS: Config file copied
ℹ INFO: Creating service: FileChecksumService
✓ SUCCESS: Service created successfully
✓ SUCCESS: Service verified
```

### Requirements

- Windows 7 or later
- .NET Framework 4.7.2+
- PowerShell 3.0+ (included in Windows 7+)
- Administrator privileges
- Execution Policy: RemoteSigned or less restrictive

### Advantages

✅ Modern, clean interface  
✅ Better error messages  
✅ Automatic prerequisite checking  
✅ Can test metrics endpoint  
✅ Can view Event Log  
✅ Structured validation  

---

## Batch Script (Universal)

**File:** `FileChecksumInstall-Enhanced.bat`  
**When to use:** Simple environments, automated deployments, legacy systems

### Features

✅ **Admin check** - Verifies running as Administrator  
✅ **Good error handling** - Clear messages for common issues  
✅ **Logging** - Writes to `C:\ProgramData\FileChecksum\install.log`  
✅ **Service verification** - Checks if service created successfully  
✅ **Timeout handling** - Proper waits between operations  
✅ **Status checking** - Can check if service is running  
✅ **No dependencies** - Works with any batch interpreter  

### Installation

Just double-click the file or run from Command Prompt:

```batch
REM Right-click and "Run as administrator"
FileChecksumInstall-Enhanced.bat install
```

### Usage Examples

```batch
REM Show help
FileChecksumInstall-Enhanced.bat help

REM Install service
FileChecksumInstall-Enhanced.bat install

REM Start service
FileChecksumInstall-Enhanced.bat start

REM Check status
FileChecksumInstall-Enhanced.bat status

REM Stop service
FileChecksumInstall-Enhanced.bat stop

REM Uninstall
FileChecksumInstall-Enhanced.bat uninstall
```

### Output Example

```
========================================================================
Installing File Checksum Service for Prometheus
========================================================================

[INFO] Install directory: C:\ProgramData\FileChecksum
[SUCCESS] Found executable: C:\...\bin\Release\FileChecksum.exe
[INFO] Copying executable and dependencies...
[SUCCESS] Files copied
[INFO] Creating service...
[SUCCESS] Service created

========================================================================
INSTALLATION COMPLETE
========================================================================
```

### Requirements

- Windows 7 or later
- .NET Framework 4.7.2+
- Administrator privileges
- Command Prompt or PowerShell

### Advantages

✅ Simple, no setup needed  
✅ Works everywhere  
✅ No PowerShell execution policy issues  
✅ Can be used in automated deployments  
✅ Clear, straightforward output  

---

## Comparison

| Feature | PowerShell | Batch |
|---------|-----------|-------|
| **Colorized Output** | ✅ Yes | ❌ No |
| **Prerequisite Check** | ✅ Detailed | ❌ Basic |
| **Event Log View** | ✅ Yes | ❌ No |
| **Metrics Testing** | ✅ Yes | ❌ No |
| **Setup Required** | ✅ Execution Policy | ❌ None |
| **Error Messages** | ✅ Very Clear | ✅ Clear |
| **Logging** | ❌ Screen Only | ✅ File + Screen |
| **Automation Ready** | ✅ Yes | ✅ Yes |
| **Works Everywhere** | ✅ Modern Windows | ✅ All Windows |

---

## Quick Start Guide

### Option A: PowerShell (Recommended)

```powershell
# 1. Open PowerShell as Administrator
# 2. Navigate to project folder
cd C:\Users\JeffR\source\repos\FileChecksum

# 3. Build the project
msbuild FileChecksum.sln /p:Configuration=Release

# 4. Enable PowerShell scripts (first time only)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# 5. Install service
.\Install-FileChecksumService.ps1 -Action Install

# 6. Configure (edit if needed)
notepad C:\ProgramData\FileChecksum\config.xml

# 7. Start service
.\Install-FileChecksumService.ps1 -Action Start

# 8. Check status
.\Install-FileChecksumService.ps1 -Action Status

# 9. View logs
.\Install-FileChecksumService.ps1 -Action Logs
```

### Option B: Batch (Simpler)

```batch
REM 1. Open Command Prompt as Administrator
REM 2. Navigate to project folder
cd C:\Users\JeffR\source\repos\FileChecksum

REM 3. Build the project
msbuild FileChecksum.sln /p:Configuration=Release

REM 4. Install service
FileChecksumInstall-Enhanced.bat install

REM 5. Configure (edit if needed)
notepad C:\ProgramData\FileChecksum\config.xml

REM 6. Start service
FileChecksumInstall-Enhanced.bat start

REM 7. Check status
FileChecksumInstall-Enhanced.bat status
```

---

## Troubleshooting

### PowerShell: "Cannot be loaded because running scripts is disabled"

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### PowerShell: "File not found"

**Solution:** Run from the project root directory containing `bin\Release\`

### Either Script: "Must run as Administrator"

**Solution:**
- **PowerShell:** Right-click PowerShell → "Run as Administrator"
- **Batch:** Right-click `.bat` file → "Run as Administrator"

### Either Script: "Failed to copy files"

**Solution:** Ensure you built the project first:
```bash
msbuild FileChecksum.sln /p:Configuration=Release
```

### Either Script: ".NET Framework not found"

**Solution:** Install .NET Framework 4.7.2 or later from:
https://dotnet.microsoft.com/download/dotnet-framework

### Either Script: Service won't start

**Solution:**
1. Check config.xml exists at `C:\ProgramData\FileChecksum\config.xml`
2. Check Event Log: Event Viewer → Windows Logs → Application
3. Filter by source: `FileChecksumService`
4. View logs (PowerShell): `.\Install-FileChecksumService.ps1 -Action Logs`

---

## Log File Locations

### PowerShell
- **Console output** - Displayed in terminal with colors

### Batch
- **Log file** - `C:\ProgramData\FileChecksum\install.log`
- View with: `notepad C:\ProgramData\FileChecksum\install.log`

### Event Log (Both)
- **Source:** FileChecksumService
- **Location:** Event Viewer → Windows Logs → Application
- View in PowerShell: `.\Install-FileChecksumService.ps1 -Action Logs`

---

## Production Deployment

For production deployments, you may want to:

### 1. Automate Installation

**PowerShell:**
```powershell
# deploy.ps1
.\Install-FileChecksumService.ps1 -Action Install
.\Install-FileChecksumService.ps1 -Action Start
```

**Batch:**
```batch
@echo off
FileChecksumInstall-Enhanced.bat install
FileChecksumInstall-Enhanced.bat start
echo Installation complete
```

### 2. Version Management

Keep service versions:
```
C:\ProgramData\FileChecksum\FileChecksum.exe.v1.0
C:\ProgramData\FileChecksum\FileChecksum.exe.v2.0
```

### 3. Backup Configuration

```powershell
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
Copy-Item "C:\ProgramData\FileChecksum\config.xml" "C:\ProgramData\FileChecksum\config_$timestamp.xml"
```

### 4. Rolling Updates

```batch
REM Stop old version
FileChecksumInstall-Enhanced.bat stop

REM Backup config
copy C:\ProgramData\FileChecksum\config.xml C:\ProgramData\FileChecksum\config.backup

REM Deploy new version
FileChecksumInstall-Enhanced.bat uninstall
REM [copy new files]
FileChecksumInstall-Enhanced.bat install

REM Restore config
copy C:\ProgramData\FileChecksum\config.backup C:\ProgramData\FileChecksum\config.xml

REM Start
FileChecksumInstall-Enhanced.bat start
```

---

## Comparison with Original Script

| Feature | Original | Enhanced |
|---------|----------|----------|
| **Install** | ✅ | ✅ |
| **Start/Stop** | ✅ | ✅ |
| **Uninstall** | ✅ | ✅ |
| **Error Handling** | Basic | ✅ Better |
| **Status Check** | ❌ | ✅ |
| **Logging** | ❌ | ✅ (Batch) |
| **Colorized Output** | ❌ | ✅ (PowerShell) |
| **Prerequisite Check** | ❌ | ✅ (PowerShell) |

---

## Which Should I Use?

**Use PowerShell if:**
- You want modern, colorized output
- You need to check prerequisites automatically
- You want to test metrics endpoint
- You want to view Event Logs easily
- You're on modern Windows (10+)

**Use Batch if:**
- You prefer simplicity
- You need it to work everywhere
- You're on older Windows systems
- You want minimal dependencies
- You're automating deployments

**Recommendation:** Use **PowerShell** for manual installation, **Batch** for automation.

---

## Next Steps

1. ✅ Choose your installation script
2. ✅ Place it in your project root
3. ✅ Build the project: `msbuild FileChecksum.sln /p:Configuration=Release`
4. ✅ Run as Administrator: Install → Configure → Start
5. ✅ Verify: Check Event Log or metrics endpoint

Both scripts are **production-ready** and handle edge cases well. Pick the one that fits your workflow!
