import CoreEngineInterop

func createMemoryBuffer(memoryManagerContext: UnsafeMutableRawPointer?, length: UInt32) -> HostMemoryBuffer {
    //print("Swift create memory buffer")
    let bufferPtr = UnsafeMutablePointer<UInt8>.allocate(capacity: Int(length))
    bufferPtr.initialize(repeating: 0, count: Int(length))
	return HostMemoryBuffer(Id: 1, Pointer: bufferPtr, Length: length)
}

func destroyMemoryBuffer(memoryManagerContext: UnsafeMutableRawPointer?, memoryBufferId: UInt32) {
    // TODO: Implement buffer destroy
    //print("Swift destroy memory buffer (NOT IMPLEMENTED YET)")
}