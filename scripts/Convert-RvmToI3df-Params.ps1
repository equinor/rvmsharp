Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

# Developer help file to avoid having changes in the Run script when there is no real changes.
# Avoid committing changes in this file if they are not new required parameters.

$Arguments = @{
    InputDirectory    = $(Join-Path "$PSScriptRoot" ".." "TestData" "HDA_RVM_lite/" -Resolve)
    WorkDirectory     = $(Join-Path "$PSScriptRoot" "./work_temp/" -Resolve)
    ProjectId         = 10000 #Unused
    ModelId           = 1
    RevisionId        = 2
    ArtifactDirectory = $(Join-Path "$PSScriptRoot" ".." "TestData" "RunArtifact/" -Resolve )
    Force             = $true
    UploadToDev       = $false
}

Write-Host "Run Parameters"
Write-Host ($Arguments | Out-String)
$convertRvmToI3dfScript = Join-Path -Path $PSScriptRoot -ChildPath "Convert-RvmToI3df.ps1"
& $convertRvmToI3dfScript @Arguments
