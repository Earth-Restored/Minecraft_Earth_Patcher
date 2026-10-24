#!/usr/bin/env pwsh
param (
    [ValidateSet('Debug', 'Release')]
    [string]$config = 'Release',

    [string[]]$frameworks = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64'),

    [string[]]$androidAbis = @('android-arm64', 'android-x64'),

    [switch]$skipDesktop,
    [switch]$skipAndroid,
    [switch]$androidOnly,
    [switch]$desktopOnly
)

if ($androidOnly) {
    $skipDesktop = $true
}
if ($desktopOnly) {
    $skipAndroid = $true
}

$ErrorActionPreference = 'Stop'

# Setup Paths
$scriptRoot = $PSScriptRoot
if (-not $scriptRoot) { $scriptRoot = Get-Location }

$propsPath = Join-Path $scriptRoot "Directory.Build.props"
$version = "1.0.0" # Fallback version

if (Test-Path $propsPath) {
    [xml]$propsXml = Get-Content $propsPath
    # Try <Version> first, then fallback to <VersionPrefix>
    $parsedVersion = $propsXml.Project.PropertyGroup.Version
    if ([string]::IsNullOrWhiteSpace($parsedVersion)) {
        $parsedVersion = $propsXml.Project.PropertyGroup.VersionPrefix
    }
    
    if (-not [string]::IsNullOrWhiteSpace($parsedVersion)) {
        $version = $parsedVersion.Trim()
    }
}
else {
    Write-Warning "Directory.Build.props not found at $propsPath. Defaulting to version $version"
}

$projects = @(
    @{ Name = "UI.Desktop"; DisplayName = "MCEPatcher_UI" }
)

# Create output directory
$outDir = Join-Path $scriptRoot "build/$config"
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

# Load the compression assembly (Required for Windows PowerShell 5.1; already loaded in PS Core)
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not $skipDesktop) {
    foreach ($proj in $projects) {
        $projName = $proj.Name
        $displayName = $proj.DisplayName
        $csprojPath = Join-Path $scriptRoot "src/$projName/MCEPatcher.$projName.csproj"

        if (-not (Test-Path $csprojPath)) {
            Write-Error "Project file not found: $csprojPath"
            continue
        }

        foreach ($fx in $frameworks) {
            Write-Host "Publishing $displayName for $fx ($config)..." -ForegroundColor Cyan

            $zipFileName = "$displayName-$fx-$version.zip"
            $zipFilePath = Join-Path $outDir $zipFileName

            $tempPubDir = Join-Path $scriptRoot "temp_publish_$([guid]::NewGuid().ToString().Substring(0,8))"

            try {
                $publishArgs = @(
                    "publish", "`"$csprojPath`"",
                    "--configuration", $config,
                    "--runtime", $fx,
                    "--output", "`"$tempPubDir`"",
                    "--self-contained",
                    "-p:PublishSingleFile=true",
                    "-p:IncludeNativeLibrariesForSelfExtract=true",
                    "-p:PublishTrimmed=false",
                    "-p:DebugType=None",
                    "-p:DebugSymbols=false"
                )
                
                $process = Start-Process dotnet -ArgumentList $publishArgs -NoNewWindow -Wait -PassThru
                if ($process.ExitCode -ne 0) {
                    Write-Error "dotnet publish failed for $projName ($fx). Exit Code: $($process.ExitCode)"
                    continue
                }

                if (Test-Path $zipFilePath) {
                    Remove-Item $zipFilePath -Force
                }

                Write-Host "Zipping artifacts to $zipFileName..."
                [System.IO.Compression.ZipFile]::CreateFromDirectory($tempPubDir, $zipFilePath)
                
                Write-Host "Success: $zipFilePath`n" -ForegroundColor Green
            }
            finally {
                if (Test-Path $tempPubDir) {
                    Remove-Item -Path $tempPubDir -Recurse -Force
                }
            }
        }
    }
}

if (-not $skipAndroid) {
    $androidProj = Join-Path $scriptRoot "src/UI.Android/MCEPatcher.UI.Android.csproj"
    if (Test-Path $androidProj) {
        foreach ($abi in $androidAbis) {
            Write-Host "Building Android APK for $abi ($config)..." -ForegroundColor Cyan

            $buildArgs = @(
                "build", "`"$androidProj`"",
                "--configuration", $config,
                "-r", $abi
            )

            $process = Start-Process dotnet -ArgumentList $buildArgs -NoNewWindow -Wait -PassThru
            if ($process.ExitCode -ne 0) {
                Write-Error "dotnet build failed for Android ($abi). Exit Code: $($process.ExitCode)"
                continue
            }

            $abiShort = if ($abi -eq 'android-arm64') { 'arm64' } elseif ($abi -eq 'android-x64') { 'x64' } else { $abi }
            $apkPattern = Join-Path $scriptRoot "src/UI.Android/bin/$config/net11.0-android/$abi/*-Signed.apk"
            $builtApk = Get-ChildItem -Path $apkPattern -ErrorAction SilentlyContinue | Select-Object -First 1

            if (-not $builtApk) {
                $fallbackPattern = Join-Path $scriptRoot "src/UI.Android/bin/$config/net11.0-android/$abi/*.apk"
                $builtApk = Get-ChildItem -Path $fallbackPattern -ErrorAction SilentlyContinue | Select-Object -First 1
            }

            if ($builtApk) {
                $targetApkName = "MCEPatcher-$abiShort-$version.apk"
                $targetApkPath = Join-Path $outDir $targetApkName
                Copy-Item $builtApk.FullName -Destination $targetApkPath -Force
                Write-Host "Success: $targetApkPath`n" -ForegroundColor Green
            }
            else {
                Write-Warning "Could not find built APK matching $apkPattern"
            }
        }
    }
    else {
        Write-Error "Android project file not found: $androidProj"
    }
}

Write-Host "All publish tasks completed successfully!" -ForegroundColor Green
