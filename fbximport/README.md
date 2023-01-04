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
- FBX SDK 2020 or later
  - FBX_ROOT environment variable must be set
- VCPKG
  - VCPKG_ROOT environment variable must be set to the VCPKG root folder without trailing slash
    - Example: `C:\Users\username\vcpkg`

## Building

```bash
# bash
cmake -B build -S . -DCMAKE_TOOLCHAIN_FILE=${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake
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

## Testing

This library uses `catch2` for unit tests.
