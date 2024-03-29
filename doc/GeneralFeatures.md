# Core Engine: General Features

## Runtime Architecture

* Support multi platforms
* Runtime written in .net core 3.0
* No external dependencies in the core codebase except for .net standard library
* All engine code is compiled into an assembly
* Platform specific hosts consume the library main entry point and provides structures for system specific calls

## Component System

* Each system is responsible for storing components data and is free to organize the data to a cache friendly order
* Each system has a query mechanism so that other systems can get updated components data of data that has changed
* Each system can have multiple dependent system (injected with DI).

## Resources Management

* All assets are defined with native source material and metadata information
* Assets are compiled so they can be optimized for each platforms
* Files generated by the resource compiler are binary files arranged to minimize loading time
* Resource types should be easily added to the resource compiler
* Any change to source material are reflected into the engine while in edit mode
* Mutliple resource storages can be defined by priority so that we can have for example the following configured storages:
  * Archive compressed storage
  * File System storage
  * Network storage

## Tools