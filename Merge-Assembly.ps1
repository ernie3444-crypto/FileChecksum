# ILMerge post-build script - merges all DLLs into single executable
param([string]$OutputDir = "bin\Release")

$OutputDir = $OutputDir.TrimEnd('\')
$MainExe = "FileChecksum.exe"
$mainExePath = Join-Path $OutputDir $MainExe
$mergedExePath = Join-Path $OutputDir "FileChecksum.Merged.exe"

Write-Host "Merging assemblies..." -ForegroundColor Cyan

# Try to find ILMerge in packages first
$ilmergePath = Get-ChildItem "packages\ILMerge.*\tools\ILMerge.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName -First 1

if (-not $ilmergePath) {
    Write-Host "ILMerge not in packages, downloading..." -ForegroundColor Yellow

    $ilmergeDir = "$env:TEMP\ILMerge"
    $ilmergePath = "$ilmergeDir\ILMerge.exe"

    if (-not (Test-Path $ilmergePath)) {
        if (-not (Test-Path $ilmergeDir)) {
            New-Item -ItemType Directory -Path $ilmergeDir -Force | Out-Null
        }

        Write-Host "Downloading ILMerge..." -ForegroundColor Cyan
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $zipPath = "$ilmergeDir\ILMerge.zip"

        try {
            Invoke-WebRequest -Uri "https://www.nuget.org/api/v2/package/ILMerge/3.0.41" -OutFile $zipPath -ErrorAction Stop
            Expand-Archive -Path $zipPath -DestinationPath $ilmergeDir -Force
            Remove-Item $zipPath

            $found = Get-ChildItem "$ilmergeDir\tools\*\ILMerge.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName -First 1
            if ($found) {
                $ilmergePath = $found
            }
        } catch {
            Write-Host "Error: $_" -ForegroundColor Red
            exit 1
        }
    }
}

if (-not (Test-Path $ilmergePath)) {
    Write-Host "ILMerge not found: $ilmergePath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $mainExePath)) {
    Write-Host "Main EXE not found: $mainExePath" -ForegroundColor Red
    exit 1
}

Write-Host "Using ILMerge: $ilmergePath" -ForegroundColor Gray

# Get all DLLs
$dlls = @(Get-ChildItem $OutputDir -Filter "*.dll" | Select-Object -ExpandProperty FullName)

if ($dlls.Count -eq 0) {
    Write-Host "No DLLs to merge" -ForegroundColor Yellow
    exit 0
}

Write-Host "Merging $($dlls.Count) assemblies..." -ForegroundColor Gray

# Build command and execute
$cmdArgs = @(
    "/out:$mergedExePath"
    $mainExePath
)
$cmdArgs += $dlls

try {
    & $ilmergePath $cmdArgs

    if ($LASTEXITCODE -eq 0 -and (Test-Path $mergedExePath)) {
        $origSize = (Get-Item $mainExePath).Length
        $mergedSize = (Get-Item $mergedExePath).Length
        Write-Host "Success!" -ForegroundColor Green
        Write-Host "Original: $('{0:N0}' -f $origSize) bytes" -ForegroundColor Gray
        Write-Host "Merged:   $('{0:N0}' -f $mergedSize) bytes" -ForegroundColor Gray
        Write-Host "Output: $mergedExePath" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "ILMerge failed" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
