import Metal
import QuartzCore.CAMetalLayer
import simd
import CoreEngineCommonInterop

class MetalCommandQueue {
    let commandQueueObject: MTLCommandQueue
    let commandQueueType: GraphicsServiceCommandType
    let fence: MTLSharedEvent
    var fenceValue: UInt64
    var commandBuffer: MTLCommandBuffer

    init (_ commandQueueObject: MTLCommandQueue, _ commandQueueType: GraphicsServiceCommandType, _ fence: MTLSharedEvent, _ commandBuffer: MTLCommandBuffer) {
        self.commandQueueObject = commandQueueObject
        self.commandQueueType = commandQueueType
        self.fence = fence
        self.fenceValue = 0
        self.commandBuffer = commandBuffer
    }
}

class MetalCommandList {
    let commandQueue: MetalCommandQueue
    var commandEncoder: MTLCommandEncoder?
    var label: String?
    var renderTargets: [MTLTexture]
    var resourceFences: [MTLFence]

    init (_ commandQueue: MetalCommandQueue, _ commandEncoder: MTLCommandEncoder?) {
        self.commandQueue = commandQueue
        self.commandEncoder = commandEncoder
        self.renderTargets = []
        self.resourceFences = []
    }
}

class MetalHeap {
    let heapObject: MTLHeap?
    let heapType: GraphicsServiceHeapType
    let length: UInt

    init (_ heapType: GraphicsServiceHeapType, _ length: UInt, _ heapObject: MTLHeap?) {
        self.heapType = heapType
        self.length = length
        self.heapObject = heapObject
    }
}

class MetalGraphicsBuffer {
    let bufferObject: MTLBuffer
    let type: GraphicsServiceHeapType
    let sizeInBytes: Int

    init (_ bufferObject: MTLBuffer, _ type: GraphicsServiceHeapType, _ sizeInBytes: Int) {
        self.bufferObject = bufferObject
        self.type = type
        self.sizeInBytes = sizeInBytes
    }
}

class MetalTexture {
    var textureObject: MTLTexture
    let textureDescriptor: MTLTextureDescriptor
    let isPresentTexture: Bool
    var resourceFence: MTLFence?

    init (_ textureObject: MTLTexture, _ textureDescriptor: MTLTextureDescriptor, isPresentTexture: Bool) {
        self.textureObject = textureObject
        self.textureDescriptor = textureDescriptor
        self.isPresentTexture = isPresentTexture
    }
}

class MetalSwapChain {
    let commandQueue: MetalCommandQueue
    let metalLayer: CAMetalLayer
    let textureDescriptor: MTLTextureDescriptor
    var backBufferDrawable: CAMetalDrawable?
    var backBufferTexture: MetalTexture?

    init (_ commandQueue: MetalCommandQueue, _ metalLayer: CAMetalLayer, _ textureDescriptor: MTLTextureDescriptor) {
        self.commandQueue = commandQueue
        self.metalLayer = metalLayer
        self.textureDescriptor = textureDescriptor
    }
}

class MetalIndirectCommandBuffer {
    let commandBufferObject: MTLIndirectCommandBuffer

    init (_ commandBufferObject: MTLIndirectCommandBuffer) {
        self.commandBufferObject = commandBufferObject
    }
}

class MetalQueryBuffer {
    let queryBufferObject: MTLCounterSampleBuffer

    init(_ queryBufferObject: MTLCounterSampleBuffer) {
        self.queryBufferObject = queryBufferObject
    }
}

class MetalShader {
    let vertexShaderFunction: MTLFunction?
    let pixelShaderFunction: MTLFunction?
    let computeShaderFunction: MTLFunction?
    var argumentEncoder: MTLArgumentEncoder?
    var argumentBuffers: [MTLBuffer]
    let argumentBuffersMaxCount = 1000
    var argumentBufferCurrentIndex = 0
    var currentArgumentBuffer: MTLBuffer?

    init(_ device: MTLDevice, _ vertexShaderFunction: MTLFunction?, _ pixelShaderFunction: MTLFunction?, _ computeShaderFunction: MTLFunction?, _ argumentEncoder: MTLArgumentEncoder?, _ debugName: String?) {
        self.vertexShaderFunction = vertexShaderFunction
        self.pixelShaderFunction = pixelShaderFunction
        self.computeShaderFunction = computeShaderFunction
        self.argumentEncoder = nil
        self.argumentBuffers = []
        self.currentArgumentBuffer = nil
    }

    func setArgumentEncoder(_ device: MTLDevice, _ argumentEncoder: MTLArgumentEncoder?) {
        if (argumentEncoder != nil) {
            self.argumentEncoder = argumentEncoder
            // TODO: Use another allocation strategie
            for _ in 0..<argumentBuffersMaxCount {
                let argumentBuffer = device.makeBuffer(length: argumentEncoder!.encodedLength)!
                self.argumentBuffers.append(argumentBuffer)
            }

            self.currentArgumentBuffer = argumentBuffers[0]
            argumentEncoder!.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
        }
    }

    func setupArgumentBuffer() {
        if (argumentEncoder != nil) {
            argumentBufferCurrentIndex += 1

            if (argumentBufferCurrentIndex == argumentBuffersMaxCount) {
                argumentBufferCurrentIndex = 0
            }

            self.currentArgumentBuffer = argumentBuffers[argumentBufferCurrentIndex]
            argumentEncoder!.setArgumentBuffer(self.currentArgumentBuffer, offset: 0)
        }
    }
}

class MetalPipelineState {
    let renderPipelineState: MTLRenderPipelineState?
    let computePipelineState: MTLComputePipelineState?

    init (renderPipelineState: MTLRenderPipelineState) {
        self.renderPipelineState = renderPipelineState
        self.computePipelineState = nil
    }

    init (computePipelineState: MTLComputePipelineState) {
        self.computePipelineState = computePipelineState
        self.renderPipelineState = nil
    }
}

public class MetalGraphicsService: GraphicsServiceProtocol {
    let graphicsDevice: MTLDevice
    let sharedEventListener: MTLSharedEventListener
    var currentShader: MetalShader?
    var currentIndexBuffer: MetalGraphicsBuffer?
    var graphicsHeaps: [MTLHeap]
    var currentComputePipelineState: MetalPipelineState?

    var depthCompareEqualState: MTLDepthStencilState!
    var depthCompareGreaterState: MTLDepthStencilState!
    var depthWriteOperationState: MTLDepthStencilState!
    var depthNoneOperationState: MTLDepthStencilState!

    public init() {
        self.graphicsDevice = MTLCreateSystemDefaultDevice()!
        self.sharedEventListener = MTLSharedEventListener()
        self.graphicsHeaps = []

        print(self.graphicsDevice.name)

        var depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .equal
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthCompareEqualState = self.graphicsDevice.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .greaterEqual
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthCompareGreaterState = self.graphicsDevice.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .greater
        depthStencilDescriptor.isDepthWriteEnabled = true
        self.depthWriteOperationState = self.graphicsDevice.makeDepthStencilState(descriptor: depthStencilDescriptor)!

        depthStencilDescriptor = MTLDepthStencilDescriptor()
        depthStencilDescriptor.depthCompareFunction = .always
        depthStencilDescriptor.isDepthWriteEnabled = false
        self.depthNoneOperationState = self.graphicsDevice.makeDepthStencilState(descriptor: depthStencilDescriptor)!
    }

    public func getGraphicsAdapterName(_ output: UnsafeMutablePointer<Int8>?) {
        let result = self.graphicsDevice.name + " (Metal 3)";
        memcpy(output, strdup(result), result.count)
        output![result.count] = 0
    }

    public func getTextureAllocationInfos(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> GraphicsAllocationInfos {
        let descriptor = createTextureDescriptor(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount)
        let sizeAndAlign = self.graphicsDevice.heapTextureSizeAndAlign(descriptor: descriptor)

        var result = GraphicsAllocationInfos()
        result.SizeInBytes = Int32(sizeAndAlign.size)
        result.Alignment = Int32(sizeAndAlign.align)

        return result
    }

    public func createCommandQueue(_ commandQueueType: GraphicsServiceCommandType) -> UnsafeMutableRawPointer? {
        // TODO: Only create shared event for the present queue?
        guard 
            let commandQueue = self.graphicsDevice.makeCommandQueue(maxCommandBufferCount: 100),
            let fence = self.graphicsDevice.makeSharedEvent(),
            let commandBuffer = commandQueue.makeCommandBuffer()
        else { return nil }

        let nativeCommandQueue = MetalCommandQueue(commandQueue, commandQueueType, fence, commandBuffer)
        return Unmanaged.passRetained(nativeCommandQueue).toOpaque()
    }

    public func setCommandQueueLabel(_ commandQueuePointer: UnsafeMutableRawPointer?, _ label: String) {
        let commandQueue = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).takeUnretainedValue()

        commandQueue.commandQueueObject.label = label
        commandQueue.fence.label = "\(label)Fence"
        commandQueue.commandBuffer.label = "\(label)CommandBuffer"
    }

    public func deleteCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).release()
    }

    public func resetCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?) {
        let commandQueue = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).takeUnretainedValue()

        guard let commandBuffer = commandQueue.commandQueueObject.makeCommandBufferWithUnretainedReferences() else {
            print("resetCommandQueue: Error while creating command buffer object.")
            return
        }
        commandQueue.commandBuffer = commandBuffer
        commandQueue.commandBuffer.label = "\(commandQueue.commandQueueObject.label!)CommandBuffer"
    }

    public func getCommandQueueTimestampFrequency(_ commandQueuePointer: UnsafeMutableRawPointer?) -> UInt {
        // TODO
        return 250000
    }

    public func executeCommandLists(_ commandQueuePointer: UnsafeMutableRawPointer?, _ commandLists: [UnsafeMutableRawPointer?], _ isAwaitable: Bool) -> UInt {
        let commandQueue = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).takeUnretainedValue()
        var fenceValue = UInt64(0)

        if (isAwaitable) {
            fenceValue = commandQueue.fenceValue
            commandQueue.commandBuffer.encodeSignalEvent(commandQueue.fence, value: fenceValue)
            commandQueue.fenceValue = commandQueue.fenceValue + 1
        }

        commandQueue.commandBuffer.commit()

        guard let commandBuffer = commandQueue.commandQueueObject.makeCommandBufferWithUnretainedReferences() else {
            print("resetCommandQueue: Error while creating command buffer object.")
            return 0
        }

        commandQueue.commandBuffer = commandBuffer
        commandQueue.commandBuffer.label = "\(commandQueue.commandQueueObject.label!)CommandBuffer"

        return UInt(fenceValue)
    }

    public func waitForCommandQueue(_ commandQueuePointer: UnsafeMutableRawPointer?, _ commandQueueToWaitPointer: UnsafeMutableRawPointer?, _ fenceValue: UInt) {
        let commandQueue = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).takeUnretainedValue()
        let commandQueueToWait = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueueToWaitPointer!).takeUnretainedValue()

        guard let commandBuffer = commandQueue.commandQueueObject.makeCommandBufferWithUnretainedReferences() else {
            print("waitForCommandQueue: Error while creating command buffer object.")
            return
        }

        commandBuffer.label = "\(commandQueue.commandQueueObject.label!)WaitCommandBuffer"
        commandBuffer.encodeWaitForEvent(commandQueueToWait.fence, value: UInt64(fenceValue))
        commandBuffer.commit()
    }

    public func waitForCommandQueueOnCpu(_ commandQueueToWaitPointer: UnsafeMutableRawPointer?, _ fenceValue: UInt) {
        let commandQueueToWait = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueueToWaitPointer!).takeUnretainedValue()

        if (commandQueueToWait.fence.signaledValue < fenceValue) {
            let group = DispatchGroup()
            group.enter()

            commandQueueToWait.fence.notify(self.sharedEventListener, atValue: UInt64(fenceValue)) { (sEvent, value) in
                group.leave()
            }

            group.wait()
        }
    }

    public func createCommandList(_ commandQueuePointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
        let commandQueue = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).takeUnretainedValue()
        var commandEncoder: MTLCommandEncoder? = nil

        if (commandQueue.commandQueueType == Copy) {
            commandEncoder = commandQueue.commandBuffer.makeBlitCommandEncoder()
        } else if (commandQueue.commandQueueType == Compute) {
            commandEncoder = commandQueue.commandBuffer.makeComputeCommandEncoder()
        }

        let nativeCommandList = MetalCommandList(commandQueue, commandEncoder)
        return Unmanaged.passRetained(nativeCommandList).toOpaque()
    }

    public func setCommandListLabel(_ commandListPointer: UnsafeMutableRawPointer?, _ label: String) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()
        commandList.label = label

        if (commandList.commandEncoder != nil) {
            commandList.commandEncoder!.label = label
        }
    }

    public func deleteCommandList(_ commandListPointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).release()
    }

    public func resetCommandList(_ commandListPointer: UnsafeMutableRawPointer?) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        if (commandList.commandQueue.commandQueueType == Copy) {
            commandList.commandEncoder = commandList.commandQueue.commandBuffer.makeBlitCommandEncoder()
        } else if (commandList.commandQueue.commandQueueType == Compute) {
            commandList.commandEncoder = commandList.commandQueue.commandBuffer.makeComputeCommandEncoder()
        } else {
            commandList.commandEncoder = nil
        }

        commandList.resourceFences = []

        self.currentShader = nil
    }

    public func commitCommandList(_ commandListPointer: UnsafeMutableRawPointer?) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let commandEncoder = commandList.commandEncoder else {
            return
        }

        for i in 0..<commandList.resourceFences.count {
            let resourceFence = commandList.resourceFences[i]

            if (commandList.commandQueue.commandQueueType == Render) {
                guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
                    print("setPipelineState: ERROR: Cannot get renderCommandEncoder")
                    return
                }

                renderCommandEncoder.updateFence(resourceFence, after: .fragment)
            } else if (commandList.commandQueue.commandQueueType == Compute) {
                guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
                    print("setPipelineState: ERROR: Cannot get computeCommandEncoder")
                    return
                }

                computeCommandEncoder.updateFence(resourceFence)
            }
        }

        commandEncoder.endEncoding()
    }

    public func createGraphicsHeap(_ type: GraphicsServiceHeapType, _ length: UInt) -> UnsafeMutableRawPointer? {
        let heapDescriptor = MTLHeapDescriptor()
        heapDescriptor.storageMode = .private
        heapDescriptor.type = .placement
        heapDescriptor.size = Int(length)
        heapDescriptor.hazardTrackingMode = .untracked

        if (type == Upload || type == ReadBack)
        {
            heapDescriptor.storageMode = .managed
            // TODO: Check why metal doesn't allow for upload heaps
            let metalHeap = MetalHeap(type, length, nil)
            return Unmanaged.passRetained(metalHeap).toOpaque()
        }

        if (type == Upload)
        {
            heapDescriptor.cpuCacheMode = .writeCombined
        }

        guard let graphicsHeap = self.graphicsDevice.makeHeap(descriptor: heapDescriptor) else {
            print("createGraphicsHeap: Creation failed.")
            return nil
        }

        self.graphicsHeaps.append(graphicsHeap)

        let metalHeap = MetalHeap(type, length, graphicsHeap)
        return Unmanaged.passRetained(metalHeap).toOpaque()
    }

    public func setGraphicsHeapLabel(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ label: String) {
        let graphicsHeap = Unmanaged<MetalHeap>.fromOpaque(graphicsHeapPointer!).takeUnretainedValue()

        guard let heapObject = graphicsHeap.heapObject else {
            return
        }

        heapObject.label = label
    }

    public func deleteGraphicsHeap(_ graphicsHeapPointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalHeap>.fromOpaque(graphicsHeapPointer!).release()
    }

    public func createGraphicsBuffer(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Bool, _ sizeInBytes: Int) -> UnsafeMutableRawPointer? {
        let graphicsHeap = Unmanaged<MetalHeap>.fromOpaque(graphicsHeapPointer!).takeUnretainedValue()

        var options: MTLResourceOptions = [.storageModePrivate, .hazardTrackingModeUntracked]
        var graphicsBuffer: MTLBuffer

        // Apple Metal doesn't support creating shared memory heaps so we create related
        // buffers via the Device
        if (graphicsHeap.heapObject == nil) {
            // TODO: CPU cache mode write combined seems to be slower on eGPU
            options = [.storageModeShared]//, .cpuCacheModeWriteCombined]

            guard let buffer = self.graphicsDevice.makeBuffer(length: sizeInBytes, options: options) else {
                print("createGraphicsBuffer: Creation failed.")
                return nil
            }

            graphicsBuffer = buffer
        }
        else {
            guard let buffer = graphicsHeap.heapObject!.makeBuffer(length: sizeInBytes, options: options, offset: Int(heapOffset)) else {
                print("createGraphicsBuffer: Creation failed.")
                return nil
            }

            graphicsBuffer = buffer

            if (isAliasable) {
                graphicsBuffer.makeAliasable()
            }
        }

        let nativeGraphicsBuffer = MetalGraphicsBuffer(graphicsBuffer, graphicsHeap.heapType, sizeInBytes)
        return Unmanaged.passRetained(nativeGraphicsBuffer).toOpaque()
    }

    public func setGraphicsBufferLabel(_ graphicsBufferPointer: UnsafeMutableRawPointer?, _ label: String) {
        let graphicsBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(graphicsBufferPointer!).takeUnretainedValue()
        graphicsBuffer.bufferObject.label = label
    }

    public func deleteGraphicsBuffer(_ graphicsBufferPointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalGraphicsBuffer>.fromOpaque(graphicsBufferPointer!).release()
    }

    public func getGraphicsBufferCpuPointer(_ graphicsBufferPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
        let graphicsBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(graphicsBufferPointer!).takeUnretainedValue()
        return graphicsBuffer.bufferObject.contents()
    }

    public func createTexture(_ graphicsHeapPointer: UnsafeMutableRawPointer?, _ heapOffset: UInt, _ isAliasable: Bool, _ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> UnsafeMutableRawPointer? {
        let graphicsHeap = Unmanaged<MetalHeap>.fromOpaque(graphicsHeapPointer!).takeUnretainedValue()
        let descriptor = createTextureDescriptor(textureFormat, usage, width, height, faceCount, mipLevels, multisampleCount)

        if (graphicsHeap.heapObject == nil) {
            return nil
        }

        guard let gpuTexture = graphicsHeap.heapObject!.makeTexture(descriptor: descriptor, offset: Int(heapOffset)) else {
            print("createTexture: Creation failed.")
            return nil
        }

        if (isAliasable) {
            gpuTexture.makeAliasable()
        }

        let nativeTexture = MetalTexture(gpuTexture, descriptor, isPresentTexture: false)
        return Unmanaged.passRetained(nativeTexture).toOpaque()
    }

    public func setTextureLabel(_ texturePointer: UnsafeMutableRawPointer?, _ label: String) {
        let texture = Unmanaged<MetalTexture>.fromOpaque(texturePointer!).takeUnretainedValue()
        texture.textureObject.label = label
    }

    public func deleteTexture(_ texturePointer: UnsafeMutableRawPointer?) {
        // TODO: There is a problem here. XCode crash

        Unmanaged<MetalTexture>.fromOpaque(texturePointer!).release()
    }

    public func createSwapChain(_ windowPointer: UnsafeMutableRawPointer?, _ commandQueuePointer: UnsafeMutableRawPointer?, _ width: Int, _ height: Int, _ textureFormat: GraphicsTextureFormat) -> UnsafeMutableRawPointer? {
        let window = Unmanaged<MacOSWindow>.fromOpaque(windowPointer!).takeUnretainedValue()
        let commandQueue = Unmanaged<MetalCommandQueue>.fromOpaque(commandQueuePointer!).takeUnretainedValue()

        let metalLayer = window.metalView.metalLayer
        metalLayer.device = self.graphicsDevice
        metalLayer.pixelFormat = .bgra8Unorm_srgb
        metalLayer.framebufferOnly = true
        metalLayer.allowsNextDrawableTimeout = true
        metalLayer.displaySyncEnabled = true
        metalLayer.maximumDrawableCount = 2
        metalLayer.drawableSize = CGSize(width: width, height: height)

        let descriptor = createTextureDescriptor(textureFormat, RenderTarget, width, height, 1, 1, 1)

        let nativeSwapChain = MetalSwapChain(commandQueue, metalLayer, descriptor)
        return Unmanaged.passRetained(nativeSwapChain).toOpaque()
    }

    public func getSwapChainBackBufferTexture(_ swapChainPointer: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
        let swapChain = Unmanaged<MetalSwapChain>.fromOpaque(swapChainPointer!).takeUnretainedValue()

        guard let nextMetalDrawable = swapChain.metalLayer.nextDrawable() else {
            return nil
        }

        if (swapChain.backBufferTexture == nil) {
            swapChain.backBufferTexture = MetalTexture(nextMetalDrawable.texture, swapChain.textureDescriptor, isPresentTexture: true)
            swapChain.backBufferDrawable = nextMetalDrawable
        } else {
            swapChain.backBufferTexture!.textureObject = nextMetalDrawable.texture
            swapChain.backBufferDrawable = nextMetalDrawable
        }

        return Unmanaged.passRetained(swapChain.backBufferTexture!).toOpaque()
    }

    public func presentSwapChain(_ swapChainPointer: UnsafeMutableRawPointer?) -> UInt {
        let swapChain = Unmanaged<MetalSwapChain>.fromOpaque(swapChainPointer!).takeUnretainedValue()
        swapChain.commandQueue.commandBuffer.present(swapChain.backBufferDrawable!)

        let fenceValue = swapChain.commandQueue.fenceValue
        swapChain.commandQueue.commandBuffer.encodeSignalEvent(swapChain.commandQueue.fence, value: fenceValue)
        swapChain.commandQueue.fenceValue = swapChain.commandQueue.fenceValue + 1

        swapChain.commandQueue.commandBuffer.commit()

        guard let commandBuffer = swapChain.commandQueue.commandQueueObject.makeCommandBuffer() else {
            print("resetCommandQueue: Error while creating command buffer object.")
            return 0
        }

        swapChain.commandQueue.commandBuffer = commandBuffer
        swapChain.commandQueue.commandBuffer.label = "\(swapChain.commandQueue.commandQueueObject.label!)CommandBuffer"

        return UInt(fenceValue)
    }

    public func createIndirectCommandBuffer(_ maxCommandCount: Int) -> UnsafeMutableRawPointer? {
        let indirectCommandBufferDescriptor = MTLIndirectCommandBufferDescriptor()
        
        indirectCommandBufferDescriptor.commandTypes = [.drawIndexed]
        indirectCommandBufferDescriptor.inheritBuffers = false
        indirectCommandBufferDescriptor.maxVertexBufferBindCount = 5
        indirectCommandBufferDescriptor.maxFragmentBufferBindCount = 5
        indirectCommandBufferDescriptor.inheritPipelineState = true

        guard let indirectCommandBuffer = self.graphicsDevice.makeIndirectCommandBuffer(descriptor: indirectCommandBufferDescriptor, maxCommandCount: maxCommandCount, options: .storageModePrivate) else {
            print("createIndirectCommandBuffer: Creation failed.")
            return nil
        }

        let nativeIndirectCommandBuffer = MetalIndirectCommandBuffer(indirectCommandBuffer)
        return Unmanaged.passRetained(nativeIndirectCommandBuffer).toOpaque()
    }

    public func setIndirectCommandBufferLabel(_ indirectCommandBufferPointer: UnsafeMutableRawPointer?, _ label: String) {
        let indirectCommandBuffer = Unmanaged<MetalIndirectCommandBuffer>.fromOpaque(indirectCommandBufferPointer!).takeUnretainedValue()
        indirectCommandBuffer.commandBufferObject.label = label
    }

    public func deleteIndirectCommandBuffer(_ indirectCommandBufferPointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalIndirectCommandBuffer>.fromOpaque(indirectCommandBufferPointer!).release()
    }

    public func createQueryBuffer(_ queryBufferType: GraphicsQueryBufferType, _ length: Int) -> UnsafeMutableRawPointer? {
        guard let counterSets = self.graphicsDevice.counterSets else {
            print("createQueryBuffer: Counter sets are not available.")
            return nil
        }

        var foundCounterSet: MTLCounterSet? = nil
        
        for systemCounterSet in counterSets {
            if (systemCounterSet.name == "timestamp" as String && (queryBufferType == Timestamp || queryBufferType == CopyTimestamp)) {
                foundCounterSet = systemCounterSet
                break
            }
        }

        guard let counterSet = foundCounterSet else {
            print("createQueryBuffer: Counter was not found.")
            return nil
        }

        let descriptor = MTLCounterSampleBufferDescriptor()
        descriptor.counterSet = counterSet
        descriptor.sampleCount = length
        descriptor.storageMode = .shared

        do {
            let queryBuffer = try self.graphicsDevice.makeCounterSampleBuffer(descriptor: descriptor)

            let nativeQueryBuffer = MetalQueryBuffer(queryBuffer)
            return Unmanaged.passRetained(nativeQueryBuffer).toOpaque()
        } catch {
            print("Failed to create query buffer, \(error)")
            return nil
        }
    }

    public func setQueryBufferLabel(_ queryBufferPointer: UnsafeMutableRawPointer?, _ label: String) {
    }

    public func deleteQueryBuffer(_ queryBufferPointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalQueryBuffer>.fromOpaque(queryBufferPointer!).release()
    }

    public func createShader(_ computeShaderFunction: String?, _ shaderByteCode: UnsafeMutableRawPointer, _ shaderByteCodeLength: Int) -> UnsafeMutableRawPointer? {
        let dispatchData = DispatchData(bytes: UnsafeRawBufferPointer(start: shaderByteCode, count: shaderByteCodeLength))
        let defaultLibrary = try! self.graphicsDevice.makeLibrary(data: dispatchData as __DispatchData)

        if (computeShaderFunction == nil) {
            let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")!
            let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

            let nativeShader = MetalShader(self.graphicsDevice, vertexFunction, fragmentFunction, nil, nil, "Shader")
            return Unmanaged.passRetained(nativeShader).toOpaque()
        } else {
            let computeFunction = defaultLibrary.makeFunction(name: computeShaderFunction!)!

            let argumentEncoder = computeFunction.makeArgumentEncoder(bufferIndex: 0)
            let nativeShader = MetalShader(self.graphicsDevice, nil, nil, computeFunction, argumentEncoder, "Shader")
            return Unmanaged.passRetained(nativeShader).toOpaque()
        }
    }

    public func setShaderLabel(_ shaderPointer: UnsafeMutableRawPointer?, _ label: String) {
        let shader = Unmanaged<MetalShader>.fromOpaque(shaderPointer!).takeUnretainedValue()

        // TODO: remove that hack!
        if (shader.computeShaderFunction != nil) {
            let argumentEncoder = shader.computeShaderFunction!.makeArgumentEncoder(bufferIndex: 0)
            shader.setArgumentEncoder(self.graphicsDevice, argumentEncoder)
        } else if (label != "RenderMeshInstanceDepthShader" && label != "RenderMeshInstanceDepthMomentShader" && label != "RenderMeshInstanceShader" && label != "RenderMeshInstanceTransparentShader" && label != "RenderMeshInstanceTransparentDepthShader") {
            if (shader.vertexShaderFunction != nil) {
                let argumentEncoder = shader.vertexShaderFunction!.makeArgumentEncoder(bufferIndex: 0)
                shader.setArgumentEncoder(self.graphicsDevice, argumentEncoder)
            } 
        }
    }

    public func deleteShader(_ shaderPointer: UnsafeMutableRawPointer?) {
        // TODO: remove that hack!
        if (self.currentShader != nil) {
            let currentShaderPointer = Unmanaged.passUnretained(self.currentShader!).toOpaque()

            if (currentShaderPointer == shaderPointer) {
                self.currentShader = nil
            }
        }

        Unmanaged<MetalShader>.fromOpaque(shaderPointer!).release()
    }

    public func createPipelineState(_ shaderPointer: UnsafeMutableRawPointer?, _ metalRenderPassDescriptor: GraphicsRenderPassDescriptor) -> UnsafeMutableRawPointer? {
        let shader = Unmanaged<MetalShader>.fromOpaque(shaderPointer!).takeUnretainedValue()

        if (metalRenderPassDescriptor.IsRenderShader == 1) {
            let pipelineStateDescriptor = MTLRenderPipelineDescriptor()

            pipelineStateDescriptor.vertexFunction = shader.vertexShaderFunction!

            if (shader.pixelShaderFunction != nil)
            {
                pipelineStateDescriptor.fragmentFunction = shader.pixelShaderFunction!
            }

            pipelineStateDescriptor.supportIndirectCommandBuffers = true
            pipelineStateDescriptor.sampleCount = (metalRenderPassDescriptor.MultiSampleCount.HasValue == 1) ? Int(metalRenderPassDescriptor.MultiSampleCount.Value) : 1

            // TODO: Use the correct render target format
            if (metalRenderPassDescriptor.RenderTarget1TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[0].pixelFormat = convertTextureFormat(metalRenderPassDescriptor.RenderTarget1TextureFormat.Value)
            }

            if (metalRenderPassDescriptor.RenderTarget2TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[1].pixelFormat = convertTextureFormat(metalRenderPassDescriptor.RenderTarget2TextureFormat.Value)
            }

            if (metalRenderPassDescriptor.RenderTarget3TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[2].pixelFormat = convertTextureFormat(metalRenderPassDescriptor.RenderTarget3TextureFormat.Value)
            }

            if (metalRenderPassDescriptor.RenderTarget4TextureFormat.HasValue == 1) {
                pipelineStateDescriptor.colorAttachments[3].pixelFormat = convertTextureFormat(metalRenderPassDescriptor.RenderTarget4TextureFormat.Value)
            }

            if (metalRenderPassDescriptor.DepthTexturePointer.HasValue == 1) {
                pipelineStateDescriptor.depthAttachmentPixelFormat = .depth32Float
            } 

            if (metalRenderPassDescriptor.RenderTarget1BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[0]!, metalRenderPassDescriptor.RenderTarget1BlendOperation.Value)
            }

            if (metalRenderPassDescriptor.RenderTarget2BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[1]!, metalRenderPassDescriptor.RenderTarget2BlendOperation.Value)
            }

            if (metalRenderPassDescriptor.RenderTarget3BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[2]!, metalRenderPassDescriptor.RenderTarget3BlendOperation.Value)
            }

            if (metalRenderPassDescriptor.RenderTarget4BlendOperation.HasValue == 1) {
                initBlendState(pipelineStateDescriptor.colorAttachments[3]!, metalRenderPassDescriptor.RenderTarget4BlendOperation.Value)
            }

            do {
                let pipelineState = try self.graphicsDevice.makeRenderPipelineState(descriptor: pipelineStateDescriptor)

                let nativePipelineState = MetalPipelineState(renderPipelineState: pipelineState)
                return Unmanaged.passRetained(nativePipelineState).toOpaque()
            } catch {
                print("Failed to created pipeline state, \(error)")
            }
        } else {
            do {
                let pipelineState = try self.graphicsDevice.makeComputePipelineState(function: shader.computeShaderFunction!)

                let nativePipelineState = MetalPipelineState(computePipelineState: pipelineState)
                return Unmanaged.passRetained(nativePipelineState).toOpaque()
            }
            catch {
                print("Failed to created pipeline state, \(error)")
            }
        }

        return nil
    }

    public func setPipelineStateLabel(_ pipelineStatePointer: UnsafeMutableRawPointer?, _ label: String) {

    }

    public func deletePipelineState(_ pipelineStatePointer: UnsafeMutableRawPointer?) {
        Unmanaged<MetalPipelineState>.fromOpaque(pipelineStatePointer!).release()
    }

    public func setShaderBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?, _ slot: Int, _ isReadOnly: Bool, _ index: Int) {
        let graphicsBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(graphicsBufferPointer!).takeUnretainedValue()
        
        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        argumentEncoder.setBuffer(graphicsBuffer.bufferObject, offset: index, index: slot)
    }

    public func setShaderBuffers(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointerList: [UnsafeMutableRawPointer?], _ slot: Int, _ index: Int) {
        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var graphicsBufferList: [MTLBuffer] = []
        var offsets: [Int] = []

        for i in 0..<graphicsBufferPointerList.count {
            let graphicsBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(graphicsBufferPointerList[i]!).takeUnretainedValue()

            graphicsBufferList.append(graphicsBuffer.bufferObject)
            offsets.append(0)
        }

        argumentEncoder.setBuffers(graphicsBufferList, offsets: offsets, range: (slot + index)..<(slot + index) + graphicsBufferPointerList.count)
    }

    public func setShaderTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ texturePointer: UnsafeMutableRawPointer?, _ slot: Int, _ isReadOnly: Bool, _ index: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()
        let texture = Unmanaged<MetalTexture>.fromOpaque(texturePointer!).takeUnretainedValue()

        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        if (texture.textureObject.usage == [.shaderRead, .renderTarget] || texture.textureObject.usage == [.shaderRead, .shaderWrite]) {
            if (commandList.commandQueue.commandQueueType == Compute) {
                guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
                    print("setShaderTexture: Wrong encoder type.")
                    return
                }

                // TODO: Something better here (Manage that like a transition barrier based on the previous state of the resource)

                if (texture.resourceFence != nil) {
                    computeCommandEncoder.waitForFence(texture.resourceFence!)
                    texture.resourceFence = nil
                }

                if (isReadOnly) {
                    computeCommandEncoder.useResource(texture.textureObject, usage: .read)
                } else {
                    computeCommandEncoder.useResource(texture.textureObject, usage: .write)

                    let resourceFence = self.graphicsDevice.makeFence()!
                    texture.resourceFence = resourceFence
                    commandList.resourceFences.append(resourceFence)
                }

            } else if (commandList.commandQueue.commandQueueType == Render) {
                guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
                    print("setShaderTexture: Wrong encoder type.")
                    return
                }

                if (texture.resourceFence != nil) {
                    renderCommandEncoder.waitForFence(texture.resourceFence!, before: .vertex)
                    texture.resourceFence = nil
                }

                if (isReadOnly) {
                    renderCommandEncoder.useResource(texture.textureObject, usage: .read)
                } else {
                    renderCommandEncoder.useResource(texture.textureObject, usage: .write)
                }
            }
        }

        argumentEncoder.setTexture(texture.textureObject, index: slot)
    }

    public func setShaderTextures(_ commandListPointer: UnsafeMutableRawPointer?, _ texturePointerList: [UnsafeMutableRawPointer?], _ slot: Int, _ index: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        var textureList: [MTLTexture] = []

        for i in 0..<texturePointerList.count {
            let texture = Unmanaged<MetalTexture>.fromOpaque(texturePointerList[i]!).takeUnretainedValue()

            if (texture.textureObject.usage == [.shaderRead, .renderTarget] || texture.textureObject.usage == [.shaderRead, .shaderWrite]) {
                if (commandList.commandQueue.commandQueueType == Compute) {
                    guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
                        print("setShaderTextures: Wrong encoder type.")
                        return
                    }

                    if (texture.resourceFence != nil) {
                        computeCommandEncoder.waitForFence(texture.resourceFence!)
                        texture.resourceFence = nil
                    }

                    computeCommandEncoder.useResource(texture.textureObject, usage: .read)
                } else if (commandList.commandQueue.commandQueueType == Render) {
                    guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
                        print("setShaderTextures: Wrong encoder type.")
                        return
                    }

                    if (texture.resourceFence != nil) {
                        renderCommandEncoder.waitForFence(texture.resourceFence!, before: .vertex)
                        texture.resourceFence = nil
                    }

                    renderCommandEncoder.useResource(texture.textureObject, usage: .read)
                }
            }

            textureList.append(texture.textureObject)
        }

        argumentEncoder.setTextures(textureList, range: (slot + index)..<(slot + index) + texturePointerList.count)
    }

    public func setShaderIndirectCommandList(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ slot: Int, _ index: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()
        let indirectCommandBuffer = Unmanaged<MetalIndirectCommandBuffer>.fromOpaque(indirectCommandListPointer!).takeUnretainedValue()

        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
            return
        }
        
        argumentEncoder.setIndirectCommandBuffer(indirectCommandBuffer.commandBufferObject, index: slot)
        computeCommandEncoder.useResource(indirectCommandBuffer.commandBufferObject, usage: .write)
    }

    public func setShaderIndirectCommandLists(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointerList: [UnsafeMutableRawPointer?], _ slot: Int, _ index: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let shader = self.currentShader else {
            return
        }

        guard let argumentEncoder = shader.argumentEncoder else {
            return
        }

        guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
            return
        }

        var commandBufferList: [MTLIndirectCommandBuffer] = []

        for i in 0..<indirectCommandListPointerList.count {
            let indirectCommandBuffer = Unmanaged<MetalIndirectCommandBuffer>.fromOpaque(indirectCommandListPointerList[i]!).takeUnretainedValue()
           
            commandBufferList.append(indirectCommandBuffer.commandBufferObject)
            computeCommandEncoder.useResource(indirectCommandBuffer.commandBufferObject, usage: .write)
        }

        argumentEncoder.setIndirectCommandBuffers(commandBufferList, range: (slot + index)..<(slot + index) + indirectCommandListPointerList.count)
    }

    public func copyDataToGraphicsBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationGraphicsBufferPointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ length: Int) {
        let destinationBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(destinationGraphicsBufferPointer!).takeUnretainedValue()
        let sourceBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(sourceGraphicsBufferPointer!).takeUnretainedValue()

        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let copyCommandEncoder = commandList.commandEncoder as? MTLBlitCommandEncoder else {
            return
        }

        copyCommandEncoder.copy(from: sourceBuffer.bufferObject, sourceOffset: 0, to: destinationBuffer.bufferObject, destinationOffset: 0, size: length)
    }

    public func copyDataToTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceGraphicsBufferPointer: UnsafeMutableRawPointer?, _ textureFormat: GraphicsTextureFormat, _ width: Int, _ height: Int, _ slice: Int, _ mipLevel: Int) {
        let destinationTexture = Unmanaged<MetalTexture>.fromOpaque(destinationTexturePointer!).takeUnretainedValue()
        let sourceGraphicsBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(sourceGraphicsBufferPointer!).takeUnretainedValue()

        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let copyCommandEncoder = commandList.commandEncoder as? MTLBlitCommandEncoder else {
            return
        }

        var sourceBytesPerRow = 4 * width
        var sourceBytesPerImage = 4 * width * height

        if (textureFormat == BC2Srgb || textureFormat == BC3Srgb || textureFormat == BC5 || textureFormat == BC6 || textureFormat == BC7Srgb) {
            sourceBytesPerRow = 16 * Int(ceil(Double(width) / 4.0))
            sourceBytesPerImage = 16 * Int(ceil(Double(width) / 4.0)) * Int(ceil(Double(height) / 4.0))
        } else if (textureFormat == BC1Srgb || textureFormat == BC4) {
            sourceBytesPerRow = 8 * Int(ceil(Double(width) / 4.0))
            sourceBytesPerImage = 8 * Int(ceil(Double(width) / 4.0)) * Int(ceil(Double(height) / 4.0))
        } else if (textureFormat == Rgba16Float) {
            sourceBytesPerRow = 8 * width
            sourceBytesPerImage = 8 * width * height
        } else if (textureFormat == Rgba32Float) {
            sourceBytesPerRow = 16 * width
            sourceBytesPerImage = 16 * width * height
        }

        copyCommandEncoder.copy(from: sourceGraphicsBuffer.bufferObject, 
                                sourceOffset: 0, 
                                sourceBytesPerRow: sourceBytesPerRow,
                                sourceBytesPerImage: sourceBytesPerImage,
                                sourceSize: MTLSize(width: width, height: height, depth: 1),
                                to: destinationTexture.textureObject, 
                                destinationSlice: slice,
                                destinationLevel: mipLevel,
                                destinationOrigin: MTLOrigin(x: 0, y: 0, z: 0))
    }

    public func copyTexture(_ commandListPointer: UnsafeMutableRawPointer?, _ destinationTexturePointer: UnsafeMutableRawPointer?, _ sourceTexturePointer: UnsafeMutableRawPointer?) {

    }

    public func resetIndirectCommandList(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int) {

    }

    public func optimizeIndirectCommandList(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandListPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int) {

    }

    public func dispatchThreads(_ commandListPointer: UnsafeMutableRawPointer?, _ threadCountX: UInt, _ threadCountY: UInt, _ threadCountZ: UInt) -> Vector3 {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
            return Vector3(X: Float(0), Y: Float(0), Z: Float(0))
        }

        guard let currentShader = self.currentShader else {
            print("dispatchThreads: Current Shader is nil.")
            return Vector3(X: Float(0), Y: Float(0), Z: Float(0))
        }

        guard let computePipelineState = self.currentComputePipelineState else {
            print("dispatchThreads: Current Pipeline state is nil.")
            return Vector3(X: Float(0), Y: Float(0), Z: Float(0))
        }

        computeCommandEncoder.setBuffer(currentShader.currentArgumentBuffer, offset: 0, index: 0)

        let w = computePipelineState.computePipelineState!.threadExecutionWidth
        let h = (threadCountY > 1) ? computePipelineState.computePipelineState!.maxTotalThreadsPerThreadgroup / w : 1
        let threadsPerGroup = MTLSizeMake(w, h, 1)

        computeCommandEncoder.dispatchThreads(MTLSize(width: Int(threadCountX), height: Int(threadCountY), depth: Int(threadCountZ)), threadsPerThreadgroup: threadsPerGroup)
        currentShader.setupArgumentBuffer()

        return Vector3(X: Float(w), Y: Float(h), Z: Float(1))
    }

    public func beginRenderPass(_ commandListPointer: UnsafeMutableRawPointer?, _ renderPassDescriptor: GraphicsRenderPassDescriptor) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()
        var renderTargetList: [MTLTexture] = []

        // Create render command encoder        
        let metalRenderPassDescriptor = MTLRenderPassDescriptor()
        
        if (renderPassDescriptor.RenderTarget1ClearColor.HasValue == 1) {
            metalRenderPassDescriptor.colorAttachments[0].loadAction = .clear
            metalRenderPassDescriptor.colorAttachments[0].clearColor = MTLClearColor.init(red: Double(renderPassDescriptor.RenderTarget1ClearColor.Value.X), green: Double(renderPassDescriptor.RenderTarget1ClearColor.Value.Y), blue: Double(renderPassDescriptor.RenderTarget1ClearColor.Value.Z), alpha: Double(renderPassDescriptor.RenderTarget1ClearColor.Value.W))
        } else {
            metalRenderPassDescriptor.colorAttachments[0].loadAction = .load
        }

        if (renderPassDescriptor.RenderTarget1TexturePointer.HasValue == 1) {
            let colorTexture = Unmanaged<MetalTexture>.fromOpaque(renderPassDescriptor.RenderTarget1TexturePointer.Value!).takeUnretainedValue()

            renderTargetList.append(colorTexture.textureObject)
            metalRenderPassDescriptor.colorAttachments[0].texture = colorTexture.textureObject

            if (colorTexture.isPresentTexture) {
                metalRenderPassDescriptor.colorAttachments[0].loadAction = .dontCare
                metalRenderPassDescriptor.colorAttachments[0].storeAction = .dontCare
            } else {
                let resourceFence = self.graphicsDevice.makeFence()!
                colorTexture.resourceFence = resourceFence

                commandList.resourceFences.append(resourceFence)

                metalRenderPassDescriptor.colorAttachments[0].storeAction = .store
            }
        }

        if (renderPassDescriptor.RenderTarget2TexturePointer.HasValue == 1) {
            let colorTexture = Unmanaged<MetalTexture>.fromOpaque(renderPassDescriptor.RenderTarget2TexturePointer.Value!).takeUnretainedValue()

            renderTargetList.append(colorTexture.textureObject)
            metalRenderPassDescriptor.colorAttachments[1].texture = colorTexture.textureObject
            metalRenderPassDescriptor.colorAttachments[1].storeAction = .store

            if (renderPassDescriptor.RenderTarget2ClearColor.HasValue == 1) {
                metalRenderPassDescriptor.colorAttachments[1].loadAction = .clear
                metalRenderPassDescriptor.colorAttachments[1].clearColor = MTLClearColor.init(red: Double(renderPassDescriptor.RenderTarget2ClearColor.Value.X), green: Double(renderPassDescriptor.RenderTarget2ClearColor.Value.Y), blue: Double(renderPassDescriptor.RenderTarget2ClearColor.Value.Z), alpha: Double(renderPassDescriptor.RenderTarget2ClearColor.Value.W))
            } else {
                metalRenderPassDescriptor.colorAttachments[1].loadAction = .load
            }
        }

        if (renderPassDescriptor.RenderTarget3TexturePointer.HasValue == 1) {
            let colorTexture = Unmanaged<MetalTexture>.fromOpaque(renderPassDescriptor.RenderTarget3TexturePointer.Value!).takeUnretainedValue()

            renderTargetList.append(colorTexture.textureObject)
            metalRenderPassDescriptor.colorAttachments[2].texture = colorTexture.textureObject
            metalRenderPassDescriptor.colorAttachments[2].storeAction = .store

            if (renderPassDescriptor.RenderTarget3ClearColor.HasValue == 1) {
                metalRenderPassDescriptor.colorAttachments[2].loadAction = .clear
                metalRenderPassDescriptor.colorAttachments[2].clearColor = MTLClearColor.init(red: Double(renderPassDescriptor.RenderTarget3ClearColor.Value.X), green: Double(renderPassDescriptor.RenderTarget3ClearColor.Value.Y), blue: Double(renderPassDescriptor.RenderTarget3ClearColor.Value.Z), alpha: Double(renderPassDescriptor.RenderTarget3ClearColor.Value.W))
            } else {
                metalRenderPassDescriptor.colorAttachments[2].loadAction = .load
            }
        }

        if (renderPassDescriptor.RenderTarget4TexturePointer.HasValue == 1) {
            let colorTexture = Unmanaged<MetalTexture>.fromOpaque(renderPassDescriptor.RenderTarget4TexturePointer.Value!).takeUnretainedValue()

            renderTargetList.append(colorTexture.textureObject)
            metalRenderPassDescriptor.colorAttachments[3].texture = colorTexture.textureObject
            metalRenderPassDescriptor.colorAttachments[3].storeAction = .store

            if (renderPassDescriptor.RenderTarget4ClearColor.HasValue == 1) {
                metalRenderPassDescriptor.colorAttachments[3].loadAction = .clear
                metalRenderPassDescriptor.colorAttachments[3].clearColor = MTLClearColor.init(red: Double(renderPassDescriptor.RenderTarget4ClearColor.Value.X), green: Double(renderPassDescriptor.RenderTarget4ClearColor.Value.Y), blue: Double(renderPassDescriptor.RenderTarget4ClearColor.Value.Z), alpha: Double(renderPassDescriptor.RenderTarget4ClearColor.Value.W))
            } else {
                metalRenderPassDescriptor.colorAttachments[3].loadAction = .load
            }
        }

        if (renderPassDescriptor.DepthTexturePointer.HasValue == 1) {
            let depthTexture = Unmanaged<MetalTexture>.fromOpaque(renderPassDescriptor.DepthTexturePointer.Value!).takeUnretainedValue()

            renderTargetList.append(depthTexture.textureObject)
            metalRenderPassDescriptor.depthAttachment.texture = depthTexture.textureObject
        }

        if (renderPassDescriptor.DepthBufferOperation != DepthNone) {
            if (renderPassDescriptor.DepthBufferOperation == ClearWrite) {
                metalRenderPassDescriptor.depthAttachment.loadAction = .clear
                metalRenderPassDescriptor.depthAttachment.clearDepth = 0.0
            } else {
                metalRenderPassDescriptor.depthAttachment.loadAction = .load
            }

            if (renderPassDescriptor.DepthBufferOperation == Write || renderPassDescriptor.DepthBufferOperation == ClearWrite) {
                metalRenderPassDescriptor.depthAttachment.storeAction = .store
            }
        } else {
            metalRenderPassDescriptor.depthAttachment.storeAction = .dontCare
        }
        
        guard let renderCommandEncoder = commandList.commandQueue.commandBuffer.makeRenderCommandEncoder(descriptor: metalRenderPassDescriptor) else {
            print("beginRenderPass: Render command encoder creation failed.")
            return
        }

        renderCommandEncoder.label = commandList.label
        commandList.commandEncoder = renderCommandEncoder

        if (renderPassDescriptor.DepthBufferOperation == Write || renderPassDescriptor.DepthBufferOperation == ClearWrite) {
            renderCommandEncoder.setDepthStencilState(self.depthWriteOperationState)
        } else if (renderPassDescriptor.DepthBufferOperation == CompareEqual) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareEqualState)
        } else if (renderPassDescriptor.DepthBufferOperation == CompareGreater) {
            renderCommandEncoder.setDepthStencilState(self.depthCompareGreaterState)
        } else {
            renderCommandEncoder.setDepthStencilState(self.depthNoneOperationState)
        }

        if (renderPassDescriptor.BackfaceCulling == 1) {
            renderCommandEncoder.setCullMode(.back)
        } else {
            renderCommandEncoder.setCullMode(.none)
        }

        if (metalRenderPassDescriptor.colorAttachments[0].texture != nil) {
            renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(metalRenderPassDescriptor.colorAttachments[0].texture!.width), height: Double(metalRenderPassDescriptor.colorAttachments[0].texture!.height), znear: 0.0, zfar: 1.0))
            renderCommandEncoder.setScissorRect(MTLScissorRect(x: 0, y: 0, width: metalRenderPassDescriptor.colorAttachments[0].texture!.width, height: metalRenderPassDescriptor.colorAttachments[0].texture!.height))
        } else if (metalRenderPassDescriptor.depthAttachment.texture != nil) {
            renderCommandEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(metalRenderPassDescriptor.depthAttachment.texture!.width), height: Double(metalRenderPassDescriptor.depthAttachment.texture!.height), znear: 0.0, zfar: 1.0))
            renderCommandEncoder.setScissorRect(MTLScissorRect(x: 0, y: 0, width: metalRenderPassDescriptor.depthAttachment.texture!.width, height: metalRenderPassDescriptor.depthAttachment.texture!.height))
        }

        commandList.renderTargets = renderTargetList
    }

    public func endRenderPass(_ commandListPointer: UnsafeMutableRawPointer?) {

    }

    public func setPipelineState(_ commandListPointer: UnsafeMutableRawPointer?, _ pipelineStatePointer: UnsafeMutableRawPointer?) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()
        let pipelineState = Unmanaged<MetalPipelineState>.fromOpaque(pipelineStatePointer!).takeUnretainedValue()
        if (commandList.commandQueue.commandQueueType == Render) {
            guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
                print("setPipelineState: ERROR: Cannot get renderCommandEncoder")
                return
            }

            renderCommandEncoder.setRenderPipelineState(pipelineState.renderPipelineState!)
            renderCommandEncoder.useHeaps(self.graphicsHeaps)
            renderCommandEncoder.useResources(commandList.renderTargets, usage: [.read, .write])
        } else if (commandList.commandQueue.commandQueueType == Compute) {
            guard let computeCommandEncoder = commandList.commandEncoder as? MTLComputeCommandEncoder else {
                print("setPipelineState: ERROR: Cannot get computeCommandEncoder")
                return
            }

            computeCommandEncoder.setComputePipelineState(pipelineState.computePipelineState!)
            computeCommandEncoder.useHeaps(self.graphicsHeaps)
            self.currentComputePipelineState = pipelineState
        }
    }

    public func setShader(_ commandListPointer: UnsafeMutableRawPointer?, _ shaderPointer: UnsafeMutableRawPointer?) {
        let shader = Unmanaged<MetalShader>.fromOpaque(shaderPointer!).takeUnretainedValue()
        self.currentShader = shader
        shader.setupArgumentBuffer()
    }

    public func executeIndirectCommandBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ indirectCommandBufferPointer: UnsafeMutableRawPointer?, _ maxCommandCount: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()
        let indirectCommandBuffer = Unmanaged<MetalIndirectCommandBuffer>.fromOpaque(indirectCommandBufferPointer!).takeUnretainedValue()
        
        if (self.currentShader == nil) {
            return
        }

        guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
            print("setPipelineState: ERROR: Cannot get renderCommandEncoder")
            return
        }
       
        renderCommandEncoder.executeCommandsInBuffer(indirectCommandBuffer.commandBufferObject, range: 0..<maxCommandCount)
    }

    public func setIndexBuffer(_ commandListPointer: UnsafeMutableRawPointer?, _ graphicsBufferPointer: UnsafeMutableRawPointer?) {
        let graphicsBuffer = Unmanaged<MetalGraphicsBuffer>.fromOpaque(graphicsBufferPointer!).takeUnretainedValue()
        self.currentIndexBuffer = graphicsBuffer
    }

    public func drawIndexedPrimitives(_ commandListPointer: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startIndex: Int, _ indexCount: Int, _ instanceCount: Int, _ baseInstanceId: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let indexBuffer = self.currentIndexBuffer else {
            print("drawPrimitives: Index Buffer is nil.")
            return
        }

        guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
            print("setPipelineState: ERROR: Cannot get renderCommandEncoder")
            return
        }
        
        if (self.currentShader != nil) {
            renderCommandEncoder.setVertexBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
            renderCommandEncoder.setFragmentBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
        }

        let startIndexOffset = Int(startIndex * 4)
        var primitiveTypeMetal = MTLPrimitiveType.triangle

        if (primitiveType == TriangleStrip) {
            primitiveTypeMetal = MTLPrimitiveType.triangleStrip
        } else if (primitiveType == Line) {
            primitiveTypeMetal = MTLPrimitiveType.line
        }

        renderCommandEncoder.drawIndexedPrimitives(type: primitiveTypeMetal, 
                                                   indexCount: Int(indexCount), 
                                                   indexType: .uint32, 
                                                   indexBuffer: indexBuffer.bufferObject, 
                                                   indexBufferOffset: startIndexOffset, 
                                                   instanceCount: instanceCount, 
                                                   baseVertex: 0, 
                                                   baseInstance: Int(baseInstanceId))

        if (self.currentShader != nil) {
            self.currentShader!.setupArgumentBuffer()
        }
    }

    public func drawPrimitives(_ commandListPointer: UnsafeMutableRawPointer?, _ primitiveType: GraphicsPrimitiveType, _ startVertex: Int, _ vertexCount: Int) {
        let commandList = Unmanaged<MetalCommandList>.fromOpaque(commandListPointer!).takeUnretainedValue()

        guard let renderCommandEncoder = commandList.commandEncoder as? MTLRenderCommandEncoder else {
            print("setPipelineState: ERROR: Cannot get renderCommandEncoder")
            return
        }

        if (self.currentShader != nil) {
            renderCommandEncoder.setVertexBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
            renderCommandEncoder.setFragmentBuffer(self.currentShader!.currentArgumentBuffer, offset: 0, index: 0)
        }

        var primitiveTypeMetal = MTLPrimitiveType.triangle

        if (primitiveType == TriangleStrip) {
            primitiveTypeMetal = MTLPrimitiveType.triangleStrip
        } else if (primitiveType == Line) {
            primitiveTypeMetal = MTLPrimitiveType.line
        }

        renderCommandEncoder.drawPrimitives(type: primitiveTypeMetal, 
                                                   vertexStart: startVertex, 
                                                   vertexCount: vertexCount)

        if (self.currentShader != nil) {
            self.currentShader!.setupArgumentBuffer()
        }
    }

    public func queryTimestamp(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ index: Int) {

    }

    public func resolveQueryData(_ commandListPointer: UnsafeMutableRawPointer?, _ queryBufferPointer: UnsafeMutableRawPointer?, _ destinationBufferPointer: UnsafeMutableRawPointer?, _ startIndex: Int, _ endIndex: Int) {

    }
}
