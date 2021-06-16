#pragma once
#include "WindowsCommon.h"
#include "../Common/CoreEngine.h"

#define VK_USE_PLATFORM_WIN32_KHR
#include "vulkan.h"

using namespace std;

static const int VulkanFramesCount = 2;

struct VulkanCommandQueue
{
    VkQueue CommandQueueObject;
    VkCommandPool CommandPools[VulkanFramesCount];
    VkSemaphore TimelineSemaphore;
    uint64_t FenceValue;
    uint32_t CommandQueueFamilyIndex;
};

struct VulkanCommandList
{
    VkCommandBuffer CommandBufferObject;
    VulkanCommandQueue* CommandQueue;
    GraphicsRenderPassDescriptor RenderPassDescriptor;
    bool IsRenderPassActive;
    VkFramebuffer RenderPassFrameBuffer;
};

struct VulkanGraphicsHeap
{
    VkDeviceMemory DeviceMemory;
    GraphicsServiceHeapType Type;
};

struct VulkanShaderResourceHeap
{
    VkDescriptorPool DescriptorPool;
    VkDescriptorSet DescriptorSets[2];
};

struct VulkanGraphicsBuffer
{
    VkBuffer BufferObject;
    int SizeInBytes;
    VulkanGraphicsHeap* GraphicsHeap;
    void* CpuPointer;
};

struct VulkanTexture
{
    VkImage TextureObject;
    VkImageView ImageView;
    VkFormat Format;
    uint32_t Width;
    uint32_t Height;
    bool IsPresentTexture;
    VkImageLayout ResourceState;
};

struct VulkanQueryBuffer
{

};

struct VulkanShader
{
    VkShaderModule AmplificationShaderMethod;
    VkShaderModule MeshShaderMethod;
    VkShaderModule PixelShaderMethod;
    VkShaderModule ComputeShaderMethod;
};

struct VulkanPipelineState
{
    VkRenderPass RenderPass;
    VkDescriptorSetLayout* DescriptorSetLayouts;
    uint32_t DescriptorSetLayoutCount;
    VkPipelineLayout PipelineLayoutObject;
    VkPipeline PipelineStateObject;
};

struct VulkanSwapChain
{
    VkSurfaceKHR WindowSurface;
    VkSwapchainKHR SwapChainObject;
    VulkanCommandQueue* CommandQueue;
    VkFormat Format;
    uint32_t CurrentImageIndex;
    VulkanTexture* BackBufferTextures[VulkanFramesCount];
    VkFence BackBufferAcquireFence;
};

class VulkanGraphicsService
{
    public:
        VulkanGraphicsService();
        ~VulkanGraphicsService();

        void GetGraphicsAdapterName(char* output);
        GraphicsAllocationInfos GetTextureAllocationInfos(enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);

        void* CreateCommandQueue(enum GraphicsServiceCommandType commandQueueType);
        void SetCommandQueueLabel(void* commandQueuePointer, char* label);
        void DeleteCommandQueue(void* commandQueuePointer);
        void ResetCommandQueue(void* commandQueuePointer);
        unsigned long GetCommandQueueTimestampFrequency(void* commandQueuePointer);
        unsigned long ExecuteCommandLists(void* commandQueuePointer, void** commandLists, int commandListsLength, struct GraphicsFence* fencesToWait, int fencesToWaitLength);
        void WaitForCommandQueueOnCpu(struct GraphicsFence fenceToWait);

        void* CreateCommandList(void* commandQueuePointer);
        void SetCommandListLabel(void* commandListPointer, char* label);
        void DeleteCommandList(void* commandListPointer);
        void ResetCommandList(void* commandListPointer);
        void CommitCommandList(void* commandListPointer);

        void* CreateGraphicsHeap(enum GraphicsServiceHeapType type, unsigned long sizeInBytes);
        void SetGraphicsHeapLabel(void* graphicsHeapPointer, char* label);
        void DeleteGraphicsHeap(void* graphicsHeapPointer);

        void* CreateShaderResourceHeap(unsigned long length);
        void SetShaderResourceHeapLabel(void* shaderResourceHeapPointer, char* label);
        void DeleteShaderResourceHeap(void* shaderResourceHeapPointer);
        void CreateShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index, void* texturePointer);
        void DeleteShaderResourceTexture(void* shaderResourceHeapPointer, unsigned int index);
        void CreateShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index, void* bufferPointer);
        void DeleteShaderResourceBuffer(void* shaderResourceHeapPointer, unsigned int index);

        void* CreateGraphicsBuffer(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, int sizeInBytes);
        void SetGraphicsBufferLabel(void* graphicsBufferPointer, char* label);
        void DeleteGraphicsBuffer(void* graphicsBufferPointer);
        void* GetGraphicsBufferCpuPointer(void* graphicsBufferPointer);
        void ReleaseGraphicsBufferCpuPointer(void* graphicsBufferPointer);

        void* CreateTexture(void* graphicsHeapPointer, unsigned long heapOffset, int isAliasable, enum GraphicsTextureFormat textureFormat, enum GraphicsTextureUsage usage, int width, int height, int faceCount, int mipLevels, int multisampleCount);
        void SetTextureLabel(void* texturePointer, char* label);
        void DeleteTexture(void* texturePointer);

        void* CreateSwapChain(void* windowPointer, void* commandQueuePointer, int width, int height, enum GraphicsTextureFormat textureFormat);
        void DeleteSwapChain(void* swapChainPointer);
        void ResizeSwapChain(void* swapChainPointer, int width, int height);
        void* GetSwapChainBackBufferTexture(void* swapChainPointer);
        unsigned long PresentSwapChain(void* swapChainPointer);
        void WaitForSwapChainOnCpu(void* swapChainPointer);

        void* CreateQueryBuffer(enum GraphicsQueryBufferType queryBufferType, int length);
        void SetQueryBufferLabel(void* queryBufferPointer, char* label);
        void DeleteQueryBuffer(void* queryBufferPointer);

        void* CreateShader(char* computeShaderFunction, void* shaderByteCode, int shaderByteCodeLength);
        void SetShaderLabel(void* shaderPointer, char* label);
        void DeleteShader(void* shaderPointer);

        void* CreatePipelineState(void* shaderPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
        void SetPipelineStateLabel(void* pipelineStatePointer, char* label);
        void DeletePipelineState(void* pipelineStatePointer);

        void CopyDataToGraphicsBuffer(void* commandListPointer, void* destinationGraphicsBufferPointer, void* sourceGraphicsBufferPointer, int sizeInBytes);
        void CopyDataToTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceGraphicsBufferPointer, enum GraphicsTextureFormat textureFormat, int width, int height, int slice, int mipLevel);
        void CopyTexture(void* commandListPointer, void* destinationTexturePointer, void* sourceTexturePointer);

        void DispatchThreads(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);

        void BeginRenderPass(void* commandListPointer, struct GraphicsRenderPassDescriptor renderPassDescriptor);
        void EndRenderPass(void* commandListPointer);

        void SetPipelineState(void* commandListPointer, void* pipelineStatePointer);
        void SetShaderResourceHeap(void* commandListPointer, void* shaderResourceHeapPointer);
        void SetShader(void* commandListPointer, void* shaderPointer);
        void SetShaderParameterValues(void* commandListPointer, unsigned int slot, unsigned int* values, int valuesLength);

        void DispatchMesh(void* commandListPointer, unsigned int threadGroupCountX, unsigned int threadGroupCountY, unsigned int threadGroupCountZ);

        void BeginQuery(void* commandListPointer, void* queryBufferPointer, int index);
        void EndQuery(void* commandListPointer, void* queryBufferPointer, int index);
        void ResolveQueryData(void* commandListPointer, void* queryBufferPointer, void* destinationBufferPointer, int startIndex, int endIndex);

    private:
        wstring deviceName;
        VkInstance vulkanInstance = nullptr;
        VkPhysicalDevice graphicsPhysicalDevice = nullptr;
        VkDevice graphicsDevice = nullptr;
        VkDebugReportCallbackEXT debugCallback = nullptr;

        int32_t currentCommandPoolIndex = 0;
        // TODO: To remove?
        VulkanPipelineState* currentPipelineState = nullptr;
        VulkanShaderResourceHeap* currentResourceHeap = nullptr;

        // TODO: Do something better here
        vector<VkFramebuffer> frameBuffersToDelete;

        uint32_t renderCommandQueueFamilyIndex;
        uint32_t computeCommandQueueFamilyIndex;
        uint32_t copyCommandQueueFamilyIndex;

        uint32_t gpuMemoryTypeIndex;
        uint32_t uploadMemoryTypeIndex;
        uint32_t readBackMemoryTypeIndex;

        VkInstance CreateVulkanInstance();
        VkPhysicalDevice FindGraphicsDevice();
        VkDevice CreateDevice(VkPhysicalDevice physicalDevice);
        void RegisterDebugCallback();
};