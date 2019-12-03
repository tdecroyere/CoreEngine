#include <metal_stdlib>
#include <simd/simd.h>

using namespace metal;

struct type_ConstantBuffer_CoreEngine_RenderPassParameters
{
    float4x4 ViewMatrix;
    float4x4 ProjectionMatrix;
};

struct ObjectProperties
{
    float4x4 WorldMatrix;
};

struct type_StructuredBuffer_ObjectProperties
{
    ObjectProperties _m0[1];
};

struct VertexShaderParameters
{
    uint objectPropertyIndex;
};

struct type_StructuredBuffer_VertexShaderParameters
{
    VertexShaderParameters _m0[1];
};

struct spvDescriptorSetBuffer1
{
    constant type_ConstantBuffer_CoreEngine_RenderPassParameters* renderPassParameters [[id(0)]];
    const device type_StructuredBuffer_ObjectProperties* objectProperties [[id(1)]];
    const device type_StructuredBuffer_VertexShaderParameters* vertexShaderParameters [[id(2)]];
};

struct VertexMain_out
{
    float4 out_var_TexCoord0 [[user(locn0)]];
    float4 gl_Position [[position]];
};

struct VertexMain_in
{
    float3 in_var_POSITION [[attribute(0)]];
    float3 in_var_TexCoord0 [[attribute(1)]];
};

vertex VertexMain_out VertexMain(VertexMain_in in [[stage_in]], constant spvDescriptorSetBuffer1& spvDescriptorSet1 [[buffer(1)]], uint gl_InstanceIndex [[instance_id]])
{
    VertexMain_out out = {};
    uint _45 = uint(int((*spvDescriptorSet1.vertexShaderParameters)._m0[gl_InstanceIndex].objectPropertyIndex));
    out.gl_Position = (((*spvDescriptorSet1.renderPassParameters).ProjectionMatrix * (*spvDescriptorSet1.renderPassParameters).ViewMatrix) * (*spvDescriptorSet1.objectProperties)._m0[_45].WorldMatrix) * float4(in.in_var_POSITION, 1.0);
    out.out_var_TexCoord0 = normalize((*spvDescriptorSet1.objectProperties)._m0[_45].WorldMatrix * float4(in.in_var_TexCoord0, 0.0));
    return out;
}

struct PixelMain_out
{
    float4 out_var_SV_TARGET [[color(0)]];
};

struct PixelMain_in
{
    float4 in_var_TexCoord0 [[user(locn0)]];
};

fragment PixelMain_out PixelMain(PixelMain_in in [[stage_in]])
{
    PixelMain_out out = {};
    out.out_var_SV_TARGET = (in.in_var_TexCoord0 * 0.5) + float4(0.5);
    return out;
}

