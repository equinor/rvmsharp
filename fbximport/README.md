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
  - `FBX_ROOT` environment variable must be set.

## Building

```script
cmake -B build -S . -D FBXSDK_VERSION=2020.3.2
cmake --build build --config Release
```

## Testing

The test binary is places in the `build/bin` folder.

Run `tests -m <path-to-fbx-file>` to test the built library.
