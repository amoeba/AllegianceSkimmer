<#
.SYNOPSIS
    Acquires the Decal assemblies needed to build AllegianceSkimmer.

.DESCRIPTION
    Downloads (with caching) the Decal MSI and extracts Decal.Adapter.dll and
    Decal.Interop.Core.dll into <repo>\deps so the project does not need the
    DLLs committed to git.

    Download is skipped when:
      - MSIFile is provided and its SHA-256 matches the pinned hash, or
      - the user-level cache already holds a copy with the pinned hash.

    The downloaded file's SHA-256 is verified against a pinned hash before it
    is used; a mismatch fails the build.

    Extraction uses "msiexec /a" on Windows and 7-Zip elsewhere.

.PARAMETER MSIUrl
    Where to download Decal.msi from. Defaults to the pinned 2983 release.

.PARAMETER ExpectedSha256
    Pinned SHA-256 of Decal.msi. Downloads failing this check abort the build.

.PARAMETER MSIFile
    Optional path to cache Decal.msi at. When provided, the script uses it as
    the cache location (the CI workflow passes the actions/cache path here).
    Defaults to %LOCALAPPDATA%\AllegianceSkimmer\decal-deps\Decal.msi on
    Windows and ~/.cache/AllegianceSkimmer/decal-deps/Decal.msi elsewhere.

.PARAMETER OutputDir
    Directory to write the extracted DLLs into. Defaults to <repo>\deps.
#>
[CmdletBinding()]
param(
    [string]$MSIUrl = "https://www.decaldev.com/releases/2983/Decal.msi",
    [string]$ExpectedSha256 = "101365BA4378BE20D9AB57BA9F1C1DEDA5F93BB1B7BDB511DA836C9A69A31F26",
    [string]$MSIFile,
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

if (-not $OutputDir) {
    $RepoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $OutputDir = Join-Path $RepoRoot "deps"
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$IsWindowsHost = $null -ne $env:OS -and $env:OS -like "Windows*"

function Test-Sha256 {
    param([string]$Path, [string]$Expected)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant()
    return $actual -eq $Expected.ToUpperInvariant()
}

# --- 1. Locate or obtain the MSI -------------------------------------------

if (-not $MSIFile) {
    $cacheRoot = if ($env:LOCALAPPDATA) {
        Join-Path $env:LOCALAPPDATA "AllegianceSkimmer\decal-deps"
    } else {
        Join-Path $HOME ".cache/AllegianceSkimmer/decal-deps"
    }
    $MSIFile = Join-Path $cacheRoot "Decal.msi"
}

# Normalize separators: msiexec fails on mixed \ and / from ${{ github.workspace }}.
$MSIFile = [System.IO.Path]::GetFullPath($MSIFile)

if (Test-Sha256 -Path $MSIFile -Expected $ExpectedSha256) {
    Write-Host "Using cached MSI: $MSIFile"
}
else {
    $msiDir = Split-Path $MSIFile -Parent
    New-Item -ItemType Directory -Path $msiDir -Force | Out-Null
    Write-Host "Downloading Decal MSI from $MSIUrl"
    Invoke-WebRequest -Uri $MSIUrl -OutFile $MSIFile
    if (-not (Test-Sha256 -Path $MSIFile -Expected $ExpectedSha256)) {
        throw "Downloaded MSI hash mismatch. Expected $ExpectedSha256, got $((Get-FileHash -LiteralPath $MSIFile -Algorithm SHA256).Hash)"
    }
    Write-Host "Download verified (SHA256 $ExpectedSha256)"
}

# --- 2. Extract the MSI ----------------------------------------------------

$extractDir = Join-Path $env:TEMP ("decal-extract-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

if ($IsWindowsHost) {
    Write-Host "Extracting with msiexec (administrative install)..."
    $log = Join-Path $env:TEMP ("decal-msi-" + [guid]::NewGuid().ToString("N") + ".log")
    $p = Start-Process msiexec -ArgumentList "/a `"$MSIFile`" /qn TARGETDIR=`"$extractDir`" /l*v `"$log`"" -Wait -PassThru
    if ($p.ExitCode -ne 0) {
        Get-Content $log -Tail 50 | ForEach-Object { Write-Host $_ }
        throw "msiexec failed with exit code $($p.ExitCode)"
    }
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
}
else {
    Write-Host "Extracting with 7-Zip..."
    & 7z x -y "-o$extractDir" "$MSIFile" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "7z extraction failed with exit code $LASTEXITCODE. Install 7-Zip (e.g. 'brew install sevenzip')."
    }
}

# --- 3. Copy the two assemblies into deps/ ---------------------------------

# The MSI stores files under short names (e.g. DecalAdapterDLL /
# InteropDecalCore). The msiexec administrative install restores the long
# names; 7-Zip does not, so match both.
$wanted = @(
    @{ Target = "Decal.Adapter.dll";     Names = @("Decal.Adapter.dll",      "DecalAdapterDLL") },
    @{ Target = "Decal.Interop.Core.dll"; Names = @("Decal.Interop.Core.dll", "Decal.Interop.Core.DLL", "InteropDecalCore") }
)

foreach ($w in $wanted) {
    $match = Get-ChildItem -Path $extractDir -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in $w.Names -and $_.FullName -notmatch '(?i)\.NET 4\.0 PIA' } |
        Select-Object -First 1
    if (-not $match) {
        throw "Could not find $($w.Names -join ' / ') in the extracted MSI"
    }
    $dest = Join-Path $OutputDir $w.Target
    Copy-Item -LiteralPath $match.FullName -Destination $dest -Force
    $h = (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash
    Write-Host "Acquired $dest ($($match.Length) bytes, SHA256 $h)"
}

# --- 4. Clean up -----------------------------------------------------------

Remove-Item -LiteralPath $extractDir -Recurse -Force -ErrorAction SilentlyContinue