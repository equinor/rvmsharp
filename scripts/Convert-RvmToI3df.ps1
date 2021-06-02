#Requires -Version 7
[CmdletBinding()]
param (
    [string] $InputDirectory = $(Join-Path "$PSScriptRoot" ".." "TestData" "HDA_RVM_lite"),
    [string] $OutputDirectory = $(Join-Path "$PSScriptRoot" ".\outputs\"),
    [string] $ArtifactDirectory = "C:\Users\nhals\GitRepos\uncover\EchoReflectApi\EchoReflect.Api\AppData\demomodel",
    [string] $I3dfPath = "C:\Users\nhals\GitRepos\conceal\i3df",
    [switch] $Force = $true,
    [switch] $UploadToDev = $false
)

begin {
}

process {
}

end {

    #region PsSetup
    Set-StrictMode -Version 3
    [Console]::ResetColor()
    $ErrorActionPreference = 'Stop'
    #endregion PsSetup


    #region Guards
    if (-not (Get-Command "dotnet" -ErrorAction 'SilentlyContinue')) {
        Write-Error "Could not find dotnet. Is the Dotnet CLI installed?"
    }

    if (-not (Get-Command "cargo" -ErrorAction 'SilentlyContinue')) {
        Write-Error "Could not find cargo. Do you have Rust and Cargo installed?"
    }

    if (-not $Force -and (Get-ChildItem $outputDirectory)) {
        Write-Error "The output directory is not empty. Consider using the ""-Force"" argument if this is expected."
    }
    #endregion Guards


    #region Reveal Composer
    $cadRevealComposerPath = Join-Path "$PSScriptRoot" ".." "CadRevealComposer.exe" "CadRevealComposer.exe.csproj"
    & dotnet.exe run --project $cadRevealComposerPath -- --InputDirectory $InputDirectory --OutputDirectory $OutputDirectory
    if ($LASTEXITCODE) {
        Write-Error "Dotnet failed with exit code $LASTEXITCODE"
    }

    #endregion Reveal Composer


    #region i3df-converter
    $expectedOutputPath = Join-Path (Resolve-Path $OutputDirectory) "output.json"
    if (-not (Test-Path $expectedOutputPath)) {
        Write-Error "Expected the output from dotnet to be at ""$expectedOutputPath"". Could not find the file. Has anything changed?"
    }

    $outputi3dFile = (Join-Path (Resolve-Path $OutputDirectory) "sector_0.i3d")

    $i3dfCargoPath = Join-Path $I3dfPath "Cargo.toml"

    & cargo.exe run --manifest-path $i3dfCargoPath --release --features dump --bin i3df-converter -- $expectedOutputPath $outputi3dFile
    if ($LASTEXITCODE) {
        Write-Error "cargo failed with exit code $LASTEXITCODE"
    }

    #endregion i3df-converter

    Copy-Item -Path $outputi3dFile $ArtifactDirectory

    if ($UploadToDev) {
        if (-not (Get-Command "az" -ErrorAction 'SilentlyContinue')) {
            Write-Error "Could not find az. Do you have Azure Cli installed?"
        }
        az storage azcopy blob upload `
            --container models `
            --account-name stechoreflectapidev `
            --source $(Join-Path $OutputDirectory "*") `
            --destination "hda/demomodel2/reflect/"
    }

    Write-Host "Success. I3d file available at ""$outputi3dFile"""
}

