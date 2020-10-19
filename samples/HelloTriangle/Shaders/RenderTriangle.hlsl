#define RootSignatureDef \
    "RootFlags(0)"

struct VertexOutput
{
    float4 Position: SV_Position;
    float3 Color: TEXCOORD0;
};

VertexOutput VertexMain(const uint vertexId: SV_VertexID)
{
    VertexOutput output = (VertexOutput)0;

    float size = 0.4;

    if ((vertexId) % 3 == 0)
    {
        output.Position = float4(-size, size, 0, 1);
        output.Color = float3(1, 0, 0);
    }

    else if ((vertexId) % 3 == 1)
    {
        output.Position = float4(size, size, 0, 1);
        output.Color = float3(0, 1, 0);
    }

    else if ((vertexId) % 3 == 2)
    {
        output.Position = float4(-size, -size, 0, 1);
        output.Color = float3(0, 0, 1);
    }

    return output;
}

struct PixelOutput
{
    float4 Color: SV_TARGET0;
};

PixelOutput PixelMain(const VertexOutput input)
{
    PixelOutput output = (PixelOutput)0;
    output.Color = float4(input.Color, 1);
    return output; 
}