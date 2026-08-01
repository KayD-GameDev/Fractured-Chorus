<#
Compiles Assembly-CSharp and Assembly-CSharp-Editor from the command line using the
Roslyn compiler shipped with Unity, reusing the reference list Unity already resolved
into Library/Bee/artifacts/**/*.rsp. Lets us catch compile errors without opening the
Editor. Source file lists are rebuilt from disk so added/removed scripts are picked up.
#>

param(
    [string]$UnityRoot = 'D:/6000.4.0f1',
    [string]$DagDir = 'Library/Bee/artifacts/1900b0aEDbg.dag'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$csc = Join-Path $UnityRoot 'Editor/Data/DotNetSdkRoslyn/csc.dll'
if (-not (Test-Path $csc)) { throw "Roslyn compiler not found: $csc" }

$outDir = Join-Path $projectRoot 'Temp/compile-check'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Get-OptionLines([string]$rspPath) {
    Get-Content $rspPath | Where-Object {
        $_.StartsWith('-') -and -not $_.StartsWith('-out:')
    }
}

function Get-Sources([bool]$editorAssembly) {
    Get-ChildItem 'Assets' -Recurse -Filter '*.cs' -File |
        Where-Object {
            $segments = $_.FullName.Substring($projectRoot.Length + 1) -split '[\\/]'
            $isEditor = $segments -contains 'Editor'
            $isEditor -eq $editorAssembly
        } |
        ForEach-Object { '"' + ($_.FullName -replace '\\', '/') + '"' }
}

function Invoke-Compile([string]$name, [string]$rspPath, [bool]$editorAssembly, [string[]]$extraOptions) {
    $target = Join-Path $outDir "$name.dll"
    $lines = @(Get-OptionLines $rspPath) + $extraOptions + @("-out:`"$($target -replace '\\', '/')`"") + @(Get-Sources $editorAssembly)

    $generated = Join-Path $outDir "$name.rsp"
    Set-Content -Path $generated -Value $lines -Encoding UTF8

    Write-Host "== $name ==" -ForegroundColor Cyan
    $output = & dotnet $csc "@$generated" 2>&1
    $exitCode = $LASTEXITCODE

    $errors = $output | Where-Object { $_ -match ': error ' }
    if ($errors) { $errors | ForEach-Object { Write-Host $_ -ForegroundColor Red } }

    return $exitCode
}

$runtimeRsp = Join-Path $DagDir 'Assembly-CSharp.rsp'
$editorRsp = Join-Path $DagDir 'Assembly-CSharp-Editor.rsp'

$failures = 0
if ((Invoke-Compile 'Assembly-CSharp' $runtimeRsp $false @()) -ne 0) { $failures++ }

$runtimeDll = (Join-Path $outDir 'Assembly-CSharp.dll') -replace '\\', '/'
if ((Invoke-Compile 'Assembly-CSharp-Editor' $editorRsp $true @("-r:`"$runtimeDll`"")) -ne 0) { $failures++ }

if ($failures -gt 0) {
    Write-Host "COMPILE FAILED ($failures assembly/assemblies)" -ForegroundColor Red
    exit 1
}

Write-Host 'COMPILE OK' -ForegroundColor Green
