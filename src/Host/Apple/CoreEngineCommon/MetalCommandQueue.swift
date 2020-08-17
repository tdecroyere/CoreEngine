import Metal
import CoreEngineCommonInterop

public class MetalCommandQueue {
    let commandQueue: MTLCommandQueue
    var commandBuffer: MTLCommandBuffer
    let commandQueueType: GraphicsCommandType

    init (_ commandQueue: MTLCommandQueue, _ commandQueueType: GraphicsCommandType) {
        self.commandQueue = commandQueue
        self.commandQueueType = commandQueueType

        guard let commandBuffer = self.commandQueue.makeCommandBuffer() else {
            assert(false)
        }

        self.commandBuffer = commandBuffer
    }

    public func setLabel(_ label: String) {
        self.commandQueue.label = label
        self.commandBuffer.label = "CommandBuffer\(label)"
    }

    public func createCommandBuffer() {
        guard let commandBuffer = self.commandQueue.makeCommandBuffer() else {
            assert(false)
        }

        self.commandBuffer = commandBuffer

        guard let commandQueueLabel = self.commandQueue.label else {
            return
        }

        self.commandBuffer.label = "CommandBuffer\(commandQueueLabel)"
    }
}