#Requires -Version 7
[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string] $InputDirectory, #Example "../TestData/FBX",
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

    if (-not $Force -and (Get-ChildItem $OutputDirectory)) {
        Write-Error "The working directory is not empty. Consider using the ""-Force"" argument if this is expected. All files will be deleted."
    }

    Remove-Item -Path $OutputDirectory -Recurse -Force -ErrorAction Ignore
    New-Item -Path $OutputDirectory -ItemType Directory -Force | Out-Null

    #endregion Guards

    #region metadata extraction

    $inputMetadata = Get-Content $(Join-Path "$InputDirectory" ".\*_metadata.json") -Raw | ConvertFrom-Json -Depth 100
    #endregion

    #region Reveal Composer

    $cadRevealComposerPath = Join-Path "$PSScriptRoot" ".." "CadRevealComposer.Exe" "CadRevealComposer.Exe.csproj"

    $composerArgs = @()
    $composerArgs += "--InputDirectory"
    $composerArgs += $InputDirectory
    $composerArgs += "--OutputDirectory"
    $composerArgs += $OutputDirectory
    $composerArgs += "--ProjectId"
    $composerArgs += 1 # $ProjectId
    $composerArgs += "--ModelId"
    $composerArgs += Get-Random -Minimum 1000 -Maximum 99999 # $ModelId
    $composerArgs += "--RevisionId"
    $composerArgs += Get-Random -Minimum 1000 -Maximum 99999 # $RevisionId

    # Note: The arg containing only " -- " separates the args to the dotnet compiler and the args to CadRevealComposer
    &dotnet run `
        --configuration Release `
        --project $cadRevealComposerPath `
        -- `
        $composerArgs

    if ($LASTEXITCODE) {
        Write-Error "Dotnet failed with exit code $LASTEXITCODE"
    }

    $sceneMetadata = Get-Content $(Join-Path "$OutputDirectory" ".\scene.json") -Raw | ConvertFrom-Json -Depth 100

    #endregion Reveal Composer

    # compose output metadata json
    $artifactVersion = $sceneMetadata.version
    $artifactKind = "Reveal"
    $plantCode = $inputMetadata.plantCode
    $platformSectionId = $inputMetadata.scaffoldingId
    $SourceModelUpdatedDateTime = [DateTime]$inputMetadata.modelUpdatedDateTime

    $outputMetadata = @{
        platformNameLabel = "Huldra" # Equal to Plant Scaffolding is placed in
        projectNameLabel = "Huldra" # Equal to Plant Scaffolding is placed in
        plantCode = $plantCode
        installationCode = "HUA" ## TODO REMOVE ME #HUA, SNA etc
        instCode = "HUA" # TODO REMOVE ME
        projectSectionId = "Topside" # Scaffolding or Topside
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
            StidTags = "VG230001, VG230022" # Example: Could be tags that are covered by this scaffolding? Might be something to search/filter on?
            LastUpdatedBy = "NIH@equinor.com"
        }
    }

    $outputJsonFile = $(Join-Path "$OutputDirectory" ".\metadata.json")
    $outputMetadata | ConvertTo-Json -depth 100 | Out-File $outputJsonFile

    #region Artifact Staging

    # $artifactStagingDirectory = Join-Path $OutputDirectory "ArtifactStaging"

    # New-Item -Path $artifactStagingDirectory -ItemType Directory -Force | Out-Null

    $hierachyFile = Join-Path "$OutputDirectory" "hierarchy.db"
    $hierarchyFileNew = $outputMetadata.hierarchyId  + ".db"
    Rename-Item -Path $hierachyFile -NewName $hierarchyFileNew


    # $artifactStagingGlobs = @(
    #     "scene.json"
    #     "initialCamera.json"
    #     "metadata.json"
    #     "*.db"
    #     "*.glb"
    # )

    # foreach ($filter in $artifactStagingGlobs) {
    #     Copy-Item -Path (Join-Path $OutputDirectory $filter) -Destination $artifactStagingDirectory
    # }

    # #endregion Artifact Staging

    # if (-not (Test-Path -Path $OutputDirectory -PathType Container )) {
    #     New-Item -Path $OutputDirectory -ItemType Container
    # }

    # Copy-Item -Path (Join-Path $artifactStagingDirectory "*") -Destination $OutputDirectory

    $targetPath = "$PlantCode-$PlatformSectionId/$($ArtifactKind)_$artifactVersion/$RevisionNumber"
    $targetPath
    # Move-Item -Path $OutputDirectory -Destination $OutputDirectory/$targetPath -Force
    $t = "$OutputDirectory 1/$targetPath";
    New-Item -Path $t  -ItemType Directory -Force
    Move-Item -Path $OutputDirectory/* -Destination $t -Force
    Write-Host "Success. Output copied to ""$OutputDirectory"". Total time: $($scriptTimer.Elapsed)"

    # The key below is the Azure Storage Emulators well-known key: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-emulator#authorize-with-shared-key-credentials
    $azConnection = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
    az storage container create -n "$PlantCode-$PlatformSectionId" --connection-string $azConnection
    az storage blob upload-batch -d "$PlantCode-$PlatformSectionId" --connection-string $azConnection -s "$OutputDirectory 1/$PlantCode-$PlatformSectionId" --overwrite

    $localModelsTool = "C:\Users\VES\Documents\Projects\Echo\EchoModelDistribution\LocalModels\bin\Debug\net6.0\LocalModels.exe"
    & $localModelsTool --add $t/metadata.json
    & $localModelsTool --metadata $t/metadata.json --copyhierarchy "$t/$($outputMetadata.hierarchyId).db"
}
