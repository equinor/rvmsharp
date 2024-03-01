# CFBX

This is a simple bridge that exports necessary functions from FBX SDK to C# for conversion of FBX to Reveal model.
Most of the FBX functionality is not supported. This library should:

- Mimic FBX SDK API
- Avoid any unnecessary logic
- Caller should be responsible for providing memory for output results from functions (exception are FBX internal object initialization)
- Be as simple as possible
- Have unit tests

## Requirements

- Visual Studio 2019 or later for Windows
- FBX SDK 2020.3.2 or later
  - FBX_ROOT environment variable must be set
  - FBXSDK_VERSION must be set at variable for the cmake build (for version checking)
- VCPKG (only if using the VCPKG package manager)
  - VCPKG_ROOT environment variable must be set to the VCPKG root folder without trailing slash
    - Example: `C:\Users\username\vcpkg`

## Building on un-managed system

```bash
# bash
cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake -D FBXSDK_VERSION=2020.3.2
cmake --build build
# For release build:
cmake --build build --config Release
```

```ps1
# PowerShell
cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE="$env:VCPKG_ROOT/scripts/buildsystems/vcpkg.cmake"
cmake --build build
# For release build:
cmake --build build --config Release
```

## Building on managed system

On a managed system we are only allowed to run executables from the C:/Appl/ folder exclusively. This causes problems for the VCPKG package manager. This is because VCPKG will attempt to install its own version of PowerShell and run the executable from a predetermined folder to perform some operations.

Because of the above limitations, we need to manually install any packages originally installed using VCPKG. How to do this is listed in the below subsections.

### Installing Catch2 (test framework) manually

To install Catch2:
1. Find the Catch2 repository and perform a 'git pull' in the C:/Appl/ folder.
2. Create a 'build' folder under C:/Appl/Catch2/
3. Goto the C:/Apple/Catch2/build folder and execute
```
cmake ../CMakeLists.txt
cd ..
# For release
cmake --build .\build\ --config Release
cmake --install .\build\ --config Release
# For debug
cmake --build .\build\ --config Debug
cmake --install .\build\ --config Debug
```

### Building fbximport

Note that in case we compile for release the compiled libraries, such as Catch2, must also be compiled in release, and vice versa. To build, execute the following sequence:
```ps1
# PowerShell
cmake -B build -S . -D FBXSDK_VERSION=2020.3.2
# For release
cmake --build build --config Release
# For debug
cmake --build build
```

## Testing

This library uses `catch2` for unit tests.
