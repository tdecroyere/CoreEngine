Texture2D InputTexture: register(s1, t1);
RWTexture2D OutputTexture: register(s1, t2);

float3 ToneMapACES(float3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;

    return saturate((x * (a * x + b)) / ( x * ( c * x + d) + e));
}

[numthreads(64, 64, 1)]
void ToneMap(uint2 pixelCoordinates: SV_DispatchThreadID)
{                
    float exposure = 0.15;

    float3 inputSample = InputTexture[pixelCoordinates].rgb;

    inputSample = ToneMapACES(inputSample * exposure);
    OutputTexture[pixelCoordinates] = float4(inputSample, 1.0);
}