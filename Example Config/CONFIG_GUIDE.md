# Sample Configuration Files Guide

## Overview

Two sample configurations are provided:
1. **Master Server Config** - Central configuration for all machine types
2. **Client Config** - Individual machine that fetches from master server

## Architecture

```
┌─────────────────────────────────────┐
│  Master Server (Centralized)        │
│  http://server/master-config.xml    │
│  - Workstation-A files              │
│  - Server-B files                   │
│  - Server-C files                   │
│  - Common files (all machines)      │
└──────────────┬──────────────────────┘
               │
       ┌───────┴───────────────┐
       │                       │
   [Client A]             [Client B]
   pcType: Workstation-A  pcType: Server-B
   Fetches master →       Fetches master →
   Filters to A files     Filters to B files
```

## Setup Steps

### Step 1: Host Master Config on Server

**Option A: IIS Web Server**
1. Create folder: `C:\inetpub\wwwroot\checksum`
2. Copy `master-config-sample.xml` to this folder
3. Rename to: `master-config.xml`
4. URL will be: `http://your-server/checksum/master-config.xml`

**Option B: Simple File Share**
1. Create network share: `\\fileserver\checksum`
2. Copy master config there
3. Access as: `http://fileserver/checksum/master-config.xml` (if HTTP enabled)
   OR use UNC path with custom handler

**Option C: Any Web Server**
- Apache: Place in `htdocs/checksum/`
- Nginx: Place in configured web root
- URL: `http://your-server/checksum/master-config.xml`

### Step 2: Configure Client Machines

For each client machine:

1. **Determine PCType**
   - Workstation-A: `pcType="Workstation-A"`
   - Server-B: `pcType="Server-B"`
   - Create custom types as needed

2. **Create config.xml**
   - Copy `client-config-sample.xml`
   - Update `pcType` attribute
   - Update `configServerUrl` with correct server address
   - Customize fallback files if needed

3. **Deploy to Machine**
   - Path: `C:\ProgramData\FileChecksum\config.xml`
   - Run service install/start

### Step 3: Add Files to Monitor

Edit master config to add files:

```xml
<File path="C:\path\to\file.exe" 
      algorithm="SHA256" 
      description="File description"
      pcType="Workstation-A">
  <ExpectedChecksum>abc123def456...</ExpectedChecksum>
</File>
```

## How to Get Expected Checksums

Use PowerShell to calculate checksums:

```powershell
# SHA256 (recommended)
(Get-FileHash "C:\path\to\file.exe" -Algorithm SHA256).Hash

# Other algorithms
(Get-FileHash "C:\path\to\file.exe" -Algorithm MD5).Hash
(Get-FileHash "C:\path\to\file.exe" -Algorithm SHA1).Hash
(Get-FileHash "C:\path\to\file.exe" -Algorithm SHA512).Hash
```

Then copy the hash into the master config.

## File Organization Strategy

### Recommendation 1: One Master, Many Clients

**Master Config:**
```
http://central-server/checksum/master-config.xml
```

**Client Configs (scattered):**
```
C:\ProgramData\FileChecksum\config.xml (on each machine)
```

Benefits:
- ✅ Single source of truth
- ✅ Easy to update all machines
- ✅ Central management
- ✅ Scales well

### Recommendation 2: Multiple Specialized Masters

**If you have very different machine types:**
```
http://server/checksum/workstations-config.xml
http://server/checksum/servers-config.xml
http://server/checksum/databases-config.xml
```

Each client points to its relevant master.

Benefits:
- ✅ Smaller config files
- ✅ Easier to manage specific groups
- ✅ Cleaner separation of concerns

## Customization Examples

### Example 1: Adding a New Workstation Type

**In master-config.xml:**
```xml
<!-- WORKSTATION-C FILES -->
<File path="D:\Projects\project1.db" 
      algorithm="SHA256" 
      description="Project Database"
      pcType="Workstation-C">
  <ExpectedChecksum>[calculate with PowerShell]</ExpectedChecksum>
</File>
```

**In client-config.xml (on Workstation-C):**
```xml
<FileChecksums 
    port="5000"
    checkIntervalMs="60000"
    pcType="Workstation-C"
    configServerUrl="http://server/checksum/master-config.xml">
```

### Example 2: Adding a Common File

**In master-config.xml:**
```xml
<!-- COMMON FILE - All machines monitor this -->
<File path="C:\Windows\System32\svchost.exe" 
      algorithm="SHA256" 
      description="Service Host Process">
  <ExpectedChecksum>[checksum]</ExpectedChecksum>
</File>
```

**Note:** No `pcType` attribute means all machines will monitor it.

### Example 3: Different Check Intervals

**For critical file server (frequent checks):**
```xml
<FileChecksums 
    checkIntervalMs="30000"
    pcType="Server-Critical">
```

**For non-critical workstations (less frequent):**
```xml
<FileChecksums 
    checkIntervalMs="300000"
    pcType="Workstation-Regular">
```

## PCType Naming Conventions

Choose a naming scheme and stick to it:

**Option A: Descriptive (Recommended)**
```
Workstations-DeptA
Workstations-DeptB
FileServer-Primary
FileServer-Backup
WebServer-Internal
WebServer-Public
DatabaseServer-Production
DatabaseServer-Staging
```

**Option B: Simple**
```
WS-A
WS-B
FS-1
FS-2
WEB-1
DB-1
```

**Option C: Location-Based**
```
NYC-Workstations
NYC-FileServer
LA-Workstations
LA-FileServer
```

**Recommendation:** Use descriptive names like `Workstations-DeptA` so it's clear what each type is for.

## Monitoring Plan Example

### File: Critical Application Executable

**On Master Config:**
```xml
<File path="C:\Program Files\MyApp\app.exe" 
      algorithm="SHA256" 
      description="Critical App v2.1.0"
      pcType="Workstation-A">
  <ExpectedChecksum>a1b2c3d4e5f6...</ExpectedChecksum>
</File>
```

**Track This In:**
- ✅ Document when you calculate the checksum (e.g., "2026-05-08 App v2.1.0 deployed")
- ✅ Note the version in the description
- ✅ Update checksum when app is updated
- ✅ Monitor for unexpected changes (indicates tampering/malware)

## Prometheus Metrics

Once configured, access metrics:

```
http://workstation-a:5000/metrics
```

Key metrics to monitor:
```promql
# Files currently being monitored
filechecksum_files_monitored

# Files that passed verification
filechecksum_matches{filechecksum_matches="1"}

# Files that FAILED verification (potential issue!)
filechecksum_mismatches{filechecksum_mismatches="1"}

# Last check timestamp
filechecksum_last_check_timestamp_seconds
```

## Troubleshooting

### "Configuration contains no files to monitor"

**Cause:** PCType filtering removed all files  
**Solution:**
1. Check master config has files with matching `pcType`
2. Check client config `pcType` matches master config `pcType`
3. Check master config has files with NO `pcType` (common files)

### "Configuration loading failed"

**Cause:** Server unreachable or master config invalid  
**Solution:**
1. Verify server URL is correct and accessible
2. Verify master config XML is valid (test with XML viewer)
3. Check network connectivity from client to server
4. Service falls back to local fallback config (check Event Log)

### "File not found" errors in Event Log

**Cause:** File path in config doesn't exist  
**Solution:**
1. Verify file path is correct
2. Verify file exists on target machine
3. Verify service account has read permissions
4. Use exact full paths (no relative paths)

### Unexpected checksum mismatches

**Cause:** File was modified or replaced  
**Solution:**
1. If intentional: Update master config with new checksum
2. If unintentional: File may have been compromised
3. Check file timestamp and version
4. Review security logs for unauthorized changes

## Best Practices

### 1. Version Your Configs

**Include version comments:**
```xml
<!-- Master Config v2.1.0 - Updated 2026-05-08
     Added: New workstation type for lab computers
     Changed: Updated antivirus path for new deployment
     Removed: Deprecated app monitoring
-->
```

### 2. Document Changes

Keep a change log in the config comments:
```xml
<!-- 
  CHANGELOG:
  v2.1.0 (2026-05-08) - Added Lab-Workstations type
  v2.0.0 (2026-04-15) - Refactored for new infrastructure
  v1.0.0 (2026-01-01) - Initial deployment
-->
```

### 3. Regular Reviews

- Review checksums quarterly
- Verify files still exist and are valid
- Update for new critical applications
- Remove obsolete entries

### 4. Backup Master Config

```bash
# Weekly backup
copy \\server\checksum\master-config.xml \\backup\configs\master-config-2026-05-08.xml
```

### 5. Test First

Before deploying to production:
1. Test master config on single client first
2. Verify checksums are calculated correctly
3. Verify metrics are being collected
4. Verify Event Log entries are clear
5. Then roll out to all machines

## File Paths: Windows Examples

### System Files
```
C:\Windows\System32\config\SAM
C:\Windows\System32\kernel32.dll
C:\Windows\System32\ntoskrnl.exe
C:\Windows\System32\svchost.exe
```

### Program Files
```
C:\Program Files\AppName\application.exe
C:\Program Files\AppName\config.xml
C:\Program Files (x86)\AppName\app.dll
```

### User Data
```
C:\Users\Public\Documents\data.bin
C:\ProgramData\CompanyName\settings.ini
D:\Projects\database.db
```

### Network Drives (UNC paths work too)
```
\\fileserver\share\critical-file.dat
```

## Algorithm Selection

| Algorithm | Size | Use When | Example |
|-----------|------|----------|---------|
| SHA256 | 256-bit | Default choice | Most files |
| SHA512 | 512-bit | Extra security needed | Encryption keys |
| SHA1 | 160-bit | Legacy systems | Old applications |
| MD5 | 128-bit | NOT recommended | Legacy only |

**Recommendation:** Use SHA256 for everything new.

## Complete Example Configuration

See sample files:
- `master-config-sample.xml` - Full master server example
- `client-config-sample.xml` - Full client example

Adapt these for your environment by:
1. Changing PCType values
2. Updating file paths
3. Calculating correct checksums
4. Adjusting check intervals
5. Setting correct server URL

---

**Next Steps:**
1. Customize the sample configs for your environment
2. Calculate checksums for your critical files
3. Deploy master config to your server
4. Deploy client configs to each machine
5. Monitor metrics in Prometheus

Questions? Refer to **README_REFACTORED.md** or **DEVELOPER_REFERENCE.md**.
