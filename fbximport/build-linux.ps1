#Requires -Version 7

$buildPath = "build/bin"
$libPath = "../CadRevealFbxProvider/lib"

# Build Dockerfile and output the binaries
docker build --platform linux/amd64 --output=$buildPath --target=binaries .

# Copy the cfbx library file to CadRevealFbxProvider
Copy-Item "$buildPath/libcfbx.so" $libPath

Write-Output "FBX Importer library for Linux successfully built and copied to CadRevealFbxProvider."
