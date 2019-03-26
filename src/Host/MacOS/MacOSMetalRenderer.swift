import Cocoa
import Metal
import MetalKit
import simd
import CoreEngineInterop

struct TriangleVertex {
    var position: float4
    var color: float4
}

struct RenderPassConstantBuffer {
    var viewMatrix: float4x4
    var projectionMatrix: float4x4

    init() {
        self.viewMatrix = makeIdentityMatrix()
        self.projectionMatrix = makeIdentityMatrix()
    }
}

struct ObjectConstantBuffer {
    var worldMatrix: float4x4

    init() {
        self.worldMatrix = makeIdentityMatrix()
    }
}

public func radians<T: FloatingPoint>(degrees: T) -> T {
    return .pi * degrees / 180
}

public func degrees<T: FloatingPoint>(radians: T) -> T {
    return radians * 180 / .pi
}

func makeIdentityMatrix() -> float4x4 {
    let row1 = float4(1, 0, 0, 0)
    let row2 = float4(0, 1, 0, 0)
    let row3 = float4(0, 0, 1, 0)
    let row4 = float4(0, 0, 0, 1)
    
    return float4x4(rows: [row1, row2, row3, row4])
}

func makeLookAtMatrix(cameraPosition: float3, cameraTarget: float3, cameraUpVector: float3) -> float4x4 {
    let zAxis = normalize(cameraTarget - cameraPosition)
    let xAxis = normalize(cross(cameraUpVector, zAxis))
    let yAxis = normalize(cross(zAxis, xAxis))

    let row1 = float4(xAxis.x, yAxis.x, zAxis.x, 0)
    let row2 = float4(xAxis.y, yAxis.y, zAxis.y, 0)
    let row3 = float4(xAxis.z, yAxis.z, zAxis.z, 0)
    let row4 = float4(-dot(xAxis, cameraPosition), -dot(yAxis, cameraPosition), -dot(zAxis, cameraPosition), 1)
    
    return float4x4(rows: [row1, row2, row3, row4])
}

func makePerspectiveFovMatrix(fieldOfViewY: Float, aspectRatio: Float, minPlaneZ: Float, maxPlaneZ: Float) -> float4x4 {
    let height = 1.0 / tan(fieldOfViewY / 2.0)

    let row1 = float4(height / aspectRatio, 0, 0, 0)
    let row2 = float4(0, height, 0, 0)
    let row3 = float4(0, 0, (maxPlaneZ / (maxPlaneZ - minPlaneZ)), 1)
    let row4 = float4(0, 0, -minPlaneZ * maxPlaneZ / (maxPlaneZ - minPlaneZ), 0)

    return float4x4(rows: [row1, row2, row3, row4])
}

func debugDrawTriangle(graphicsContext: UnsafeMutableRawPointer?, color1: Vector4, color2: Vector4, color3: Vector4, worldMatrix: Matrix4x4) {
    let renderer = Unmanaged<MacOSMetalRenderer>.fromOpaque(graphicsContext!).takeUnretainedValue()

    // TODO: Write a convert implicit func

    let dstColor1 = float4(color1.X, color1.Y, color1.Z, color1.W)
    let dstColor2 = float4(color2.X, color2.Y, color2.Z, color2.W)
    let dstColor3 = float4(color3.X, color3.Y, color3.Z, color3.W)

    let row1 = float4(worldMatrix.Item00, worldMatrix.Item01, worldMatrix.Item02, worldMatrix.Item03)
    let row2 = float4(worldMatrix.Item10, worldMatrix.Item11, worldMatrix.Item12, worldMatrix.Item13)
    let row3 = float4(worldMatrix.Item20, worldMatrix.Item21, worldMatrix.Item22, worldMatrix.Item23)
    let row4 = float4(worldMatrix.Item30, worldMatrix.Item31, worldMatrix.Item32, worldMatrix.Item33)
    
    let dstWorldMatrix = float4x4(rows: [row1, row2, row3, row4])

    renderer.drawTriangle(dstColor1, dstColor2, dstColor3, dstWorldMatrix)
}

class MacOSMetalRenderer: NSObject, MTKViewDelegate {
    let device: MTLDevice
    let mtkView: MTKView
    var commandQueue: MTLCommandQueue!
    var pipelineState: MTLRenderPipelineState!
    var currentCommandBuffer: MTLCommandBuffer!
    var currentRenderEncoder: MTLRenderCommandEncoder!

    var viewportSize: float2
    
    init(view: MTKView, device: MTLDevice) {
        self.mtkView = view
        self.device = device
        self.viewportSize = float2(Float(view.drawableSize.width), Float(view.drawableSize.height))
        self.currentCommandBuffer = nil

        super.init()

        let compileOptions = MTLCompileOptions()
        compileOptions.fastMathEnabled = true
        compileOptions.languageVersion = .version2_1
   
        // Load all the shader files with a .metal file extension in the project
        let defaultLibrary = try! self.device.makeLibrary(source: self.getShaderSourceCode(), options: compileOptions)

        // Load the vertex function from the library
        let vertexFunction = defaultLibrary.makeFunction(name: "VertexMain")

        // Load the fragment function from the library
        let fragmentFunction = defaultLibrary.makeFunction(name: "PixelMain")

        // Configure a pipeline descriptor that is used to create a pipeline state
        let pipelineStateDescriptor = MTLRenderPipelineDescriptor()
        pipelineStateDescriptor.label = "Simple Pipeline"
        pipelineStateDescriptor.vertexFunction = vertexFunction
        pipelineStateDescriptor.fragmentFunction = fragmentFunction
        pipelineStateDescriptor.colorAttachments[0].pixelFormat = self.mtkView.colorPixelFormat

        do {
            self.pipelineState = try self.device.makeRenderPipelineState(descriptor: pipelineStateDescriptor)
        } catch {
            print("Failed to created pipeline state, \(error)")
        }

        self.commandQueue = self.device.makeCommandQueue()
    }

    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
        print("Renderer MTKView func")
        self.viewportSize = simd_float2(Float(view.drawableSize.width), Float(view.drawableSize.height))
    }

    func beginRender() {
        // Create a new command buffer for each render pass to the current drawable
        self.currentCommandBuffer = self.commandQueue.makeCommandBuffer()!
        self.currentCommandBuffer.label = "MyCommand"

        self.mtkView.clearColor = MTLClearColor.init(red: 0.0, green: 0.0, blue: 1.0, alpha: 1.0)

        var renderPassBuffer = RenderPassConstantBuffer()
        renderPassBuffer.viewMatrix = makeLookAtMatrix(cameraPosition: float3(0, 0, -5), cameraTarget: float3(0, 0, 0), cameraUpVector: float3(0, 1, 0))
        renderPassBuffer.projectionMatrix = makePerspectiveFovMatrix(fieldOfViewY: radians(degrees: 39.375), aspectRatio: self.viewportSize.x / self.viewportSize.y, minPlaneZ: 1.0, maxPlaneZ: 10000)
        
        self.mtkView.clearColor = MTLClearColor.init(red: 0.0, green: 0.5, blue: 1.0, alpha: 1.0)

        // Obtain a render pass descriptor, generated from the view's drawable
        let renderPassDescriptor = self.mtkView.currentRenderPassDescriptor!
        
        self.currentRenderEncoder = self.currentCommandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor)!
        self.currentRenderEncoder.label = "BeginRenderEncoder"

        // Set the region of the drawable to which we'll draw.
        self.currentRenderEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(self.viewportSize.x), height: Double(self.viewportSize.y), znear: -1.0, zfar: 1.0))
        self.currentRenderEncoder.setRenderPipelineState(self.pipelineState)
        self.currentRenderEncoder.setVertexBytes(&renderPassBuffer, length: MemoryLayout<RenderPassConstantBuffer>.size, index: 1)
    }

    func endRender() {
        self.currentRenderEncoder.endEncoding()
        self.currentCommandBuffer.present(self.mtkView.currentDrawable!)
        self.currentCommandBuffer.commit()

        self.currentRenderEncoder = nil
        self.currentCommandBuffer = nil
    }

    func drawTriangle(_ color1: float4, _ color2: float4, _ color3: float4, _ worldMatrix: float4x4) {
        // TODO: Change the fact that we have only one command buffer stored in a private field
        let triangleVertices = [ 
            TriangleVertex(position: float4(1, -1, 0, 1), color: color1), 
            TriangleVertex(position: float4(-1, -1, 0, 1), color: color2), 
            TriangleVertex(position: float4(0, 1, 0, 1), color: color3) 
        ]

        var objectBuffer = ObjectConstantBuffer()
        objectBuffer.worldMatrix = worldMatrix

        self.currentRenderEncoder.setRenderPipelineState(self.pipelineState)

        // TODO: Switch to metal buffers
        self.currentRenderEncoder.setVertexBytes(triangleVertices, length: triangleVertices.count * MemoryLayout<TriangleVertex>.size, index: 0)
        self.currentRenderEncoder.setVertexBytes(&objectBuffer, length: MemoryLayout<ObjectConstantBuffer>.size, index: 2)

        // Draw the 3 vertices of our triangle
        self.currentRenderEncoder.drawPrimitives(type: .triangle, vertexStart: 0, vertexCount: 3)
    }
 
    func draw(in view: MTKView) {

    }

    func getShaderSourceCode() -> String {
        return """
            #include <metal_stdlib>
            #include <simd/simd.h>
            
            using namespace metal;
           
            struct VertexInput
            {
                float4 Position;
                float4 Color;
            };

            struct VertexOutput
            {
                float4 Position [[position]];
                float4 Color;
            };

            struct CoreEngine_RenderPassConstantBuffer {
                float4x4 ViewMatrix;
                float4x4 ProjectionMatrix;
            };

            struct CoreEngine_ObjectConstantBuffer
            {
                float4x4 WorldMatrix;
            };
        
            vertex VertexOutput VertexMain(uint vertexID [[vertex_id]], 
                                           constant VertexInput* input [[buffer(0)]], 
                                           constant CoreEngine_RenderPassConstantBuffer* renderPassParametersPointer [[buffer(1)]],
                                           constant CoreEngine_ObjectConstantBuffer* objectParametersPointer [[buffer(2)]])
            {
                VertexOutput output;

                CoreEngine_RenderPassConstantBuffer renderPassParameters = *renderPassParametersPointer;
                CoreEngine_ObjectConstantBuffer objectParameters = *objectParametersPointer;
                
                float4x4 worldViewProjMatrix = objectParameters.WorldMatrix * renderPassParameters.ViewMatrix * renderPassParameters.ProjectionMatrix;

                output.Position = input[vertexID].Position * worldViewProjMatrix;
                output.Color = input[vertexID].Color;
                
                return output;
            }
            
            fragment float4 PixelMain(VertexOutput input [[stage_in]])
            {
                return input.Color;
            }
"""
    }
}
