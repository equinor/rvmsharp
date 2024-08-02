# CFBX

This is a simple bridge that exports necessary functions from FBX SDK to C# for conversion of FBX to Reveal model.
Most of the FBX functionality is not supported. This library should:

- Mimic FBX SDK API
- Avoid any unnecessary logic
- Caller should be responsible for providing memory for output results from functions (exception are FBX internal object initialization)
- Be as simple as possible
- Have unit tests

## Building on Windows or Mac

### Requirements

- Visual Studio 2019 or later for Windows
- FBX SDK 2020.3.2 or later
  - `FBX_ROOT` environment variable must be set.

### Building

Run the following commands to build the library and test project. The binaries are places in the `build/bin` folder.

```script
cmake -B build -S . -D FBXSDK_VERSION=<version>
cmake --build build --config Release
```

Where `<version>` is installed fbxsdk version. E.g. *2020.3.2*

## Building for Linux

To build, make sure *Docker* is installed and running, then simply run the `build-linux.ps1` script.

This will build the *Dockerfile*, which is set up to build the *fbximporter*, and copy the built library to `CadRevealFbxProvider/lib` folder. The library and test binaries can also be found in the `build/bin` folder.

## Testing

The test binary is places in the `build/bin` folder.

Run `tests -m <path-to-fbx-file>` to test the built library.
