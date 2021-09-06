#Requires -Version 7
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string] $InputDirectory, #Example "../TestData/HDA_RVM"),
    [Parameter(Mandatory = $false)][string] $WorkDirectory = $(Join-Path "$PSScriptRoot" ".\work_temp\"),
    [Parameter(Mandatory = $true)][long] $ProjectId, #Example value:  1,
    [Parameter(Mandatory = $true)][long] $ModelId, #Example value: 2
    [Parameter(Mandatory = $true)][long] $RevisionId, #Example value: 3
    [Parameter(Mandatory = $true)][string] $ArtifactDirectory, #Example: "C:/artifacts/rvmsharp/hda",
    [switch] $Force = $false,
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
    $scriptTimer = [system.diagnostics.stopwatch]::StartNew()
    #endregion PsSetup


    #region Guards
    if (-not (Get-Command "dotnet" -ErrorAction 'SilentlyContinue')) {
        Write-Error "Could not find dotnet. Is the Dotnet CLI installed?"
    }

    if (-not $Force -and (Get-ChildItem $WorkDirectory)) {
        Write-Error "The output directory is not empty. Consider using the ""-Force"" argument if this is expected."
    }

    Remove-Item -Path $WorkDirectory -Recurse -Force -ErrorAction Ignore
    New-Item -Path $WorkDirectory -ItemType Directory -Force | Out-Null

    #endregion Guards


    #region Reveal Composer

    # Note: The line containing only " -- " separates the arguments to the dotnet compiler and the arguments to CadRevealComposer
    $cadRevealComposerPath = Join-Path "$PSScriptRoot" ".." "CadRevealComposer.exe" "CadRevealComposer.exe.csproj"
    & dotnet run `
        --configuration Release `
        --project $cadRevealComposerPath `
        -- `
        --InputDirectory $InputDirectory `
        --OutputDirectory $WorkDirectory `
        --ProjectId $ProjectId `
        --ModelId $ModelId `
        --RevisionId $RevisionId


    if ($LASTEXITCODE) {
        Write-Error "Dotnet failed with exit code $LASTEXITCODE"
    }

    #endregion Reveal Composer

    #region ctm-converter
    $mesh2ctm = "mesh2ctm.exe"
    if ($IsMacOS) {
        $mesh2ctm = "mesh2ctm.osx"
    }

    $CtmConverterPath = Join-Path $PSScriptRoot ".." "tools" "OpenCTM" $mesh2ctm
    Get-ChildItem -Path "$WorkDirectory/*" -Filter "*.obj" | ForEach-Object {
        Write-Output ("Converting " + $_.Name + " (" + ("{0:n2} MB" -f ($_.Length / 1MB) + ") to CTM"))
        $ctmInputPath = $_.FullName

        $ctmFileName = $_.BaseName + ".ctm"
        $ctmOutputPath = Join-Path $WorkDirectory $ctmFileName

        $compressionLevel = 4
        & $CtmConverterPath $ctmInputPath $ctmOutputPath --comment "RvmSharp" --method MG1 --level $compressionLevel --no-texcoords --no-colors --upaxis Y
        if ($LASTEXITCODE) {
            Write-Error "ctmconv failed." -ErrorAction Stop
        }
    }

    #endregion ctm-converter

    #region Artifact Staging

    $artifactStagingDirectory = Join-Path $WorkDirectory "ArtifactStaging"

    New-Item -Path $artifactStagingDirectory -ItemType Directory -Force | Out-Null


    $artifactStagingGlobs = @(
        "scene.json"
        "*.db"
        "*.i3d"
        "*.f3d"
        "*.ctm"
    )

    foreach ($filter in $artifactStagingGlobs) {
        Copy-Item -Path (Join-Path $WorkDirectory $filter) -Destination $artifactStagingDirectory
    }

    #endregion Artifact Staging

    if (-not (Test-Path -Path $ArtifactDirectory -PathType Container )) {
        New-Item -Path $ArtifactDirectory -ItemType Container
    }

    Copy-Item -Path (Join-Path $artifactStagingDirectory "*") -Destination $ArtifactDirectory

    if ($UploadToDev) {
        if (-not (Get-Command "az" -ErrorAction 'SilentlyContinue')) {
            Write-Error "Could not find az. Do you have Azure Cli installed?"
        }

        $destination = "hda/demomodel/reveal/"

        az storage azcopy blob upload `
            --container 'models' `
            --account-name 'stechoreflectapidev' `
            --source "$artifactStagingDirectory/*" `
            --destination $destination
    }

    Write-Host "Success. Output copied to ""$ArtifactDirectory"". Total time: $($scriptTimer.Elapsed)"
}
