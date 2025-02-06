#Requires -Version 7
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string] $InputDirectory, #Example "../TestData/HDA_RVM"),
    [Parameter(Mandatory = $false)][string] $WorkDirectory = $(Join-Path "$PSScriptRoot" ".\work_temp\"),
    [Parameter(Mandatory = $true)][long] $ProjectId, #Example value:  1,
    [Parameter(Mandatory = $true)][long] $ModelId, #Example value: 2
    [Parameter(Mandatory = $true)][long] $RevisionId, #Example value: 3
    [Parameter(Mandatory = $false)][AllowNull()][Nullable[System.Int32]] $InstancingThreshold = $null, # Optional. Must be > 0 if defined.
    [Parameter(Mandatory = $false)][AllowNull()][Nullable[System.Single]] $SimplificationThreshold = $null, # Optional. Must be > 0 if defined.
    [Parameter(Mandatory = $false)][AllowNull()][string] $NodeNameExcludeRegex = $null, # Optional.
    [switch] $SplitIntoZones = $false, # Used for large in extents assets to have separate splitting roots.
    [Parameter(Mandatory = $true)][string] $ArtifactDirectory, #Example: "C:/artifacts/rvmsharp/hda",
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
    $scriptTimer = [system.diagnostics.stopwatch]::StartNew()
    #endregion PsSetup


    #region Guards
    if (-not (Get-Command "dotnet" -ErrorAction 'SilentlyContinue')) {
        Write-Error "Could not find dotnet. Is the Dotnet CLI installed?"
    }

    if (-not $Force -and (Get-ChildItem $WorkDirectory)) {
        Write-Error "The working directory is not empty. Consider using the ""-Force"" argument if this is expected. All files will be deleted."
    }

    Remove-Item -Path $WorkDirectory -Recurse -Force -ErrorAction Ignore
    New-Item -Path $WorkDirectory -ItemType Directory -Force | Out-Null

    #endregion Guards


    #region Reveal Composer

    $cadRevealComposerPath = Join-Path "$PSScriptRoot" ".." "CadRevealComposer.exe" "CadRevealComposer.exe.csproj"

    $composerArgs = @()
    $composerArgs += "--InputDirectory"
    $composerArgs += $InputDirectory
    $composerArgs += "--OutputDirectory"
    $composerArgs += $WorkDirectory
    $composerArgs += "--ProjectId"
    $composerArgs += $ProjectId
    $composerArgs += "--ModelId"
    $composerArgs += $ModelId
    $composerArgs += "--RevisionId"
    $composerArgs += $RevisionId

    if ($InstancingThreshold) {
        $composerArgs += "--InstancingThreshold"
        $composerArgs += $InstancingThreshold
    }

    if ($SimplificationThreshold) {
        $composerArgs += "--SimplificationThreshold"
        $composerArgs += $SimplificationThreshold
    }

    if ($SplitIntoZones) {
        $composerArgs += "--SplitIntoZones"
        $composerArgs += $SplitIntoZones
    }

    if ($NodeNameExcludeRegex) {
        $composerArgs += "--NodeNameExcludeRegex"
        $composerArgs += $NodeNameExcludeRegex
    }

    # Note: The arg containing only " -- " separates the args to the dotnet compiler and the args to CadRevealComposer
    &dotnet run `
        --configuration Release `
        --project $cadRevealComposerPath `
        -- `
        $composerArgs

    if ($LASTEXITCODE) {
        Write-Error "Dotnet failed with exit code $LASTEXITCODE"
    }

    #endregion Reveal Composer

    if (-not (Test-Path -Path $ArtifactDirectory -PathType Container )) {
        New-Item -Path $ArtifactDirectory -ItemType Container
    }

    # Files not in the glob list are considered as temp files. Add new globs as needed.
    $artifactGlobs = @(
        "scene.json"
        "initialCamera.json"
        "modelMetadata.json"
        "*.db"
        "*.glb"
    )

    foreach ($filter in $artifactGlobs) {
        Copy-Item -Path (Join-Path $WorkDirectory $filter) -Destination $ArtifactDirectory
    }

    Write-Host "Success. Output copied to ""$ArtifactDirectory"". Total time: $($scriptTimer.Elapsed)"
}
