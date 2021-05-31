#Requires -Version 7
[CmdletBinding()]
param (
    [string] $inputDirectory = $(Join-Path ".." "TestData" "HDA_RVM_lite"),
    [string] $outputDirectory = ".\outputs\",
    [string] $i3dfPath = "C:\Users\nhals\GitRepos\conceal\i3df",
    [switch] $Force = $false
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
    & dotnet.exe run --project $cadRevealComposerPath -- --InputDirectory $inputDirectory --OutputDirectory $outputDirectory
    #endregion Reveal Composer


    #region i3df-converter
    $expectedOutputPath = Join-Path (Resolve-Path $outputDirectory) "output.json"
    if (-not (Test-Path $expectedOutputPath)) {
        Write-Error "Expected the output from dotnet to be at ""$expectedOutputPath"". Could not find the file. Has anything changed?"
    }

    $outputi3dFile = (Join-Path (Resolve-Path $outputDirectory) "output.i3d")

    $i3dfCargoPath = Join-Path $i3dfPath "Cargo.toml"

    & cargo.exe run --manifest-path $i3dfCargoPath --release --features dump --bin i3df-converter -- $expectedOutputPath $outputi3dFile
    #endregion i3df-converter

    Write-Host "Success. I3d file available at ""$outputi3dFile"""
}

