#Requires -Version 7
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string] $InputDirectory, #Example "../TestData/FBX",
    [Parameter(Mandatory = $false)][string] $WorkDirectory = $(Join-Path "$PSScriptRoot" ".\work_temp\"),
    [Parameter(Mandatory = $true)][string] $OutputDirectory, #Example: "C:/artifacts/scaffolding",
    [Parameter(Mandatory = $true)][Int32] $RevisionNumber, # Read previous revNr and increment +n, important that is is always increasing
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
    if ($IsLinux) {
        # Known Linux compability issues: We do not yet have tested a binary with the FBX SDK for Linux.
        Write-Error "This script is not yet tested for Linux. If you want to try running it, please remove this line and beware of dragons!"
    }
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
    Remove-Item -Path $OutputDirectory -Recurse -Force -ErrorAction Ignore
    New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null

    #endregion Guards

    #region metadata extraction

    $inputMetadata = Get-Content $(Join-Path "$InputDirectory" ".\fbx_test_model_metadata.json") -Raw | ConvertFrom-Json -Depth 100
    #endregion

    #region Reveal Composer

    $cadRevealComposerPath = Join-Path "$PSScriptRoot" ".." "CadRevealComposer.exe" "CadRevealComposer.exe.csproj"

    $composerArgs = @()
    $composerArgs += "--InputDirectory"
    $composerArgs += $InputDirectory
    $composerArgs += "--OutputDirectory"
    $composerArgs += $WorkDirectory
    $composerArgs += "--ProjectId"
    $composerArgs += 1 # $ProjectId
    $composerArgs += "--ModelId"
    $composerArgs += 2 # $ModelId
    $composerArgs += "--RevisionId"
    $composerArgs += 3 # $RevisionId

    # Note: The arg containing only " -- " separates the args to the dotnet compiler and the args to CadRevealComposer
    &dotnet run `
        --configuration Release `
        --project $cadRevealComposerPath `
        -- `
        $composerArgs

    if ($LASTEXITCODE) {
        Write-Error "Dotnet failed with exit code $LASTEXITCODE"
    }

    $sceneMetadata = Get-Content $(Join-Path "$WorkDirectory" ".\scene.json") -Raw | ConvertFrom-Json -Depth 100

    #endregion Reveal Composer

    # compose output metadata json
    $artifactVersion = $sceneMetadata.version
    $artifactKind = "Reveal"
    $plantCode = $inputMetadata.plantCode
    $platformSectionId = $inputMetadata.scaffoldingId
    $SourceModelUpdatedDateTime = [DateTime]$inputMetadata.modelUpdatedDateTime

    $outputMetadata = @{
        platformNameLabel = "Processing Platform 1" # Equal to Plant Scaffolding is placed in
        projectNameLabel = "Johan Sverdrup" # Equal to Plant Scaffolding is placed in
        plantCode = $plantCode
        projectSectionId = "Scaffolding"
        platformSectionId = $platformSectionId # Needs to be consistent across multiple updates of the same scaffolding file
        platformSectionLabel = $inputMetadata.scaffoldingDisplayName # Display name of the scaffolding
        revisionNumber = $RevisionNumber
        buildRunDateTime = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ" -AsUTC
        sourceModelUpdatedDateTime = $SourceModelUpdatedDateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        artifactKind = $artifactKind # Kept as is
        artifactFileFormatVersion = $artifactVersion # Kept as is
        hierarchyId = ($plantCode + "-" +
            $artifactKind +"_" +
            $artifactVersion + "_" +
            $platformSectionId + "-" +
            $RevisionNumber + "-" + [guid]::NewGuid()).ToLower() # Unique for each scaffolding "File"
        metadata = @{
            Key = "Value" # Generic Key-Value pairs of metadata that can be used to filter scaffoldings client side?,
            StidTags = @("VG230001", "VG230022") # Example: Could be tags that are covered by this scaffolding? Might be something to search/filter on?
            LastUpdatedBy = "NIH@equinor.com"
        }
    }

    $outputJsonFile = $(Join-Path "$WorkDirectory" ".\metadata.json")
    $outputMetadata | ConvertTo-Json -depth 100 | Out-File $outputJsonFile

    #region Artifact Staging

    $artifactStagingDirectory = Join-Path $WorkDirectory "ArtifactStaging"

    New-Item -Path $artifactStagingDirectory -ItemType Directory -Force | Out-Null

    $hierachyFile = Join-Path "$WorkDirectory" ".\hierarchy.db"
    $hierarchyFileNew = $outputMetadata.hierarchyId  + ".db"
    $hierachyFile
    $hierarchyFileNew
    Rename-Item $hierachyFile $hierarchyFileNew

    $artifactStagingGlobs = @(
        "scene.json"
        "initialCamera.json"
        "metadata.json"
        "*.db"
        "*.glb"
    )

    foreach ($filter in $artifactStagingGlobs) {
        Copy-Item -Path (Join-Path $WorkDirectory $filter) -Destination $artifactStagingDirectory
    }

    #endregion Artifact Staging

    if (-not (Test-Path -Path $OutputDirectory -PathType Container )) {
        New-Item -Path $OutputDirectory -ItemType Container
    }

    Copy-Item -Path (Join-Path $artifactStagingDirectory "*") -Destination $OutputDirectory

    Write-Host "Success. Output copied to ""$OutputDirectory"". Total time: $($scriptTimer.Elapsed)"
}
