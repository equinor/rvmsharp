# CAD Reveal automation

NOTE: This automation library expects `f3df-dump`, `i3df-dump` and `mesh2ctm` to be available on the computer. Add to the `PATH` environment variable. If it works in console it should be okay.

### Debug slow facet group matching by group key

NOTE: Use `Skip` and `Take` to limit number of facet groups. 

``` PowerShell
Export-FacetGroups -Verbose -GroupKey 1258427176102755799 -ExportFile 'gina-krog-1258427176102755799.obj' -RvmDirectory 'D:\Models\Gina Krog ASB 2022.02.03' | Convert-ObjToCtm
```

### Export all not instanced facet groups

NOTE: Use `Skip` and `Take` to limit number of facet groups. 

``` PowerShell
Export-FacetGroups -Verbose -NotInstanced -ExportFile 'huldra-notinstanced.obj' -RvmDirectory 'D:\Models\Huldra 2021.04.01' | Convert-ObjToCtm
```

### Convert all RVM files to Cognite Reveal model

``` PowerShell
Get-Help Convert-RvmToRevealModel

Convert-RvmToRevealModel -InputDirectory '...' -OutputDirectory '...' -ProjectId 1 -ModelId 1 -RevisionId 1
```

### Convert RVM to OBJ,CTM files per area

``` PowerShell
Get-ChildItem -Filter *.rvm `
| group { $_.Name.Split('-')[0] } `
| foreach { $_.Group | Convert-RvmToObj -ObjFile "$($_.Name).obj" } `
| Convert-ObjToCtm
```

### Convert RVM to OBJ,CTM

``` PowerShell
Get-ChildItem -Filter *.rvm | Convert-RvmToObj | Convert-ObjToCtm
```

### Convert RVM to OBJ

``` PowerShell
Get-Help Convert-RvmToObj

Convert-RvmToObj -RvmFile '...' -ObjFile '...'

Get-ChildItem -Filter *.rvm | Convert-RvmToObj
```

### Convert OBJ to CTM

``` PowerShell
Get-Help Convert-ObjToCtm

Convert-ObjToCtm -InputPath '...'

Get-ChildItem -Filter *.obj | Convert-ObjToCtm
```

### Dump F3D as json 

``` PowerShell
Get-Help Convert-F3dToJson

Convert-F3dToJson -InputPath '...'

Get-ChildItem -Filter *.f3d | Convert-F3dToJson
```

### Dump I3D as json  

``` PowerShell
Get-Help Convert-I3dToJson

Convert-I3dToJson -InputPath '...'

Get-ChildItem -Filter *.i3d | Convert-I3dToJson
```

### Create project folders for RVM files

``` PowerShell
$plantUri = 'http://ws3311.statoil.net/globalwebapi/echo/projectrvmreport/JCA'
$projects = Invoke-WebRequest $plantUri | ConvertFrom-ProjectJson
foreach($project in $projects) {
	New-Item -Type Directory $project.PlantProjectDescription
	foreach($rvmFile in $project.RvmFiles) {
		$txtFileName = [System.IO.Path]::ChangeExtension($rvmFile.FileName, "txt")
		$targetRvmFilePath = Join-Path $project.PlantProjectDescription $rvmFile.FileName
		$targetTxtFilePath = Join-Path $project.PlantProjectDescription $txtFileName
		If ((Test-Path $rvmFile.FileName) -eq $True -and (Test-Path $targetRvmFilePath) -eq $False) {
			Copy-Item $rvmFile.FileName $targetRvmFilePath
		}
		If ((Test-Path $txtFileName) -eq $True -and (Test-Path $targetTxtFilePath) -eq $False) {
			Copy-Item $txtFileName $targetTxtFilePath
		}
	}
}
```
