# ILMerge post-build script for FileChecksum
# Merges all DLLs into a single executable
# Run: .\Merge-Assembly.ps1

param(
    [string]$OutputDir = ".\bin\Release",
    [string]$MainExe = "FileChecksum.exe"
)

# Check if ILMerge is installed via NuGet
$ilmergePath = Get-ChildItem ".\packages\ILMerge.*\tools\ILMerge.exe" -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $ilmergePath) {
    Write-Host "ILMerge not found in packages. Attempting to download..." -ForegroundColor Yellow

    # Create temp directory for ILMerge
    $ilmergeDir = "$env:TEMP\ILMerge"
    if (-not (Test-Path $ilmergeDir)) {
        New-Item -ItemType Directory -Path $ilmergeDir -Force | Out-Null
    }

    # Download ILMerge if not already downloaded
    $ilmergePath = "$ilmergeDir\ILMerge.exe"
    if (-not (Test-Path $ilmergePath)) {
        Write-Host "Downloading ILMerge..." -ForegroundColor Cyan
        # Using a direct download - ILMerge 3.0.41 from nuget
        $url = "https://www.nuget.org/api/v2/package/ILMerge/3.0.41"
        $zipPath = "$ilmergeDir\ILMerge.zip"

        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest -Uri $url -OutFile $zipPath -ErrorAction Stop
            Expand-Archive -Path $zipPath -DestinationPath $ilmergeDir -Force
            Remove-Item $zipPath

            $ilmergePath = Get-ChildItem "$ilmergeDir\tools\*\ILMerge.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($ilmergePath) {
                $ilmergePath = $ilmergePath.FullName
            }
        } catch {
            Write-Host "Failed to download ILMerge: $_" -ForegroundColor Red
            exit 1
        }
    }
}

if (-not (Test-Path $ilmergePath)) {
    Write-Host "ILMerge executable not found at: $ilmergePath" -ForegroundColor Red
    exit 1
}

Write-Host "Using ILMerge: $ilmergePath" -ForegroundColor Green

# Prepare paths
$mainExePath = Join-Path $OutputDir $MainExe
$mergedExePath = Join-Path $OutputDir "$($MainExe -replace '.exe', '.Merged.exe')"
$tempMergedPath = Join-Path $env:TEMP "FileChecksum.Merged.exe"

if (-not (Test-Path $mainExePath)) {
    Write-Host "Main executable not found: $mainExePath" -ForegroundColor Red
    exit 1
}

Write-Host "Merging assemblies..." -ForegroundColor Cyan
Write-Host "Input:  $mainExePath" -ForegroundColor Gray
Write-Host "Output: $mergedExePath" -ForegroundColor Gray

# Get all DLLs in the output directory
$dlls = @(Get-ChildItem $OutputDir -Filter "*.dll" | Where-Object { $_.Name -notlike "*Fody*" } | Select-Object -ExpandProperty FullName)

if ($dlls.Count -eq 0) {
    Write-Host "No DLLs found to merge" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found $($dlls.Count) DLLs to merge" -ForegroundColor Gray

# Build ILMerge command
$ilmergeArgs = @(
    "/out:`"$mergedExePath`""
    "`"$mainExePath`""
)
$ilmergeArgs += $dlls | ForEach-Object { "`"$_`"" }

try {
    # Run ILMerge
    $cmdLine = "& `"$ilmergePath`" $($ilmergeArgs -join ' ')"
    Invoke-Expression $cmdLine

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ILMerge failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }

    # Verify output
    if (Test-Path $mergedExePath) {
        $originalSize = (Get-Item $mainExePath).Length
        $mergedSize = (Get-Item $mergedExePath).Length
        Write-Host "✓ Merge successful!" -ForegroundColor Green
        Write-Host "  Original EXE: $('{0:N0}' -f $originalSize) bytes" -ForegroundColor Gray
        Write-Host "  Merged EXE:   $('{0:N0}' -f $mergedSize) bytes" -ForegroundColor Gray
        Write-Host "`nMerged executable: $mergedExePath" -ForegroundColor Green
        Write-Host "`nYou can now deploy just this single .exe file!`nNo DLLs needed." -ForegroundColor Green
    } else {
        Write-Host "Merged executable not created" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error during merge: $_" -ForegroundColor Red
    exit 1
}
