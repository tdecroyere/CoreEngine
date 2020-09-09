import Metal
import simd
import CoreEngineCommonInterop

func convertTextureFormat(_ textureFormat: GraphicsTextureFormat) -> MTLPixelFormat {
    if (textureFormat == Bgra8UnormSrgb) {
        return .bgra8Unorm_srgb
    } else if (textureFormat == Depth32Float) {
        return .depth32Float
    } else if (textureFormat == Rgba16Float) {
        return .rgba16Float
    } else if (textureFormat == R16Float) {
        return .r16Float
    } else if (textureFormat == BC1Srgb) {
        return .bc1_rgba_srgb
    } else if (textureFormat == BC2Srgb) {
        return .bc2_rgba_srgb
    } else if (textureFormat == BC3Srgb) {
        return .bc3_rgba_srgb
    } else if (textureFormat == BC4) {
        return .bc4_rUnorm
    } else if (textureFormat == BC5) {
        return .bc5_rgUnorm
    } else if (textureFormat == BC6) {
        return .bc6H_rgbuFloat
    } else if (textureFormat == BC7Srgb) {
        return .bc7_rgbaUnorm_srgb
    } else if (textureFormat == Rgba32Float) {
        return .rgba32Float
    } else if (textureFormat == Rgba16Unorm) {
        return .rgba16Unorm
    }
    
    return .rgba8Unorm_srgb
}

func createTextureDescriptor(_ textureFormat: GraphicsTextureFormat, _ usage: GraphicsTextureUsage, _ width: Int, _ height: Int, _ faceCount: Int, _ mipLevels: Int, _ multisampleCount: Int) -> MTLTextureDescriptor {
    // TODO: Check for errors
    let descriptor = MTLTextureDescriptor()

    descriptor.width = width
    descriptor.height = height
    descriptor.depth = 1
    descriptor.mipmapLevelCount = mipLevels
    descriptor.arrayLength = 1
    descriptor.sampleCount = multisampleCount
    descriptor.storageMode = .private
    descriptor.hazardTrackingMode = .untracked
    descriptor.pixelFormat = convertTextureFormat(textureFormat)

    if (usage == RenderTarget) {
        descriptor.usage = [.shaderRead, .renderTarget]
    } else if (usage == ShaderWrite) {
        descriptor.usage = [.shaderRead, .shaderWrite]
    } else {
        descriptor.usage = [.shaderRead]
    }

    if (multisampleCount > 1) {
        descriptor.textureType = .type2DMultisample
    } else if (faceCount > 1) {
        descriptor.textureType = .typeCube
    } else {
        descriptor.textureType = .type2D
    }

    return descriptor
}

func initBlendState(_ colorAttachmentDescriptor: MTLRenderPipelineColorAttachmentDescriptor, _ blendOperation: GraphicsBlendOperation) {
    if (blendOperation == AlphaBlending) {
        colorAttachmentDescriptor.isBlendingEnabled = true
        colorAttachmentDescriptor.rgbBlendOperation = .add
        colorAttachmentDescriptor.alphaBlendOperation = .add
        colorAttachmentDescriptor.sourceRGBBlendFactor = .sourceAlpha
        colorAttachmentDescriptor.sourceAlphaBlendFactor = .sourceAlpha;
        colorAttachmentDescriptor.destinationRGBBlendFactor = .oneMinusSourceAlpha
        colorAttachmentDescriptor.destinationAlphaBlendFactor = .oneMinusSourceAlpha
    } else if (blendOperation == AddOneOne) {
        colorAttachmentDescriptor.isBlendingEnabled = true
        colorAttachmentDescriptor.rgbBlendOperation = .add
        colorAttachmentDescriptor.alphaBlendOperation = .add
        colorAttachmentDescriptor.sourceRGBBlendFactor = .one
        colorAttachmentDescriptor.sourceAlphaBlendFactor = .one;
        colorAttachmentDescriptor.destinationRGBBlendFactor = .one
        colorAttachmentDescriptor.destinationAlphaBlendFactor = .one
    } else if (blendOperation == AddOneMinusSourceColor) {
        colorAttachmentDescriptor.isBlendingEnabled = true
        colorAttachmentDescriptor.rgbBlendOperation = .add
        colorAttachmentDescriptor.sourceRGBBlendFactor = .zero
        colorAttachmentDescriptor.destinationRGBBlendFactor = .oneMinusSourceColor
    }
}