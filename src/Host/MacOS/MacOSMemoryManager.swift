import Cocoa
import CoreEngineInterop

func createMemoryBuffer(memoryManagerContext: UnsafeMutableRawPointer?, length: Int32) -> MemoryBuffer {
    //print("Swift create memory buffer")
    let bufferPtr = UnsafeMutablePointer<UInt8>.allocate(capacity: Int(length))
    bufferPtr.initialize(repeating: 0, count: Int(length))
	return MemoryBuffer(Id: 1, Pointer: bufferPtr, Length: length)
}

func destroyMemoryBuffer(memoryManagerContext: UnsafeMutableRawPointer?, memoryBufferId: UInt32) {
    // TODO: Implement buffer destroy
    //print("Swift destroy memory buffer (NOT IMPLEMENTED YET)")
}


class MacOSMemoryManager {
    // var memoryBuffers: [UInt32: UnsafeMutableRawPointer]
    // var currentMemoryBufferId: UInt32
}