# Core Engine

![CI_BuildMacOS](https://github.com/tdecroyere/CoreEngine/workflows/CI_BuildMacOS/badge.svg?branch=master) [![codecov](https://codecov.io/gh/tdecroyere/CoreEngine/branch/master/graph/badge.svg)](https://codecov.io/gh/tdecroyere/CoreEngine)

Core Engine is a multi-platform realtime Game Engine written in .NET 5.

# General Features

- Support multi platforms:
    - MacOS: Host written in Swift and Metal. !~ In Progress ~!
    - Windows: Host written in C++/WinRT and DirectX12. -! Planned !-
    - Other Platforms: TBD
- Runtime written in dotnet 5.
- No external dependencies in the runtime codebase except for dotnet standard library.
- Entity Component System. +( Done )+
- CLI tools to manage game projects and compilation of assets.
- Visual editor:
    - In game editor !~ In Progress ~!
    - Native MacOS. -! Planned !-
    - Native WinUI 3.0 -! Planned !-
- Renderer:
    - GPU driven renderer.
    - Geometry culling on GPU.
    - Moment shadows maps.
    - PBR Rendering.

# Screenshots

![Bistro Scene](/doc/screenshots/20200124_Bistro.jpeg)
