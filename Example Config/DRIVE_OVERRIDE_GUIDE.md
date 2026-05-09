# Drive Override Feature - Complete Guide

## Overview

The **drive override** feature allows you to use a single master configuration for multiple machines that have identical file structures but on **different drive letters**.

### Problem It Solves

You have 3 nearly identical PCs:
- **PC-1**: Files on `C:\ drive`
- **PC-2**: Files on `D:\ drive` (has extra storage)
- **PC-3**: Files on `E:\ drive` (network mapped)

Without drive override:
- Need 3 different master configs (C paths, D paths, E paths)
- Maintenance nightmare if files move

With drive override:
- One master config with C paths
- Each PC specifies `driveOverride="D"` or `driveOverride="E"`
- Single master config covers all machines

---

## How It Works

### Step 1: Master Config Has C:\ Paths

```xml
<FileChecksums>
  <File path="C:\Program Files\App\application.exe" 
        algorithm="SHA256" 
        description="Main App">
    <ExpectedChecksum>abc123...</ExpectedChecksum>
  </File>
  
  <File path="C:\Data\config.ini" 
        algorithm="SHA256" 
        description="Config">
    <ExpectedChecksum>def456...</ExpectedChecksum>
  </File>
</FileChecksums>
```

### Step 2: Client Specifies Drive Override

**PC-1 (files on C:):**
```xml
<FileChecksums 
    pcType="PC-Type-A"
    configServerUrl="http://master/config.xml"
    driveOverride="">
```

**PC-2 (files on D:):**
```xml
<FileChecksums 
    pcType="PC-Type-A"
    configServerUrl="http://master/config.xml"
    driveOverride="D">
```

**PC-3 (files on E:):**
```xml
<FileChecksums 
    pcType="PC-Type-A"
    configServerUrl="http://master/config.xml"
    driveOverride="E">
```

### Step 3: Service Applies Override

When service starts:

1. Downloads master config (has C paths)
2. Applies drive override transformation:
   - PC-1: `C:\Program Files\App\application.exe` → `C:\Program Files\App\application.exe` ✓
   - PC-2: `C:\Program Files\App\application.exe` → `D:\Program Files\App\application.exe` ✓
   - PC-3: `C:\Program Files\App\application.exe` → `E:\Program Files\App\application.exe` ✓

3. Monitors correct paths on each PC

---

## Configuration

### Master Config Format

```xml
<FileChecksums>
  <File path="C:\Critical\file.exe" 
        algorithm="SHA256" 
        description="Critical File"
        pcType="PC-Type-A">
    <ExpectedChecksum>abc123...</ExpectedChecksum>
  </File>
</FileChecksums>
```

No special changes needed. Write paths with drive letters as normal.

### Client Config Format

```xml
<FileChecksums 
    port="5000"
    checkIntervalMs="60000"
    pcType="PC-Type-A"
    configServerUrl="http://master/config.xml"
    driveOverride="D">
    <!-- Leave empty or omit to use C: as-is -->
</FileChecksums>
```

**driveOverride attribute:**
- `driveOverride=""` - Use paths exactly as defined (no override)
- `driveOverride="D"` - Replace C: with D: in all paths
- `driveOverride="E"` - Replace C: with E: in all paths
- `driveOverride="Z"` - Works with any drive letter

---

## Detailed Behavior

### What Gets Replaced

```
Master Config Path          | driveOverride | Result Path
====================================================
C:\App\file.exe            | (empty)       | C:\App\file.exe
C:\App\file.exe            | D             | D:\App\file.exe
C:\App\file.exe            | E             | E:\App\file.exe
C:\Data\config.ini         | D             | D:\Data\config.ini
D:\Backup\file.zip         | E             | E:\Backup\file.zip
```

### What Doesn't Get Replaced

```
Master Config Path          | driveOverride | Result Path
====================================================
\\server\share\file.exe     | D             | \\server\share\file.exe
Z:\Mounted\file.exe         | D             | Z:\Mounted\file.exe
```

**UNC paths** (network paths starting with `\\`) are NOT affected by drive override.

---

## Real-World Examples

### Scenario 1: Development vs Production

**Development Environment:**
- Dev-PC: Files on E:\ (fast SSD)
- Production-PC: Files on C:\ (system drive)

**Master config (with C: paths):**
```xml
<File path="C:\App\app.exe" algorithm="SHA256">...</File>
```

**Dev-PC config:**
```xml
<FileChecksums driveOverride="E" ...>
```

**Production-PC config:**
```xml
<FileChecksums driveOverride="" ...>
```

### Scenario 2: Multi-Server Setup

**Server setup:**
- Primary: Files on C:\ (system)
- Backup: Files on D:\ (dedicated storage)
- Archive: Files on Z:\ (network mounted)

**Single master config covers all:**
```xml
<FileChecksums>
  <File path="C:\Database\db.mdf" pcType="Primary">...</File>
  <File path="C:\Database\db.mdf" pcType="Backup">...</File>
  <File path="C:\Database\db.mdf" pcType="Archive">...</File>
</FileChecksums>
```

**Each server:**
```xml
Primary:  <FileChecksums driveOverride="" ...>
Backup:   <FileChecksums driveOverride="D" ...>
Archive:  <FileChecksums driveOverride="Z" ...>
```

### Scenario 3: Hardware Replacement

Same PC gets drive upgrade:
- Old: Files on C:\ (was only drive)
- New: Files on D:\ (new fast drive added)

Just update client config:
```xml
<!-- Old -->
<FileChecksums driveOverride="" ...>

<!-- New -->
<FileChecksums driveOverride="D" ...>
```

---

## Validation

### Valid Drive Letters

✅ Single letter: `D`, `E`, `F`, `Z`  
✅ Upper or lower: `D` or `d`  
✅ With or without colon: `D` or `D:`

### Invalid Values

❌ `C:\` - includes colon and backslash  
❌ `DD` - two letters  
❌ `:D` - colon first  
❌ `1` - not a letter  
❌ ` D ` - has spaces (it will try to trim)

### Error Messages

**Invalid drive letter in config:**
```
Error applying drive override: Invalid drive letter: DD. Use single letter (e.g., 'D', 'E').
```

**Invalid drive doesn't exist:**
The service will try to monitor the path anyway. If the drive doesn't exist, you'll see:
```
File not found: D:\path\file.exe
```

---

## Event Log Examples

### Successful Override

```
Service starting...
Configuration loaded successfully from: Remote server: http://master/config.xml
Applied PCType filter: PC-Type-A. Monitoring 5 files.
Applied drive override: D. File paths updated.
Example file path: D:\Program Files\App\app.exe
Prometheus metrics server started on port 5000
Service started successfully.
```

### Override Applied but Path Missing

```
Applied drive override: D. File paths updated.
Example file path: D:\Program Files\App\app.exe
Error during checksum verification: File not found: D:\Program Files\App\app.exe
```

### Invalid Override Value

```
Error applying drive override: Invalid drive letter: DD. Use single letter (e.g., 'D', 'E').
Error during service startup: Configuration loading failed.
```

---

## Best Practices

### 1. Consistent Master Config

Always use **C:** drive in master config:
```xml
<!-- GOOD -->
<File path="C:\App\file.exe">

<!-- AVOID -->
<File path="D:\App\file.exe">
```

Why? Makes master config reusable for any drive letter.

### 2. Document Drive Assignments

In your client configs, add comments:
```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 
  PC-Production configuration
  Files located on: D:\ drive
  Master config URL: http://master/config.xml
-->
<FileChecksums 
    driveOverride="D"
    configServerUrl="http://master/config.xml">
```

### 3. Test Before Deployment

Test the override on one machine first:
```batch
# Start service
FileChecksumInstall.bat start

# Check Event Log
Event Viewer → Windows Logs → Application
Filter by: FileChecksumService

# Verify path update
Should show: "Example file path: D:\..."
```

### 4. Monitor File Path Errors

In metrics, watch for file-not-found errors:
```
filechecksum_mismatches = 1
ErrorMessage: "File not found"
```

Indicates wrong drive letter was used.

### 5. Handle Network Paths

For network paths (UNC), don't use drive override:
```xml
<!-- Network path - won't be affected by driveOverride -->
<File path="\\fileserver\share\file.exe">

<!-- Local path - will be affected -->
<File path="C:\local\file.exe">
```

---

## Comparison Table

| Feature | Without Override | With Override |
|---------|------------------|---------------|
| **Master configs needed** | 3 (one per drive) | 1 (all drives use it) |
| **Client configs needed** | 3 (one per drive) | 3 (minimal diffs) |
| **Maintenance** | Update all 3 | Update 1 master |
| **Flexibility** | Can mix drive letters | All paths from C:, override letters |
| **UNC paths** | Work as-is | Not affected |

---

## Troubleshooting

### "File not found" after override

**Problem:** Configured D: override but files actually on C:

**Solution:**
```xml
<!-- Check current drive -->
<!-- Change from: -->
<FileChecksums driveOverride="D">

<!-- To: -->
<FileChecksums driveOverride="C">
```

### Different file locations on different machines

**Problem:** Can't use override because drives have different files

**Solution:** Use PCType filtering instead:
```xml
<File path="C:\App\app.exe" pcType="PC-Type-A">
<File path="C:\Database\db.mdf" pcType="PC-Type-B">
```

### Can't remember which drive each PC is on

**Solution:** Document in comments:
```xml
<!-- Server-Primary: D:\ drive
     Server-Backup: D:\ drive (same config)
     Server-Archive: Z:\ drive (different override)
-->
<FileChecksums driveOverride="D" ...>
```

---

## Code Changes Required

To use this feature, update these files in your project:

1. **FileChecksumsConfig.cs** - Add `DriveOverride` attribute
   - Replace with: `FileChecksumsConfig-Enhanced.cs`

2. **ConfigurationService.cs** - Add override logic
   - Replace with: `ConfigurationService-Enhanced.cs`
   - Adds method: `ApplyDriveOverride(config, driveOverride)`

3. **FileChecksumService.cs** - Apply override during startup
   - Replace with: `FileChecksumService-Enhanced.cs`
   - Calls: `ApplyDriveOverride()` after filtering

---

## Summary

The **drive override** feature:
- ✅ Solves the "files on different drives" problem
- ✅ Reduces configuration management burden
- ✅ Works with PCType filtering (use both!)
- ✅ Only affects drive-based paths (not UNC)
- ✅ Validates input and provides clear errors
- ✅ Logs applied overrides to Event Log

Perfect for scenarios where machines have identical setups but files on different drives!

---

## Files Provided

- `FileChecksumsConfig-Enhanced.cs` - Model with DriveOverride
- `ConfigurationService-Enhanced.cs` - Service with override logic
- `FileChecksumService-Enhanced.cs` - Service applying override
- `master-config-with-override.xml` - Master config example
- `client-configs-with-override.xml` - Three client examples
- `DRIVE_OVERRIDE_GUIDE.md` - This guide
