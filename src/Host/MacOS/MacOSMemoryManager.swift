import Cocoa
import CoreEngineInterop

func createMemoryBuffer(memoryManagerContext: UnsafeMutableRawPointer?, length: Int32) -> MemoryBuffer {
    print("Swift create memory buffer")
    let bufferPtr = UnsafeMutablePointer<UInt8>.allocate(capacity: Int(length))
	return MemoryBuffer(Id: 1, Pointer: bufferPtr, Length: length)
}

func destroyMemoryBuffer(memoryManagerContext: UnsafeMutableRawPointer?, memoryBufferId: UInt32) {
    // TODO: Implement buffer destroy
}


class MacOSMemoryManager {
    // TODO: Implement proper memory management
}