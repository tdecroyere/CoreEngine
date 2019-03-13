import Cocoa
import Metal
import MetalKit
import simd

//  This structure defines the layout of each vertex in the array of vertices set as an input to our
//    Metal vertex shader.  Since this header is shared between our .metal shader and C code,
//    we can be sure that the layout of the vertex array in our C code matches the layout that
//    our .metal vertex shader expects
struct TriangleVertex {
    // Positions in pixel space
    // (e.g. a value of 100 indicates 100 pixels from the center)
    var position: simd_float2

    // Floating-point RGBA colors
    var color: simd_float4
}

class Renderer: NSObject, MTKViewDelegate {
    let device: MTLDevice
    let mtkView: MTKView
    var commandQueue: MTLCommandQueue!
    var pipelineState: MTLRenderPipelineState!

    // The current size of our view so we can use this in our render pipeline
    var viewportSize: simd_float2
    
    init(view: MTKView, device: MTLDevice) {
        self.mtkView = view
        self.device = device
        self.viewportSize = simd_float2(Float(view.drawableSize.width), Float(view.drawableSize.height))
        super.init()

        print("Renderer constructor")

        let compileOptions = MTLCompileOptions()
        compileOptions.fastMathEnabled = true
        compileOptions.languageVersion = .version2_1
   
        
        // Load all the shader files with a .metal file extension in the project
        let defaultLibrary = try! self.device.makeLibrary(source: self.getShaderSourceCode(), options: compileOptions)

        // Load the vertex function from the library
        let vertexFunction = defaultLibrary.makeFunction(name: "vertexShader")

        // Load the fragment function from the library
        let fragmentFunction = defaultLibrary.makeFunction(name: "fragmentShader")

        // // Configure a pipeline descriptor that is used to create a pipeline state
        // MTLRenderPipelineDescriptor *pipelineStateDescriptor = [[MTLRenderPipelineDescriptor alloc] init];
        // pipelineStateDescriptor.label = @"Simple Pipeline";
        // pipelineStateDescriptor.vertexFunction = vertexFunction;
        // pipelineStateDescriptor.fragmentFunction = fragmentFunction;
        // pipelineStateDescriptor.colorAttachments[0].pixelFormat = mtkView.colorPixelFormat;

        // _pipelineState = [_device newRenderPipelineStateWithDescriptor:pipelineStateDescriptor
        //                                                          error:&error];
        // if (!_pipelineState)
        // {
        //     // Pipeline State creation could fail if we haven't properly set up our pipeline descriptor.
        //     //  If the Metal API validation is enabled, we can find out more information about what
        //     //  went wrong.  (Metal API validation is enabled by default when a debug build is run
        //     //  from Xcode)
        //     NSLog(@"Failed to created pipeline state, error %@", error);
        //     return nil;
        // }

        self.commandQueue = self.device.makeCommandQueue()

        self.mtkView.clearColor = MTLClearColor.init(red: 0.0, green: 0.0, blue: 1.0, alpha: 1.0)
    }

    func mtkView(_ view: MTKView, drawableSizeWillChange size: CGSize) {
        print("Renderer MTKView func")
        self.viewportSize = simd_float2(Float(view.drawableSize.width), Float(view.drawableSize.height))
    }
 
    func draw(in view: MTKView) {
        let triangleVertices = [ 
            TriangleVertex(position: simd_float2(250, -250), color: simd_float4(1, 0, 0, 1)), 
            TriangleVertex(position: simd_float2(-250, -250), color: simd_float4(0, 1, 0, 1)), 
            TriangleVertex(position: simd_float2(0, 250), color: simd_float4(0, 0, 1, 1)) 
        ]

        view.clearColor = MTLClearColor.init(red: 0.0, green: 0.0, blue: 1.0, alpha: 1.0)

        // Create a new command buffer for each render pass to the current drawable
        let commandBuffer = self.commandQueue.makeCommandBuffer()!
        commandBuffer.label = "MyCommand";

        // Obtain a render pass descriptor, generated from the view's drawable
        let renderPassDescriptor = view.currentRenderPassDescriptor

        // If you've successfully obtained a render pass descriptor, you can render to
        // the drawable; otherwise you skip any rendering this frame because you have no
        // drawable to draw to
        if (renderPassDescriptor != nil)
        {
            // TODO: Replace with guard to unwrap value?
            
            let renderEncoder = commandBuffer.makeRenderCommandEncoder(descriptor: renderPassDescriptor!)!
            renderEncoder.label = "MyRenderEncoder"

            // Set the region of the drawable to which we'll draw.
            renderEncoder.setViewport(MTLViewport(originX: 0.0, originY: 0.0, width: Double(self.viewportSize.x), height: Double(self.viewportSize.y), znear: -1.0, zfar: 1.0))
            renderEncoder.setRenderPipelineState(self.pipelineState)

            // TODO: Switch to metal buffers
            renderEncoder.setVertexBytes(triangleVertices, length: triangleVertices.count * MemoryLayout<Float>.stride, index: 0)
            renderEncoder.setVertexBytes([self.viewportSize.x, self.viewportSize.y], length: 2 * MemoryLayout<Float>.stride, index: 1)

            // Draw the 3 vertices of our triangle
            renderEncoder.drawPrimitives(type: .triangle, vertexStart: 0, vertexCount: 3)
    
            // We would normally use the render command encoder to draw our objects, but for
            // the purposes of this sample, all we need is the GPU clear command that
            // Metal implicitly performs when we create the encoder.

            // Since we aren't drawing anything, indicate we're finished using this encoder
            renderEncoder.endEncoding()

            // Add a final command to present the cleared drawable to the screen
            commandBuffer.present(view.currentDrawable!)
        }

        // Finalize rendering here and submit the command buffer to the GPU
        commandBuffer.commit()
    }

    func getShaderSourceCode() -> String {
        return """
            #include <metal_stdlib>
            #include <simd/simd.h>
            
            using namespace metal;
            
            typedef enum AAPLVertexInputIndex
{
    AAPLVertexInputIndexVertices     = 0,
    AAPLVertexInputIndexViewportSize = 1,
} AAPLVertexInputIndex;

//  This structure defines the layout of each vertex in the array of vertices set as an input to our
//    Metal vertex shader.  Since this header is shared between our .metal shader and C code,
//    we can be sure that the layout of the vertex array in our C code matches the layout that
//    our .metal vertex shader expects
typedef struct
{
    // Positions in pixel space
    // (e.g. a value of 100 indicates 100 pixels from the center)
    vector_float2 position;

    // Floating-point RGBA colors
    vector_float4 color;
} AAPLVertex;
        
        // Vertex shader outputs and fragment shader inputs
        typedef struct
        {
            // The [[position]] attribute of this member indicates that this value is the clip space
            // position of the vertex when this structure is returned from the vertex function
            float4 clipSpacePosition [[position]];
            
            // Since this member does not have a special attribute, the rasterizer interpolates
            // its value with the values of the other triangle vertices and then passes
            // the interpolated value to the fragment shader for each fragment in the triangle
            float4 color;
            
        } RasterizerData;
        
        // Vertex function
        vertex RasterizerData
        vertexShader(uint vertexID [[vertex_id]],
                     constant AAPLVertex *vertices [[buffer(AAPLVertexInputIndexVertices)]],
            constant vector_uint2 *viewportSizePointer [[buffer(AAPLVertexInputIndexViewportSize)]])
        {
            RasterizerData out;
            
            // Initialize our output clip space position
            out.clipSpacePosition = vector_float4(0.0, 0.0, 0.0, 1.0);
            
            // Index into our array of positions to get the current vertex
            //   Our positions are specified in pixel dimensions (i.e. a value of 100 is 100 pixels from
            //   the origin)
            float2 pixelSpacePosition = vertices[vertexID].position.xy;
            
            // Dereference viewportSizePointer and cast to float so we can do floating-point division
            vector_float2 viewportSize = vector_float2(*viewportSizePointer);
            
            // The output position of every vertex shader is in clip-space (also known as normalized device
            //   coordinate space, or NDC).   A value of (-1.0, -1.0) in clip-space represents the
            //   lower-left corner of the viewport whereas (1.0, 1.0) represents the upper-right corner of
            //   the viewport.
            
            // Calculate and write x and y values to our clip-space position.  In order to convert from
            //   positions in pixel space to positions in clip-space, we divide the pixel coordinates by
            //   half the size of the viewport.
            out.clipSpacePosition.xy = pixelSpacePosition / (viewportSize / 2.0);
            
            // Pass our input color straight to our output color.  This value will be interpolated
            //   with the other color values of the vertices that make up the triangle to produce
            //   the color value for each fragment in our fragment shader
            out.color = vertices[vertexID].color;
            
            return out;
        }
        
        // Fragment function
        fragment float4 fragmentShader(RasterizerData in [[stage_in]])
        {
            // We return the color we just set which will be written to our color attachment.
            return in.color;
        }
        
"""
    }
}
