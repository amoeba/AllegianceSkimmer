param([string]$NuGetPackageRoot, [string]$ProjectDir);

$NuGetPackageRoot = $NuGetPackageRoot.TrimEnd('\')
$ProjectDir = $ProjectDir.TrimEnd('\')

$makensis = Join-Path $NuGetPackageRoot "nsis-tool\3.0.8\tools\makensis.exe"
$nsiFile = Join-Path $ProjectDir "scripts\installer.nsi"

If ($Env:OS -ne "" -and $Env:OS -ne $null -and $Env:OS.ToLower().Contains("windows")) {
    & $makensis $nsiFile
}
else {
    & makensis $nsiFile
}
