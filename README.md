# Core Engine

![Windows x64](https://github.com/tdecroyere/CoreEngine/workflows/Windows%20x64/badge.svg) ![MacOS x64](https://github.com/tdecroyere/CoreEngine/workflows/MacOS%20x64/badge.svg) [![codecov](https://codecov.io/gh/tdecroyere/CoreEngine/branch/master/graph/badge.svg)](https://codecov.io/gh/tdecroyere/CoreEngine)

Core Engine is a multi-platform realtime Game Engine written in .NET 5.

# General Features

- Support multi platforms:
    - MacOS: Host written in Swift and Metal.
    - Windows: Host written in C++/WinRT and DirectX12.
    - Other Platforms: TBD
- Runtime written in .NET 5.
- No external dependencies in the runtime codebase except for dotnet standard library.
- Entity Component System.
- CLI tools to manage game projects and compilation of assets.
- Renderer:
    - GPU driven renderer.
    - Geometry culling on GPU.
    - Moment shadows maps.
    - PBR Rendering.

# Screenshots

![Bistro Scene](/doc/screenshots/20200124_Bistro.jpeg)
